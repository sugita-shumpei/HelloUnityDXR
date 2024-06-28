using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Denoising;
using UnityEngine.Assertions;

[RequireComponent(typeof(Camera))]
public class SimplePathTracer : MonoBehaviour
{
    enum BackgroundMode
    {
        Skybox    ,
        SolidColor
    }
    [SerializeField]
    private RayTracingShader                rayTracingShader       = null;
    private RenderTexture                   _outputTexture         = null;
    private RenderTexture                   _accumeTexture         = null;
    private RayTracingAccelerationStructure _accelerationStructure = null;
    private GraphicsBuffer                  _randomBuffer          = null;
    private Matrix4x4                       _prevViewMatrix        = Matrix4x4.identity;
    private Matrix4x4                       _prevProjMatrix        = Matrix4x4.identity;
    private int                             _accumeSamples         = 0;
    [SerializeField]
    private int                             dispatchSamples        = 1;
    private int                             _dispatchSamples       = 1;
    [SerializeField]
    private BackgroundMode                  backgroundMode         = BackgroundMode.Skybox;
    private BackgroundMode                  _prevBackgroundMode    = BackgroundMode.Skybox;
    [SerializeField]
    private Color                           backgroundColor        = Color.black;
    private Color                           _backgroundColor       = Color.black;
    [SerializeField]
    private bool                            denoising              = false;
    private bool                            _denoising             = false;
    [SerializeField]
    private bool                            tonemapping            = true;
    private bool                            _tonemapping           = true;
    [SerializeField]
    private Color                           whiteColor             = Color.white;
    [SerializeField,Range(0.01F, 100.0F)]
    private float                           whiteIntensity         = 1.0f;
    [SerializeField, Range(0.001F,0.20F)]
    private float                           exposure               = 1.0f;
    private Material                        _luminanceMaterial     = null;
    private Material                        _TonemapMaterial       = null;
    private Material                        _copySkyboxMaterial    = null;
    Material luminanceMaterial
    {
        get
        {
            if (_luminanceMaterial == null)
            {
                _luminanceMaterial = new Material(Shader.Find("Hidden/Luminance"));
            }
            return _luminanceMaterial;
        }
    }
    Material tonemapMaterial
    {
        get
        {
            if (_TonemapMaterial == null)
            {
                _TonemapMaterial = new Material(Shader.Find("Hidden/Tonemap"));
            }
            return _TonemapMaterial;
        }
    }
    Material copySkyboxMaterial
    {
        get
        {
            if (_copySkyboxMaterial == null)
            {
                _copySkyboxMaterial = new Material(Shader.Find("Hidden/CopySkybox"));
            }
            return _copySkyboxMaterial;
        }
    }
    

    private int  _resIdxRenderTarget    = 0;
    private int  _resIdxAccumeTarget    = 0;
    private int  _resIdxRandomBuffer    = 0;
    private int  _resIdxWorld           = 0;
    private int  _resIdxAccumeSamples   = 0;
    private int  _resIdxSkybox          = 0;
    private int  _resIdxDispatchSamples = 0;
    private int _resIdxBackgroundMode   = 0;
    private int _resIdxBackgroundColor  = 0;
    private bool _dirtyAS               = false;
    private bool _updateAS              = false;
    private void OnEnable()
    {
        _resIdxRenderTarget = Shader.PropertyToID("RenderTarget");
        _resIdxAccumeTarget = Shader.PropertyToID("AccumeTarget");
        _resIdxRandomBuffer = Shader.PropertyToID("RandomBuffer");
        _resIdxWorld = Shader.PropertyToID("World");
        _resIdxAccumeSamples = Shader.PropertyToID("AccumeSamples");
        _resIdxDispatchSamples = Shader.PropertyToID("DispatchSamples");
        _resIdxSkybox = Shader.PropertyToID("Skybox");
        _resIdxBackgroundMode = Shader.PropertyToID("BackgroundMode");
        _resIdxBackgroundColor = Shader.PropertyToID("BackgroundColor");
        _accelerationStructure = new RayTracingAccelerationStructure();
        var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;
            if (materials != null)
            {
                foreach (var material in materials)
                {
                    if (material.HasColor("_EmissionColor") && material.HasFloat("_EmissionIntensity"))
                    {
                        var color = material.GetColor("_EmissionColor");
                        var intensity = material.GetFloat("_EmissionIntensity");
                        if (color != Color.black && intensity > 0.0f)
                        {
                            var gameObject = renderer.gameObject;
                            gameObject.AddComponent<AreaLightController>();

                        }
                    }
                }
            }
        }
        CreateTexture();
        CreateRandomBuffer(); 
        BuildAccelerationStructure();
        InitShaderParameters();
    }

    private void OnDisable()
    {
        if (_accelerationStructure != null)
        {
            _accelerationStructure.Release();
            _accelerationStructure = null;
        }
        ReleaseTexture();
        ReleaseRandomBuffer();
        var renderers = FindObjectsByType<AreaLightController>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
        foreach (var renderer in renderers)
        {
            Destroy(renderer);
        }
    }
    void CreateTexture()
    {
        ReleaseTexture();
        _outputTexture = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
        _outputTexture.enableRandomWrite = true;
        _outputTexture.Create();
        _accumeTexture = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 0, RenderTextureFormat.ARGBFloat);
        _accumeTexture.enableRandomWrite = true;
        _accumeTexture.Create();
    }
    void ReleaseTexture()
    {
        if (_outputTexture != null)
        {
            _outputTexture.Release();
            _outputTexture = null;
        }
        if (_accumeTexture != null)
        {
            _accumeTexture.Release();
            _accumeTexture = null;
        }
    }
    void CreateRandomBuffer()
    {
        ReleaseRandomBuffer();
        _randomBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, Camera.main.pixelWidth * Camera.main.pixelHeight, sizeof(uint));
        var random = new System.Random();
        var data = new List<uint>();
        data.Capacity = Camera.main.pixelWidth * Camera.main.pixelHeight;
        for (int i = 0; i < data.Capacity; i++)
        {
            data.Add((uint)random.Next());
        }
        _randomBuffer.SetData(data);
    }
    void ReleaseRandomBuffer()
    {
        if (_randomBuffer != null)
        {
            _randomBuffer.Release();
            _randomBuffer = null;
        }
    }

    void Resize(int width_, int height_, bool clear_ = false)
    {
        if (_outputTexture.width == width_ && _outputTexture.height == height_)
        {
            if (clear_)
            {
                RenderTexture activeRT = RenderTexture.active;
                RenderTexture.active   = _accumeTexture;
                GL.Clear(true, true, Color.black);
                RenderTexture.active   = activeRT;
            }
            return;
        }
        CreateTexture();
        CreateRandomBuffer();
        UpdateShaderParameters();
        return;
    }

    public void MarkDirty()
    {
        _dirtyAS = true;
    }

    void InitShaderParameters()
    {
        if (rayTracingShader == null) { return; }
        rayTracingShader.SetTexture(_resIdxRenderTarget, _outputTexture);
        rayTracingShader.SetTexture(_resIdxAccumeTarget, _accumeTexture);
        rayTracingShader.SetBuffer(_resIdxRandomBuffer , _randomBuffer);
        rayTracingShader.SetInt(_resIdxAccumeSamples   , _accumeSamples);
        rayTracingShader.SetInt(_resIdxDispatchSamples, _dispatchSamples);

    }

    void UpdateShaderParameters()
    {
        if (rayTracingShader == null) { return; }
        rayTracingShader.SetTexture(_resIdxRenderTarget, _outputTexture);
        rayTracingShader.SetTexture(_resIdxAccumeTarget, _accumeTexture);
        rayTracingShader.SetBuffer(_resIdxRandomBuffer , _randomBuffer);
    }

    void BuildAccelerationStructure()
    {
        var flags = new List<RayTracingSubMeshFlags>();
        flags.Capacity = 1;
        for (int i = 0; i < flags.Capacity; i++)
        {
            flags.Add(RayTracingSubMeshFlags.Enabled);
        }
        // レイトレーシングの対象となるRendererを取得
        var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.InstanceID);
        // それまでのInstanceをクリア
        _accelerationStructure.ClearInstances();
        foreach (var renderer in renderers)
        {
            // レイトレーシングの対象となるRendererを登録
            _accelerationStructure.AddInstance(renderer, flags.ToArray());
        }
        // ビルド
        _accelerationStructure.Build();
        // ビルドしたAccelerationStructureをShaderに設定
        if (rayTracingShader == null) { return; }
        rayTracingShader.SetAccelerationStructure(_resIdxWorld, _accelerationStructure);
    }

    bool UpdateResources(int width_, int height_)
    {
        // 本実装ではASの更新と描画を分けて処理する
        // つまり, ビルドを行った場合, 処理が重くなるため, 一つ前のフレームの結果を返し, 描画の時間をビルドに割り当てる
        bool updateAS     = _updateAS;
        bool isDirty      = _dirtyAS ;
        // 加算フレームをクリアするかどうか（著しくシーンの変更が重なった場合, このフラグを立てて加算フレームをクリアする)
        bool updateFrame  = false;
        // カメラが動いた場合, 加算フレームをクリア
        {
            var viewMatrix = Camera.main.worldToCameraMatrix;
            var projMatrix = Camera.main.projectionMatrix;
            if (viewMatrix != _prevViewMatrix || projMatrix != _prevProjMatrix)
            {
                updateFrame = true;
                _prevViewMatrix = viewMatrix;
                _prevProjMatrix = projMatrix;
            }
        }
        {
            // 背景の変更があった場合, 加算フレームをクリア
            if (backgroundMode != _prevBackgroundMode)
            {
                _prevBackgroundMode = backgroundMode;
                updateFrame = true;
            }
            if (_prevBackgroundMode == BackgroundMode.SolidColor)
            {
                if (backgroundColor != _backgroundColor)
                {
                    _backgroundColor = backgroundColor;
                    updateFrame = true;
                }
            }
        }
        /// リサイズ処理
        Resize(width_, height_, updateFrame);
        // ASの更新が必要な場合, ビルドを行う
        if (_dirtyAS)
        {
            _updateAS = true;
            _dirtyAS  = false;
        }
        // ビルド処理
        if (updateAS)
        {
            BuildAccelerationStructure();
            _updateAS   = false;
            isDirty     = true;
            updateFrame = true;
        }
        // フレームの更新を更新する場合, 加算フレームをクリア
        if (updateFrame)
        {
            _accumeSamples = 0;
        }
        // パラメータの更新
        {
            if (_tonemapping != tonemapping)
            {
                _tonemapping = tonemapping;
            }
            if (_denoising != denoising)
            {
                _denoising = denoising;
            }
        }
        return isDirty;
    }
    private void CopySkybox(RenderTexture dstCubemap)
    {
        // Skyboxをコピーする
        RenderTexture tempFaceRT = RenderTexture.GetTemporary(dstCubemap.width, dstCubemap.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        RenderTexture activeRT   = RenderTexture.active;
        for (int i = 0; i < 6; i++)
        {
            Graphics.Blit(null, tempFaceRT,copySkyboxMaterial, i);
            Graphics.CopyTexture(tempFaceRT, 0, 0, dstCubemap, i, 0);
        }
        RenderTexture.active = activeRT;
        RenderTexture.ReleaseTemporary(tempFaceRT);
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (rayTracingShader == null || !SystemInfo.supportsRayTracing || !SystemInfo.supportsRayTracingShaders)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            // ASの更新を行うかどうか
            if (!UpdateResources(source.width, source.height))
            {
                // 必要ないなら通常通り描画
                if (_dispatchSamples != dispatchSamples)
                {
                    _dispatchSamples = dispatchSamples;
                }
                // なぜかRayTracingShader内では, unity_SpecCube0が見つからないとエラーが出るので, ここで一時的にコピーを作成
                RenderTexture skyCubeRT = RenderTexture.GetTemporary(new RenderTextureDescriptor(512, 512, RenderTextureFormat.ARGBFloat, 0, 0, RenderTextureReadWrite.Linear){dimension = UnityEngine.Rendering.TextureDimension.Cube});
                CopySkybox(skyCubeRT);
                // レイトレーシングを実行
                rayTracingShader.SetTexture(_resIdxSkybox        , skyCubeRT);
                rayTracingShader.SetInt(_resIdxAccumeSamples     , _accumeSamples);
                rayTracingShader.SetInt(_resIdxDispatchSamples   , _dispatchSamples);
                rayTracingShader.SetInt(_resIdxBackgroundMode    , (int)backgroundMode);
                rayTracingShader.SetVector(_resIdxBackgroundColor, new Vector4(backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundColor.a));
                rayTracingShader.SetShaderPass("SimplePathTracer");
                rayTracingShader.Dispatch("RayGenShaderForSensor", _outputTexture.width, _outputTexture.height, 1, Camera.main);
                // デノイジングを行う
                // 本質的な部分ではないので無視してもらっても構わない
                if (_denoising)
                {
                    var denoiser = new Denoiser();
                    var result   = denoiser.Init(DenoiserType.OpenImageDenoise, _outputTexture.width, _outputTexture.height);
                    Assert.AreEqual(Denoiser.State.Success, result);
                    var request  = AsyncGPUReadback.Request(_outputTexture);
                    request.WaitForCompletion();
                    result       = denoiser.DenoiseRequest("color", request.GetData<Vector4>());
                    Assert.AreEqual(Denoiser.State.Success, result);
                    var dst      = new NativeArray<Vector4>(_outputTexture.width * _outputTexture.height, Allocator.Temp);
                    result       = denoiser.GetResults(dst); ;
                    Assert.AreEqual(Denoiser.State.Success, result);
                    var tex      = new Texture2D(_outputTexture.width, _outputTexture.height, TextureFormat.RGBAFloat, false);
                    tex.LoadRawTextureData(dst);
                    tex.Apply(false);
                    Graphics.Blit(tex, _outputTexture);
                    denoiser.DisposeDenoiser();
                    dst.Dispose();
                    Object.Destroy(tex);
                }
                // トーンマッピング(Reinhard Tonemapping)を実行
                // 本質的な部分ではないので無視してもらっても構わない
                if (_tonemapping)
                {
                    // Reinhard Tonemapping
                    RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(_outputTexture.width, _outputTexture.height, RenderTextureFormat.ARGBFloat, 0, Texture.GenerateAllMips);
                    // Reinhard Tonemappingでは輝度値の平均値を求める必要があるが, 普通に面倒なのでMipmap0で代用
                    rtDesc.useMipMap = true;
                    RenderTexture luminanceRT = RenderTexture.GetTemporary(rtDesc);
                    // 輝度値の計算
                    Graphics.Blit(_outputTexture, luminanceRT, luminanceMaterial);
                    tonemapMaterial.SetTexture("_LuminanceAverageTex", luminanceRT);
                    tonemapMaterial.SetColor("_WhiteColor", whiteColor);
                    tonemapMaterial.SetFloat("_WhiteIntensity", whiteIntensity);
                    tonemapMaterial.SetFloat("_Exposure", exposure);
                    Graphics.Blit(_outputTexture, destination, tonemapMaterial);
                    // 開放処理
                    RenderTexture.ReleaseTemporary(luminanceRT);
                }
                else
                {
                    Graphics.Blit(_outputTexture, destination);
                }
                RenderTexture.ReleaseTemporary(skyCubeRT);
                _accumeSamples = _accumeSamples + _dispatchSamples;
            }
            else
            {
                // 行った場合, 処理時間を稼ぐため一つ前のフレームの結果を返す
                Graphics.Blit(_outputTexture, destination);
            }
        }
    }
}
