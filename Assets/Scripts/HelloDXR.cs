using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class HelloDXR : MonoBehaviour
{
    enum BackgroundMode
    {
        Skybox,
        SolidColor
    }
    [SerializeField]
    private RayTracingShader rayTracingShader = null;
    private RayTracingAccelerationStructure _accelerationStructure = null;
    private RenderTexture _renderTarget = null;

    private Matrix4x4 _prevViewMatrix = Matrix4x4.identity;
    private Matrix4x4 _prevProjMatrix = Matrix4x4.identity;
    private int _resIdxRenderTarget = 0;
    private int _resIdxWorld = 0;
    private bool _dirtyAS = false;
    private bool _updateAS = false;

    private void Awake()
    {
        if (rayTracingShader == null)
        {
            return;
        }
        _resIdxRenderTarget    = Shader.PropertyToID("RenderTarget");
        _resIdxWorld           = Shader.PropertyToID("World");
        _accelerationStructure = new RayTracingAccelerationStructure();
        _renderTarget = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _renderTarget.enableRandomWrite = true;
        _renderTarget.Create();
    }
    private void OnEnable()
    {
        BuildAccelerationStructure();
    }

    private void OnDisable()
    {
        if (_accelerationStructure != null)
        {
            _accelerationStructure.Release();
            _accelerationStructure = null;
        }
        if (_renderTarget != null)
        {
            _renderTarget.Release();
            _renderTarget = null;
        }
    }
    public void MarkDirty()
    {
        _dirtyAS = true;
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
        var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
        // それまでのInstanceをクリア
        _accelerationStructure.ClearInstances();
        foreach (var renderer in renderers)
        {
            // レイトレーシングの対象となるRendererを登録
            _accelerationStructure.AddInstance(renderer, flags.ToArray());
        }
        // ビルド
        _accelerationStructure.Build();
    }

    bool UpdateResources(int width_, int height_)
    {
        bool updateAS = _updateAS;
        bool isDirty = _dirtyAS;
        var viewMatrix = Camera.main.worldToCameraMatrix;
        var projMatrix = Camera.main.projectionMatrix;
        if (_renderTarget)
        {
            if (_renderTarget.width != width_ || _renderTarget.height != height_)
            {
                _renderTarget.Release();
                _renderTarget.width = width_;
                _renderTarget.height = height_;
                _renderTarget.Create();
            }
        } else if (_renderTarget == null)
        {
            _renderTarget = new RenderTexture(width_, height_, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _renderTarget.enableRandomWrite = true;
            _renderTarget.Create();
        }
        if (viewMatrix != _prevViewMatrix || projMatrix != _prevProjMatrix)
        {
            _prevViewMatrix = viewMatrix;
            _prevProjMatrix = projMatrix;
        }
        if (updateAS)
        {
            BuildAccelerationStructure();
            _updateAS = false;
            isDirty   = true;
        }
        return isDirty;
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (rayTracingShader == null)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            if (!UpdateResources(source.width, source.height))
            {
                // レイトレーシングシェーダーのパスを設定(必須)
                // ここではPathTraceという名前のパスを使用
                // この名前はAccelerationStructureのビルド時に入力したRendererのMaterialのパス名に対応している
                // もし, パス名が含まれていない場合, そのマテリアルではシェーダが実行されないので注意
                rayTracingShader.SetShaderPass("HelloDXR");
                rayTracingShader.SetTexture(_resIdxRenderTarget, _renderTarget);
                rayTracingShader.SetAccelerationStructure(_resIdxWorld, _accelerationStructure);
                // レイトレーシングを実行
                rayTracingShader.Dispatch("RayGenShaderForTest", Screen.width, Screen.height, 1, Camera.main);
                Graphics.Blit(_renderTarget, destination);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }
    }
}
