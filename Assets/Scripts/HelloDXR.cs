using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class HelloDXR : MonoBehaviour
{
    enum ProceduralGeometryType
    {
        None,
        Sphere,
        Cube,
        Plane,
    }
    [SerializeField]
    private RayTracingShader rayTracingShader = null;
    private RayTracingAccelerationStructure _accelerationStructure = null;
    private RenderTexture _renderTarget = null;

    private Matrix4x4 _prevViewMatrix = Matrix4x4.identity;
    private Matrix4x4 _prevProjMatrix = Matrix4x4.identity;

    private int _resIdxRenderTarget = 0;
    private int _resIdxWorld = 0;

    const string kTargetShaderPass = "HelloDXR";
    const string kRayGenShaderName = "RayGenShaderForTest";

    private void OnEnable()
    {
        if (rayTracingShader == null)
        {
            return;
        }
        _resIdxRenderTarget = Shader.PropertyToID("RenderTarget");
        _resIdxWorld = Shader.PropertyToID("World");
        CreateAccelerationStructure();
        _renderTarget = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _renderTarget.enableRandomWrite = true;
        _renderTarget.Create();
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
            rayTracingShader.SetShaderPass(kTargetShaderPass);
            rayTracingShader.SetTexture(_resIdxRenderTarget, _renderTarget);
            rayTracingShader.SetAccelerationStructure(_resIdxWorld, _accelerationStructure);
            rayTracingShader.Dispatch(kRayGenShaderName, Screen.width, Screen.height, 1, Camera.main);
            Graphics.Blit(_renderTarget, destination);
        }
    }
    void UpdateResources(int width_, int height_)
    {
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
        }
        else if (_renderTarget == null)
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
        // ƒrƒ‹ƒh
        BuildAccelerationStructure();
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
        if (_accelerationStructure == null)
        {
            CreateAccelerationStructure();
        }
        _accelerationStructure.Build();
    }
}
