using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class HelloProcedural : MonoBehaviour
{
    // ���̂̃v���~�e�B�u�^�C�v
    const int kProceduralPrimitiveTypeSphere = 1;
    // HelloDXR�Ɠ���
    [SerializeField]
    private RayTracingShader rayTracingShader = null;
    private RayTracingAccelerationStructure _accelerationStructure = null;
    private RenderTexture _renderTarget = null;
    private Matrix4x4 _prevViewMatrix = Matrix4x4.identity;
    private Matrix4x4 _prevProjMatrix = Matrix4x4.identity;
    private int _resIdxRenderTarget = 0;
    private int _resIdxWorld = 0;
    private bool _dirtyAS = false;
    // ���̊֘A
    [SerializeField]
    private Vector3 sphereCenter = new Vector3(0.0f, 0.0f, 0.0f);
    private Vector3 _sphereCenter = new Vector3(0.0f, 0.0f, 0.0f);
    [SerializeField]
    private float sphereRadius = 1.0f;
    private float _sphereRadius = 1.0f;
    private GraphicsBuffer _aabbBuffer = null;
    private Material _proceduralMaterial = null;
    private Material proceduralMaterial
    {
        get
        {
            if (_proceduralMaterial == null)
            {
                _proceduralMaterial = new Material(Shader.Find("Unlit/HelloProcedural"));
            }
            return _proceduralMaterial;
        }
    }
    private bool supportRayTracing
    {
        get
        {
            return SystemInfo.supportsRayTracing && SystemInfo.supportsRayTracingShaders && rayTracingShader != null;
        }
    }
    public void MarkDirty()
    {
        _dirtyAS = true;
    }
    private void OnEnable()
    {
        _resIdxRenderTarget = Shader.PropertyToID("RenderTarget");
        _resIdxWorld = Shader.PropertyToID("World");
        _accelerationStructure = new RayTracingAccelerationStructure();
        _renderTarget = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _renderTarget.enableRandomWrite = true;
        _renderTarget.Create();

        _sphereRadius = sphereRadius;
        _sphereCenter = sphereCenter;

        _aabbBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 24);
        var aabbData = new Vector3[2];
        aabbData[0] = _sphereCenter + new Vector3(-_sphereRadius, -_sphereRadius, -_sphereRadius);
        aabbData[1] = _sphereCenter + new Vector3(_sphereRadius, _sphereRadius, _sphereRadius);
        _aabbBuffer.SetData(aabbData);

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
        if (_aabbBuffer != null)
        {
            _aabbBuffer.Release();
            _aabbBuffer = null;
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
            UpdateFrameResources(source.width, source.height);
            UpdateAccelerationStructures();
            rayTracingShader.SetShaderPass("HelloProcedural");
            rayTracingShader.SetTexture(_resIdxRenderTarget, _renderTarget);
            rayTracingShader.SetAccelerationStructure(_resIdxWorld, _accelerationStructure);
            rayTracingShader.Dispatch("RayGenShaderForTest", Screen.width, Screen.height, 1, Camera.main);
            Graphics.Blit(_renderTarget, destination);
        }
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
        {
            var geometryConfig = new RayTracingAABBsInstanceConfig();
            geometryConfig.aabbBuffer = _aabbBuffer;
            geometryConfig.aabbCount = 1;
            geometryConfig.aabbOffset = 0;
            geometryConfig.accelerationStructureBuildFlags = RayTracingAccelerationStructureBuildFlags.PreferFastBuild;
            geometryConfig.accelerationStructureBuildFlagsOverride = false;
            geometryConfig.layer = 0;
            geometryConfig.mask = 0xff;
            geometryConfig.dynamicGeometry = false;
            geometryConfig.material = proceduralMaterial;

            var spherePropertyBlock = new MaterialPropertyBlock();
            spherePropertyBlock.SetInt("_ProceduralPrimitiveType", kProceduralPrimitiveTypeSphere);
            spherePropertyBlock.SetVector("_SphereCenter", _sphereCenter);
            spherePropertyBlock.SetFloat("_SphereRadius", _sphereRadius);
            geometryConfig.materialProperties = spherePropertyBlock;
            geometryConfig.opaqueMaterial = true;
            _accelerationStructure.AddInstance(geometryConfig, Matrix4x4.identity);
        }
        _accelerationStructure.Build();
    }
    bool UpdateProceduralSphere()
    {
        if (_sphereCenter != sphereCenter || _sphereRadius != sphereRadius)
        {
            _sphereCenter = sphereCenter;
            _sphereRadius = sphereRadius;
            var aabbData = new Vector3[2];
            aabbData[0] = _sphereCenter + new Vector3(-_sphereRadius, -_sphereRadius, -_sphereRadius);
            aabbData[1] = _sphereCenter + new Vector3(_sphereRadius, _sphereRadius, _sphereRadius);
            _aabbBuffer.SetData(aabbData);
            return true;
        }
        return false;
    }
    void UpdateAccelerationStructures()
    {
        bool isDirtyAS = _dirtyAS;
        if (UpdateProceduralSphere())
        {
            isDirtyAS = true;
        }
        if (isDirtyAS)
        {
            BuildAccelerationStructure();
            _dirtyAS = false;
        }
    }
    void UpdateFrameResources(int width_, int height_)
    {
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
        else
        {
            _renderTarget = new RenderTexture(width_, height_, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _renderTarget.enableRandomWrite = true;
            _renderTarget.Create();
        }
        var viewMatrix = Camera.main.worldToCameraMatrix;
        var projMatrix = Camera.main.projectionMatrix;
        if (viewMatrix != _prevViewMatrix || projMatrix != _prevProjMatrix)
        {
            _prevViewMatrix = viewMatrix;
            _prevProjMatrix = projMatrix;
        }
    }

}
