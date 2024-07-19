using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


[RequireComponent(typeof(Camera))]
public class SimplePathTracerOmnidir : MonoBehaviour
{
    enum BackgroundMode
    {
        Skybox = 0,
        SolidColor = 1
    }
    enum TonemapMode
    {
        None,
        Linear,
        Reinhard,
        Uncharted2,
        ACES
    }

    static BackgroundMode ConvertToBackgroundMode(CameraClearFlags clearFlags)
    {
        if (clearFlags == CameraClearFlags.Skybox)
        {
            return BackgroundMode.Skybox;
        }
        return BackgroundMode.SolidColor;
    }
    const string kTargetShaderPass = "SimplePathTracer";
    const string kRayGenShaderName = "RayGenShaderForSensor";
    [SerializeField]
    private RayTracingShader rayTracingShader = null;
    private RenderTexture _outputTexture = null;
    private RenderTexture _accumeTexture = null;
    private RayTracingAccelerationStructure _accelerationStructure = null;
    private GraphicsBuffer _randomBuffer = null;
    private Camera _camera = null;
    Camera targetCamera
    {
        get
        {
            if (_camera == null)
            {
                _camera = gameObject.GetComponent<Camera>();
                if (_camera == null)
                {
                    _camera = gameObject.AddComponent<Camera>();
                }
            }
            return _camera;
        }
    }
    private BackgroundMode _prevBackgroundMode = BackgroundMode.Skybox;
    private Color _prevBackgroundColor = Color.black;
    private int _accumeSamples = 0;
    [SerializeField]
    private int dispatchSamples = 1;
    private int _dispatchSamples = 1;
    [SerializeField]
    private TonemapMode tonemapMode = TonemapMode.None;
    private TonemapMode _prevTonemapMode = TonemapMode.None;
    [SerializeField, Range(0.001F, 0.5F)]
    private float exposure = 0.23F;
    private Material _luminanceMaterial = null;
    private Material _luminanceHistogramMaterial = null;
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
    Material luminanceHistogramMaterial
    {
        get
        {
            if (_luminanceHistogramMaterial == null)
            {
                _luminanceHistogramMaterial = new Material(Shader.Find("Hidden/LuminanceHistogram"));
            }
            return _luminanceHistogramMaterial;
        }
    }
    Material tonemapMaterial
    {
        get
        {
            if (_TonemapMaterial == null)
            {
                _TonemapMaterial = new Material(Shader.Find("Hidden/Tonemap"));
                _TonemapMaterial.EnableKeyword("_USE_SMALL_TEXTURE");
                var currTonemapMode = tonemapMode;
                if (currTonemapMode == TonemapMode.Linear)
                {
                    tonemapMaterial.EnableKeyword("_TONEMODE_LINEAR");
                    tonemapMaterial.DisableKeyword("_TONEMODE_REINHARD");
                    tonemapMaterial.DisableKeyword("_TONEMODE_UNCHARTED2");
                    tonemapMaterial.DisableKeyword("_TONEMODE_ACES");
                }
                else if (currTonemapMode == TonemapMode.Reinhard)
                {
                    tonemapMaterial.EnableKeyword("_TONEMODE_REINHARD");
                    tonemapMaterial.DisableKeyword("_TONEMODE_LINEAR");
                    tonemapMaterial.DisableKeyword("_TONEMODE_UNCHARTED2");
                    tonemapMaterial.DisableKeyword("_TONEMODE_ACES");
                }
                else if (currTonemapMode == TonemapMode.Uncharted2)
                {
                    Debug.Log("Tonemap Uncharted is Not Implemanted Yet!");
                    tonemapMaterial.EnableKeyword("_TONEMODE_UNCHARTED2");
                    tonemapMaterial.DisableKeyword("_TONEMODE_REINHARD");
                    tonemapMaterial.DisableKeyword("_TONEMODE_LINEAR");
                    tonemapMaterial.DisableKeyword("_TONEMODE_ACES");
                }
                else if (currTonemapMode == TonemapMode.ACES)
                {
                    tonemapMaterial.EnableKeyword("_TONEMODE_ACES");
                    tonemapMaterial.DisableKeyword("_TONEMODE_REINHARD");
                    tonemapMaterial.DisableKeyword("_TONEMODE_LINEAR");
                    tonemapMaterial.DisableKeyword("_TONEMODE_UNCHARTED2");
                }
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
    [SerializeField]
    private bool _visLuminance = false;
    [SerializeField]
    private Material _LUTMaterial;
    [SerializeField]
    private Vector3 cameraPosition = new Vector3(0.0f, 0.0f, 0.0f);
    private Vector3 _cameraPosition;
    [SerializeField]
    private Vector3 cameraNormal = new Vector3(0.0f, 0.0f, 1.0f);
    private Vector3 _cameraNormal;
    private int _resIdxRenderTarget = 0;
    private int _resIdxAccumeTarget = 0;
    private int _resIdxRandomBuffer = 0;
    private int _resIdxWorld = 0;
    private int _resIdxAccumeSamples = 0;
    private int _resIdxSkybox = 0;
    private int _resIdxDispatchSamples = 0;
    private int _resIdxBackgroundMode = 0;
    private int _resIdxBackgroundColor = 0;
    private int _resIdxOmnidirCameraPosition = 0;
    private int _resIdxOmnidirCameraNormal = 0;
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
        if (!supportRayTracing) { return; }
        _resIdxRenderTarget = Shader.PropertyToID("RenderTarget");
        _resIdxAccumeTarget = Shader.PropertyToID("AccumeTarget");
        _resIdxRandomBuffer = Shader.PropertyToID("RandomBuffer");
        _resIdxWorld = Shader.PropertyToID("World");
        _resIdxAccumeSamples = Shader.PropertyToID("AccumeSamples");
        _resIdxDispatchSamples = Shader.PropertyToID("DispatchSamples");
        _resIdxSkybox = Shader.PropertyToID("Skybox");
        _resIdxBackgroundMode = Shader.PropertyToID("BackgroundMode");
        _resIdxBackgroundColor = Shader.PropertyToID("BackgroundColor");
        _resIdxOmnidirCameraPosition = Shader.PropertyToID("OmnidirCameraPosition");
        _resIdxOmnidirCameraNormal = Shader.PropertyToID("OmnidirCameraNormal");
        _prevBackgroundMode = ConvertToBackgroundMode(targetCamera.clearFlags);
        _prevBackgroundColor = targetCamera.backgroundColor;
        _prevTonemapMode = tonemapMode;
        _cameraPosition = cameraPosition;
        _cameraNormal = cameraNormal;
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

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!supportRayTracing)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            UpdateResources(source.width, source.height);
            RenderTexture skyCubeRT = RenderTexture.GetTemporary(new RenderTextureDescriptor(512, 512, RenderTextureFormat.ARGBFloat, 0, 0)
            {
                dimension = UnityEngine.Rendering.TextureDimension.Cube
            });
            CopySkybox(skyCubeRT);
            TraceRay(skyCubeRT);
            if (_visLuminance)
            {
                RenderTexture luminanceRT = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                Graphics.Blit(_outputTexture, luminanceRT, luminanceMaterial);
                Graphics.Blit(luminanceRT, destination, _LUTMaterial);
                RenderTexture.ReleaseTemporary(luminanceRT);
            }
            else if (!ExecuteToneMapping(_outputTexture, destination))
            {
                Graphics.Blit(_outputTexture, destination);
            }
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
    void BuildAccelerationStructure()
    {
        var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
        _accelerationStructure.ClearInstances();
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;
            if (materials != null)
            {
                int i = 0;
                var flags = new RayTracingSubMeshFlags[materials.Length];
                foreach (var material in materials)
                {
                    flags[i] = RayTracingSubMeshFlags.Enabled;
                    ++i;
                }
                _accelerationStructure.AddInstance(renderer, flags);
            }
        }
        _accelerationStructure.Build();
        rayTracingShader.SetAccelerationStructure(_resIdxWorld, _accelerationStructure);
    }
    bool UpdateCamera()
    {
        if (targetCamera == null) { return false; }
        if (_cameraPosition != cameraPosition)
        {
            _cameraPosition = cameraPosition;
            return true;
        }
        if (_cameraNormal != cameraNormal)
        {
            _cameraNormal = cameraNormal;
            return true;
        }
        return false;
    }
    bool UpdateBackground()
    {
        bool updateFrame = false;
        var backgroundMode = ConvertToBackgroundMode(targetCamera.clearFlags);
        if (backgroundMode != _prevBackgroundMode)
        {
            _prevBackgroundMode = backgroundMode;
            updateFrame = true;
        }
        var backgroundColor = targetCamera.backgroundColor;
        if (_prevBackgroundMode == BackgroundMode.SolidColor)
        {
            if (backgroundColor != _prevBackgroundColor)
            {
                _prevBackgroundColor = backgroundColor;
                updateFrame = true;
            }
        }
        return updateFrame;
    }
    bool UpdateAccelerationStructures()
    {
        // SceneÇ…ïœçXÇ™Ç†Ç¡ÇΩÇ∆Ç´ÇÃÇ›ASÇçƒç\ízÇ∑ÇÈ
        if (_dirtyAS)
        {
            BuildAccelerationStructure();
            _dirtyAS = false;
            return true;
        }
        return false;
    }
    void UpdateResources(int width_, int height_)
    {
        bool updateFrame = false;
        if (UpdateAccelerationStructures())
        {
            updateFrame = true;
        }
        if (UpdateCamera())
        {
            updateFrame = true;
        }
        if (UpdateBackground())
        {
            updateFrame = true;
        }
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
        rayTracingShader.SetVector(_resIdxOmnidirCameraPosition, new Vector4(_cameraPosition.x, _cameraPosition.y, _cameraPosition.z, 0.0f));
        rayTracingShader.SetVector(_resIdxOmnidirCameraNormal, new Vector4(_cameraNormal.x, _cameraNormal.y, _cameraNormal.z, 0.0f));
        rayTracingShader.SetTexture(_resIdxSkybox, skybox);
        rayTracingShader.SetInt(_resIdxAccumeSamples, _accumeSamples);
        rayTracingShader.SetInt(_resIdxDispatchSamples, _dispatchSamples);
        rayTracingShader.SetInt(_resIdxBackgroundMode, (int)_prevBackgroundMode);
        rayTracingShader.SetVector(_resIdxBackgroundColor, new Vector4(_prevBackgroundColor.r, _prevBackgroundColor.g, _prevBackgroundColor.b, _prevBackgroundColor.a));
        rayTracingShader.SetShaderPass(kTargetShaderPass);
        rayTracingShader.Dispatch(kRayGenShaderName, _outputTexture.width, _outputTexture.height, 1);
        _accumeSamples = _accumeSamples + _dispatchSamples;
    }
    bool ExecuteToneMapping(RenderTexture source, RenderTexture destination)
    {
        var prevTonemapMode = _prevTonemapMode;
        var currTonemapMode = tonemapMode;
        if (_prevTonemapMode != tonemapMode)
        {
            _prevTonemapMode = tonemapMode;
        }
        if (currTonemapMode == TonemapMode.None)
        {
            return false;
        }
        {
            RenderTexture luminanceRT = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, luminanceRT, luminanceMaterial);
            // TODO: GPUÇ≈ÇÃåvéZÇ…ïœçX
            RenderTexture smallRT = null;
            {
                int requiredTexCount = Mathf.CeilToInt(Mathf.Log(Mathf.Max(source.width, source.height), 8));
                RenderTexture[] luminanceMipRTs = new RenderTexture[requiredTexCount];
                for (int i = 0; i < requiredTexCount; i++)
                {
                    luminanceMipRTs[i] = RenderTexture.GetTemporary(Mathf.Max(source.width >> (3 * (i + 1)), 1), Mathf.Max(source.height >> (3 * (i + 1)), 1), 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                }
                Graphics.Blit(luminanceRT, luminanceMipRTs[0], luminanceHistogramMaterial);
                for (int i = 0; i < requiredTexCount - 1; i++)
                {
                    Graphics.Blit(luminanceMipRTs[i], luminanceMipRTs[i + 1], luminanceHistogramMaterial);
                }
                smallRT = luminanceMipRTs[requiredTexCount - 1];
                for (int i = 0; i < requiredTexCount - 1; i++)
                {
                    RenderTexture.ReleaseTemporary(luminanceMipRTs[i]);
                }
                tonemapMaterial.SetTexture("_SmallTex", smallRT);
                tonemapMaterial.SetFloat("_Exposure", exposure);
            }
            if (currTonemapMode != prevTonemapMode)
            {
                if (currTonemapMode == TonemapMode.Linear)
                {
                    tonemapMaterial.EnableKeyword("_TONEMODE_LINEAR");
                    tonemapMaterial.DisableKeyword("_TONEMODE_REINHARD");
                    tonemapMaterial.DisableKeyword("_TONEMODE_UNCHARTED2");
                    tonemapMaterial.DisableKeyword("_TONEMODE_ACES");
                }
                else if (currTonemapMode == TonemapMode.Reinhard)
                {
                    tonemapMaterial.EnableKeyword("_TONEMODE_REINHARD");
                    tonemapMaterial.DisableKeyword("_TONEMODE_LINEAR");
                    tonemapMaterial.DisableKeyword("_TONEMODE_UNCHARTED2");
                    tonemapMaterial.DisableKeyword("_TONEMODE_ACES");
                }
                else if (currTonemapMode == TonemapMode.Uncharted2)
                {
                    Debug.Log("Tonemap Uncharted is Not Implemanted Yet!");
                    tonemapMaterial.EnableKeyword("_TONEMODE_UNCHARTED2");
                    tonemapMaterial.DisableKeyword("_TONEMODE_REINHARD");
                    tonemapMaterial.DisableKeyword("_TONEMODE_LINEAR");
                    tonemapMaterial.DisableKeyword("_TONEMODE_ACES");
                }
                else if (currTonemapMode == TonemapMode.ACES)
                {
                    tonemapMaterial.EnableKeyword("_TONEMODE_ACES");
                    tonemapMaterial.DisableKeyword("_TONEMODE_REINHARD");
                    tonemapMaterial.DisableKeyword("_TONEMODE_LINEAR");
                    tonemapMaterial.DisableKeyword("_TONEMODE_UNCHARTED2");
                }
            }
            Graphics.Blit(source, destination, tonemapMaterial);
            if (smallRT != null)
            {
                RenderTexture.ReleaseTemporary(smallRT);
            }
            RenderTexture.ReleaseTemporary(luminanceRT);
            return true;
        }
    }
}
