using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


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
    private BackgroundMode backgroundMode   = BackgroundMode.Skybox;
    private BackgroundMode _prevBackgroundMode  = BackgroundMode.Skybox;
    [SerializeField]
    private Color          backgroundColor  = Color.black;
    private Color          _backgroundColor = Color.black;
    [SerializeField]
    private bool tonemapping = true;
    private bool _tonemapping = true;
    [SerializeField]
    private Color                           whiteColor             = Color.white;
    [SerializeField,Range(0.01F, 100.0F)]
    private float                           whiteIntensity         = 1.0f;
    [SerializeField, Range(0.001F,1.0F)]
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

    private void Awake()
    {
        if (rayTracingShader == null)
        {
            return;
        }
        _resIdxRenderTarget    = Shader.PropertyToID("RenderTarget");
        _resIdxAccumeTarget    = Shader.PropertyToID("AccumeTarget");
        _resIdxRandomBuffer    = Shader.PropertyToID("RandomBuffer");
        _resIdxWorld           = Shader.PropertyToID("World");
        _resIdxAccumeSamples   = Shader.PropertyToID("AccumeSamples");
        _resIdxDispatchSamples = Shader.PropertyToID("DispatchSamples");
        _resIdxSkybox          = Shader.PropertyToID("Skybox");
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
    }
    private void OnEnable()
    {
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
        // ���C�g���[�V���O�̑ΏۂƂȂ�Renderer���擾
        var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.InstanceID);
        // ����܂ł�Instance���N���A
        _accelerationStructure.ClearInstances();
        foreach (var renderer in renderers)
        {
            // ���C�g���[�V���O�̑ΏۂƂȂ�Renderer��o�^
            _accelerationStructure.AddInstance(renderer, flags.ToArray());
        }
        // �r���h
        _accelerationStructure.Build();
        // �r���h����AccelerationStructure��Shader�ɐݒ�
        rayTracingShader.SetAccelerationStructure(_resIdxWorld, _accelerationStructure);
    }

    bool UpdateResources(int width_, int height_)
    {
        bool updateAS     = _updateAS;
        bool updateFrame  = false;
        bool isDirty      = _dirtyAS;
        var viewMatrix    = Camera.main.worldToCameraMatrix;
        var projMatrix    = Camera.main.projectionMatrix;
        if (viewMatrix   !=_prevViewMatrix || projMatrix != _prevProjMatrix)
        {
            updateFrame     = true;
            _prevViewMatrix = viewMatrix;
            _prevProjMatrix = projMatrix;
        }
        if (_tonemapping != tonemapping)
        {
            _tonemapping = tonemapping;
        }
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

        if (_dirtyAS)
        {
            _updateAS = true;
            _dirtyAS  = false;
        }
        if (updateAS)
        {
            BuildAccelerationStructure();
            _updateAS = false;
            isDirty   = true;
        }
        Resize(width_, height_, updateFrame);
        if (updateFrame || updateAS)
        {
            _accumeSamples = 0;
        }
        return isDirty;
    }
    private void CopySkybox(RenderTexture dstCubemap)
    {
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
        if (rayTracingShader == null)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            if (!UpdateResources(source.width, source.height))
            {

                if (_dispatchSamples != dispatchSamples)
                {
                    _dispatchSamples = dispatchSamples;
                }
                RenderTextureDescriptor cubeRTDesc = new RenderTextureDescriptor(512, 512, RenderTextureFormat.ARGBFloat, 0, 0, RenderTextureReadWrite.Linear)
                {
                    dimension = UnityEngine.Rendering.TextureDimension.Cube

                };
                // �Ȃ���RayTracingShader���ł�, unity_SpecCube0��������Ȃ��ƃG���[���o��̂�, �����ňꎞ�I�ɃR�s�[���쐬
                RenderTexture skyCubeRT = RenderTexture.GetTemporary(cubeRTDesc);
                CopySkybox(skyCubeRT);
                // ���C�g���[�V���O�V�F�[�_�[�ɕK�v�ȃp�����[�^��ݒ�
                // �قƂ�ǃR���s���[�g�Ɠ���
                rayTracingShader.SetTexture(_resIdxSkybox     , skyCubeRT);
                rayTracingShader.SetInt(_resIdxAccumeSamples  , _accumeSamples);
                rayTracingShader.SetInt(_resIdxDispatchSamples, _dispatchSamples);
                rayTracingShader.SetInt(_resIdxBackgroundMode, (int)backgroundMode);
                rayTracingShader.SetVector(_resIdxBackgroundColor, new Vector4(backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundColor.a));
                // ���C�g���[�V���O�V�F�[�_�[�̃p�X��ݒ�(�K�{)
                // �����ł�PathTrace�Ƃ������O�̃p�X���g�p
                // ���̖��O��AccelerationStructure�̃r���h���ɓ��͂���Renderer��Material�̃p�X���ɑΉ����Ă���
                // ����, �p�X�����܂܂�Ă��Ȃ��ꍇ, ���̃}�e���A���ł̓V�F�[�_�����s����Ȃ��̂Œ���
                rayTracingShader.SetShaderPass("SimplePathTracer");
                // ���C�g���[�V���O�����s
                rayTracingShader.Dispatch("RayGenShaderForSensor", _outputTexture.width, _outputTexture.height, 1, Camera.main);
                // �g�[���}�b�s���O(Reinhard Tonemapping)�����s
                // �{���I�ȕ����ł͂Ȃ��̂Ŗ������Ă�����Ă��\��Ȃ�
                if (_tonemapping)
                {
                    // Reinhard Tonemapping
                    RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(_outputTexture.width, _outputTexture.height, RenderTextureFormat.ARGBFloat, 0, Texture.GenerateAllMips);
                    // Reinhard Tonemapping�ł͋P�x�l�̕��ϒl�����߂�K�v�����邪, ���ʂɖʓ|�Ȃ̂�Mipmap0�ő�p
                    rtDesc.useMipMap = true;
                    RenderTexture luminanceRT = RenderTexture.GetTemporary(rtDesc);
                    // �P�x�l�̌v�Z
                    Graphics.Blit(_outputTexture, luminanceRT, luminanceMaterial);
                    tonemapMaterial.SetTexture("_LuminanceAverageTex", luminanceRT);
                    tonemapMaterial.SetColor("_WhiteColor", whiteColor);
                    tonemapMaterial.SetFloat("_WhiteIntensity", whiteIntensity);
                    tonemapMaterial.SetFloat("_Exposure", exposure);
                    Graphics.Blit(_outputTexture, destination, tonemapMaterial);
                    // �J������
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
                // ��������̗��R�Ń��C�g���[�V���O�̏������s���Ȃ������ꍇ, ���̂܂܃R�s�[
                Graphics.Blit(_outputTexture, destination);
            }
        }
    }
}
