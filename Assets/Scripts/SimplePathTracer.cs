using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


[RequireComponent(typeof(Camera))]
public class SimplePathTracer : MonoBehaviour
{
    enum BackgroundMode
    {
        Skybox,
        SolidColor
    }
    [SerializeField]
    private RayTracingShader rayTracingShader = null;
    private RenderTexture _outputTexture = null;
    private RenderTexture _accumeTexture = null;
    private RayTracingAccelerationStructure _accelerationStructure = null;
    private GraphicsBuffer _randomBuffer = null;
    private Matrix4x4 _prevViewMatrix = Matrix4x4.identity;
    private Matrix4x4 _prevProjMatrix = Matrix4x4.identity;
    private int _accumeSamples = 0;
    [SerializeField]
    private int dispatchSamples = 1;
    private int _dispatchSamples = 1;
    [SerializeField]
    private BackgroundMode backgroundMode = BackgroundMode.Skybox;
    private BackgroundMode _prevBackgroundMode = BackgroundMode.Skybox;
    [SerializeField]
    private Color backgroundColor = Color.black;
    private Color _backgroundColor = Color.black;
    [SerializeField]
    private bool tonemapping = true;
    private bool _tonemapping = true;
    [SerializeField]
    private Color whiteColor = Color.white;
    [SerializeField, Range(0.01F, 100.0F)]
    private float whiteIntensity = 1.0f;
    [SerializeField, Range(0.001F, 1.0F)]
    private float exposure = 1.0f;
    private Material _luminanceMaterial = null;
    private Material _TonemapMaterial = null;
    private Material _copySkyboxMaterial = null;
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
    const string kTargetShaderPass = "SimplePathTracer";
    const string kRayGenShaderName = "RayGenShaderForSensor";
    private int _resIdxRenderTarget = 0;
    private int _resIdxAccumeTarget = 0;
    private int _resIdxRandomBuffer = 0;
    private int _resIdxWorld = 0;
    private int _resIdxAccumeSamples = 0;
    private int _resIdxSkybox = 0;
    private int _resIdxDispatchSamples = 0;
    private int _resIdxBackgroundMode = 0;
    private int _resIdxBackgroundColor = 0;
    private int _resIdxLuminanceAverageTex = 0;
    private int _resIdxWhiteColor = 0;
    private int _resIdxWhiteIntensity = 0;
    private int _resIdxExposure = 0;
    private bool _dirtyAS = false;
    private bool supportRayTracing
    {
        get
        {
            return SystemInfo.supportsRayTracing && SystemInfo.supportsRayTracingShaders;
        }
    }
    private void OnEnable()
    {
        if (!supportRayTracing){return;}
        _resIdxRenderTarget = Shader.PropertyToID("RenderTarget");
        _resIdxAccumeTarget = Shader.PropertyToID("AccumeTarget");
        _resIdxRandomBuffer = Shader.PropertyToID("RandomBuffer");
        _resIdxWorld = Shader.PropertyToID("World");
        _resIdxAccumeSamples = Shader.PropertyToID("AccumeSamples");
        _resIdxDispatchSamples = Shader.PropertyToID("DispatchSamples");
        _resIdxSkybox = Shader.PropertyToID("Skybox");
        _resIdxBackgroundMode = Shader.PropertyToID("BackgroundMode");
        _resIdxBackgroundColor = Shader.PropertyToID("BackgroundColor");
        _resIdxLuminanceAverageTex = Shader.PropertyToID("_LuminanceAverageTex");
        _resIdxWhiteColor = Shader.PropertyToID("_WhiteColor");
        _resIdxWhiteIntensity = Shader.PropertyToID("_WhiteIntensity");
        _resIdxExposure = Shader.PropertyToID("_Exposure");
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
        CreateAccelerationStructure();
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

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!supportRayTracing)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            UpdateResources(source.width, source.height);
            RenderTexture skyCubeRT = RenderTexture.GetTemporary(new RenderTextureDescriptor(512, 512, RenderTextureFormat.ARGBFloat, 0, 0, RenderTextureReadWrite.Linear)
            {
                dimension = UnityEngine.Rendering.TextureDimension.Cube
            });
            CopySkybox(skyCubeRT);
            TraceRay(skyCubeRT);
            if (!ExecuteToneMapping(_outputTexture, destination)) { Graphics.Blit(_outputTexture, destination); }
            RenderTexture.ReleaseTemporary(skyCubeRT);
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
                RenderTexture.active = _accumeTexture;
                GL.Clear(true, true, Color.black);
                RenderTexture.active = activeRT; 
                _accumeSamples = 0;
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
        rayTracingShader.SetBuffer(_resIdxRandomBuffer, _randomBuffer);
        rayTracingShader.SetInt(_resIdxAccumeSamples, _accumeSamples);
        rayTracingShader.SetInt(_resIdxDispatchSamples, _dispatchSamples);

    }
    void UpdateShaderParameters()
    {
        if (rayTracingShader == null) { return; }
        rayTracingShader.SetTexture(_resIdxRenderTarget, _outputTexture);
        rayTracingShader.SetTexture(_resIdxAccumeTarget, _accumeTexture);
        rayTracingShader.SetBuffer(_resIdxRandomBuffer, _randomBuffer);
    }
    bool UpdateCamera()
    {
        var camera = gameObject.GetComponent<Camera>();
        if (camera == null) { return false; }
        var viewMatrix = camera.worldToCameraMatrix;
        var projMatrix = camera.projectionMatrix;
        if (viewMatrix != _prevViewMatrix || projMatrix != _prevProjMatrix)
        {
            _prevViewMatrix = viewMatrix;
            _prevProjMatrix = projMatrix;
            return true;
        }
        return false;
    }
    bool UpdateBackground()
    {
        bool updateFrame = false;
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
        return updateFrame;
    }
    void CreateAccelerationStructure()
    {
        _accelerationStructure = new RayTracingAccelerationStructure(new RayTracingAccelerationStructure.Settings
        {
            layerMask = 255,
            managementMode = RayTracingAccelerationStructure.ManagementMode.Automatic,
            rayTracingModeMask = RayTracingAccelerationStructure.RayTracingModeMask.Everything
        });
    }
    void BuildAccelerationStructure()
    {
        if (_accelerationStructure != null)
        {
            CreateAccelerationStructure();
        }
        _accelerationStructure.Build();
    }
    void UpdateResources(int width_, int height_)
    {
        bool updateFrame = false;
        if (UpdateCamera())
        {
            updateFrame = true;
        }
        if (UpdateBackground())
        {
            updateFrame = true;
        }
        BuildAccelerationStructure();
        Resize(width_, height_, updateFrame);
    }
    void CopySkybox(RenderTexture dstCubemap)
    {
        RenderTexture tempFaceRT = RenderTexture.GetTemporary(dstCubemap.width, dstCubemap.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        RenderTexture activeRT = RenderTexture.active;
        for (int i = 0; i < 6; i++)
        {
            Graphics.Blit(null, tempFaceRT, copySkyboxMaterial, i);
            Graphics.CopyTexture(tempFaceRT, 0, 0, dstCubemap, i, 0);
        }
        RenderTexture.active = activeRT;
        RenderTexture.ReleaseTemporary(tempFaceRT);
    }
    void TraceRay(RenderTexture skybox)
    {
        if (_dispatchSamples != dispatchSamples) { _dispatchSamples = dispatchSamples; }
        rayTracingShader.SetTexture(_resIdxSkybox, skybox);
        rayTracingShader.SetInt(_resIdxAccumeSamples, _accumeSamples);
        rayTracingShader.SetInt(_resIdxDispatchSamples, _dispatchSamples);
        rayTracingShader.SetInt(_resIdxBackgroundMode, (int)backgroundMode);
        rayTracingShader.SetVector(_resIdxBackgroundColor, new Vector4(backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundColor.a));
        rayTracingShader.SetShaderPass(kTargetShaderPass);
        rayTracingShader.Dispatch(kRayGenShaderName, _outputTexture.width, _outputTexture.height, 1, Camera.main);
        _accumeSamples = _accumeSamples + _dispatchSamples;
    }
    bool ExecuteToneMapping(RenderTexture source, RenderTexture destination)
    {
        if (_tonemapping != tonemapping)
        {
            _tonemapping = tonemapping;
        }
        if (_tonemapping)
        {
            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(source.width, source.height, RenderTextureFormat.ARGBFloat, 0, Texture.GenerateAllMips);
            rtDesc.useMipMap = true;
            RenderTexture luminanceRT = RenderTexture.GetTemporary(rtDesc);
            Graphics.Blit(source, luminanceRT, luminanceMaterial);
            tonemapMaterial.SetTexture(_resIdxLuminanceAverageTex, luminanceRT);
            tonemapMaterial.SetColor(_resIdxWhiteColor, whiteColor);
            tonemapMaterial.SetFloat(_resIdxWhiteIntensity, whiteIntensity);
            tonemapMaterial.SetFloat(_resIdxExposure, exposure);
            Graphics.Blit(source, destination, tonemapMaterial);
            RenderTexture.ReleaseTemporary(luminanceRT);
            return true;
        }
        return false;
    }
}
