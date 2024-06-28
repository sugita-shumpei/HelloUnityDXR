using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// ランタイム中に光源を変更しても元に戻せるようにするためのスクリプト
public class AreaLightController : MonoBehaviour
{
    [SerializeField] private Color color     = Color.white;
    [SerializeField] private float intensity = 1.0f;

    private Color _initialColor     = Color.white;
    private float _initialIntensity = 1.0f;
    private bool  _isInitialized    = false;

    public void Initialize(Color color, float intensity)
    {
        this.color        = color;
        this.intensity    = intensity;
        _initialColor     = color;
        _initialIntensity = intensity;
        _isInitialized    = true;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        var renderer = GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            return;
        }
        renderer.sharedMaterial.SetColor("_EmissionColor"    , color);
        renderer.sharedMaterial.SetFloat("_EmissionIntensity", intensity);
    }

    void OnEnable()
    {
        if (!_isInitialized)
        {
            return;
        }
        var renderer = GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            return;
        }
        this.Initialize(color, intensity);
        renderer.sharedMaterial.SetColor("_EmissionColor", color);
        renderer.sharedMaterial.SetFloat("_EmissionIntensity", intensity);
    }
    void OnDisable()
    {
        var renderer = GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            return;
        }
        color = _initialColor;
        intensity = _initialIntensity;
        renderer.sharedMaterial.SetColor("_EmissionColor"    , _initialColor);
        renderer.sharedMaterial.SetFloat("_EmissionIntensity", _initialIntensity);
        _isInitialized = false;
    }
}
