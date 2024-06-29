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
    const string kTargetShaderPass = "HelloDXR";
    const string kRayGenShaderName = "RayGenShaderForTest";
    private int _resIdxRenderTarget = 0;
    private int _resIdxWorld = 0;
    private bool _dirtyAS = false;

    private void OnEnable()
    {
        _resIdxRenderTarget = Shader.PropertyToID("RenderTarget");
        _resIdxWorld = Shader.PropertyToID("World");
        _accelerationStructure = new RayTracingAccelerationStructure();
        _renderTarget = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _renderTarget.enableRandomWrite = true;
        _renderTarget.Create();
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
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (rayTracingShader == null)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            UpdateResources(source.width, source.height);
            {
                rayTracingShader.SetShaderPass(kTargetShaderPass);
                rayTracingShader.SetTexture(_resIdxRenderTarget, _renderTarget);
                rayTracingShader.SetAccelerationStructure(_resIdxWorld, _accelerationStructure);
                rayTracingShader.Dispatch(kRayGenShaderName, Screen.width, Screen.height, 1, Camera.main);
                Graphics.Blit(_renderTarget, destination);
            }
        }
    }
    void Resize(int width_, int height_, bool clear_ = false)
    {
        if (_renderTarget.width == width_ && _renderTarget.height == height_)
        {
            _renderTarget.Release();
            _renderTarget.width = width_;
            _renderTarget.height = height_;
            _renderTarget.Create();
            return;
        }
        return;
    }

    public void MarkDirty()
    {
        _dirtyAS = true;
    }

    void BuildAccelerationStructure()
    {
        var flags = new List<RayTracingSubMeshFlags>(new RayTracingSubMeshFlags[1] { RayTracingSubMeshFlags.Enabled });
        var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
        _accelerationStructure.ClearInstances();
        foreach (var renderer in renderers)
        {
            _accelerationStructure.AddInstance(renderer, flags.ToArray());
        }
        _accelerationStructure.Build();
        rayTracingShader.SetAccelerationStructure(_resIdxWorld, _accelerationStructure);
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
    bool UpdateAccelerationStructures()
    {
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
        Resize(width_, height_, updateFrame);
    }
}
