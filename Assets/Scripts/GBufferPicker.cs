using UnityEngine;

public class GBufferPicker : MonoBehaviour
{
    private Texture2D _gBufferColor;
    private Texture2D _gBufferPosition;
    private Texture2D _gBufferNormal;
    private Texture2D _gBufferDepth;

    private Texture2D _Tex1x1Color;
    private Texture2D _Tex1x1Position;
    private Texture2D _Tex1x1Normal;
    private Texture2D _Tex1x1Depth;


    [SerializeField]
    private Shader _gBufferShader;

    private Vector3 _color = Vector3.zero;
    private Vector3 _position = Vector3.zero;
    private Vector3 _normal = Vector3.zero;
    private Vector3 _depth = Vector3.zero;

    private bool _onRenderGBuffer = false;

    [SerializeField]
    private bool _showGUI = false;

    public enum GBufferType
    {
        Position,
        Normal,
        Depth,
        Color
    }

    [SerializeField]
    private GBufferType _gBufferType = GBufferType.Position;
    // Start is called before the first frame update

    void CreateGBuffers()
    {
        _gBufferColor = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBAFloat, false, true);
        _gBufferPosition = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBAFloat, false, true);
        _gBufferNormal = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBAFloat, false, true);
        _gBufferDepth = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBAFloat, false, true);
        InitGBuffers();
    }

    void InitGBuffers()
    {
        RenderTexture gBufferRTColor = RenderTexture.GetTemporary(Screen.width, Screen.height, 16, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        RenderTexture gBufferRTPosition = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        RenderTexture gBufferRTNormal = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        RenderTexture gBufferRTDepth = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

        _onRenderGBuffer = true;
        Camera.main.SetReplacementShader(_gBufferShader, "RenderType");
        var targetTexture = Camera.main.targetTexture;
        Camera.main.SetTargetBuffers(new RenderBuffer[] { gBufferRTColor.colorBuffer, gBufferRTPosition.colorBuffer, gBufferRTNormal.colorBuffer, gBufferRTDepth.colorBuffer }, gBufferRTColor.depthBuffer);
        Camera.main.Render();
        Camera.main.ResetReplacementShader();
        Camera.main.targetTexture = targetTexture;
        _onRenderGBuffer = false;

        RenderTexture activeRT = RenderTexture.active;
        RenderTexture.active = gBufferRTPosition;
        _gBufferPosition.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        _gBufferPosition.Apply(false, false);

        RenderTexture.active = gBufferRTNormal;
        _gBufferNormal.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        _gBufferNormal.Apply(false, false);

        RenderTexture.active = gBufferRTColor;
        _gBufferColor.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        _gBufferColor.Apply(false, false);

        RenderTexture.active = gBufferRTDepth;
        _gBufferDepth.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        _gBufferDepth.Apply(false, false);

        RenderTexture.active = activeRT;

        RenderTexture.ReleaseTemporary(gBufferRTPosition);
        RenderTexture.ReleaseTemporary(gBufferRTNormal);
        RenderTexture.ReleaseTemporary(gBufferRTDepth);
        RenderTexture.ReleaseTemporary(gBufferRTColor);
    }

    void OnEnable()
    {
        _Tex1x1Position = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
        _Tex1x1Normal = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
        _Tex1x1Color = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
        _Tex1x1Depth = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
        CreateGBuffers();
    }

    private void OnDisable()
    {
    }
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (transform.hasChanged)
        {
            InitGBuffers();
            transform.hasChanged = false;
        }
        Vector3 mousePos = Input.mousePosition;
        if (Input.GetMouseButtonDown(0))
        {
            // マウスの位置にあるピクセル法線を取得
            var position = _gBufferPosition.GetPixelData<Vector4>(0)[(int)mousePos.x + (int)mousePos.y * Screen.width];
            // マウスの位置にあるピクセル位置を取得
            var normal = _gBufferNormal.GetPixelData<Vector4>(0)[(int)mousePos.x + (int)mousePos.y * Screen.width];

            var color = _gBufferColor.GetPixelData<Vector4>(0)[(int)mousePos.x + (int)mousePos.y * Screen.width];
            var depth = _gBufferDepth.GetPixelData<Vector4>(0)[(int)mousePos.x + (int)mousePos.y * Screen.width];

            Debug.Log($"{mousePos.x}, {mousePos.y}, Position: {position}, Normal: {normal}, Color: {color}, Depth: {depth}");

            _position = new Vector3(position.x, position.y, position.z);
            _normal = new Vector3(normal.x, normal.y, normal.z);
            _color = new Vector3(color.x, color.y, color.z);
            _depth = new Vector3(depth.x, depth.y, depth.z);

            Graphics.CopyTexture(_gBufferPosition, 0, 0, (int)mousePos.x, (int)mousePos.y, 1, 1, _Tex1x1Position, 0, 0, 0, 0);
            Graphics.CopyTexture(_gBufferNormal, 0, 0, (int)mousePos.x, (int)mousePos.y, 1, 1, _Tex1x1Normal, 0, 0, 0, 0);
            Graphics.CopyTexture(_gBufferColor, 0, 0, (int)mousePos.x, (int)mousePos.y, 1, 1, _Tex1x1Color, 0, 0, 0, 0);
            Graphics.CopyTexture(_gBufferDepth, 0, 0, (int)mousePos.x, (int)mousePos.y, 1, 1, _Tex1x1Depth, 0, 0, 0, 0);
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!_onRenderGBuffer)
        {
            if (_gBufferType == GBufferType.Position)
            {
                Graphics.Blit(_gBufferPosition, destination);
            }
            else if (_gBufferType == GBufferType.Normal)
            {
                Graphics.Blit(_gBufferNormal, destination);
            }
            else if (_gBufferType == GBufferType.Depth)
            {
                Graphics.Blit(_gBufferDepth, destination);
            }
            else if (_gBufferType == GBufferType.Color)
            {
                Graphics.Blit(_gBufferColor, destination);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    void OnGUI()
    {
        if (!_showGUI)
        {
            return;
        }

        Texture2D Tex1x1 = null;
        switch (_gBufferType)
        {
            case GBufferType.Position:
                {
                    GUIStyle style = new GUIStyle();
                    style.fontSize = 40;
                    GUIStyleState gUIStyleState = new GUIStyleState();
                    gUIStyleState.textColor = Color.black;
                    style.normal = gUIStyleState;
                    Tex1x1 = _Tex1x1Position;
                    GUI.Label(new Rect(0, 0, 200, 40), $"Position: {_position}", style);
                }
                break;
            case GBufferType.Normal:
                {
                    GUIStyle style = new GUIStyle();
                    style.fontSize = 40;
                    GUIStyleState gUIStyleState = new GUIStyleState();
                    gUIStyleState.textColor = Color.white;
                    style.normal = gUIStyleState;
                    Tex1x1 = _Tex1x1Normal;
                    GUI.Label(new Rect(0, 0, 200, 40), $"Normal: {_normal}", style);
                }
                break;
            case GBufferType.Depth:
                {
                    GUIStyle style = new GUIStyle();
                    style.fontSize = 40;
                    GUIStyleState gUIStyleState = new GUIStyleState();
                    gUIStyleState.textColor = Color.white;
                    style.normal = gUIStyleState;
                    Tex1x1 = _Tex1x1Depth;
                    GUI.Label(new Rect(0, 0, 200, 40), $"Depth: {_depth}", style);
                }
                break;
            case GBufferType.Color:
                {
                    GUIStyle style = new GUIStyle();
                    style.fontSize = 40;
                    GUIStyleState gUIStyleState = new GUIStyleState();
                    gUIStyleState.textColor = Color.black;
                    style.normal = gUIStyleState;
                    Tex1x1 = _Tex1x1Color;
                    GUI.Label(new Rect(0, 0, 200, 40), $"Color: {_color}", style);
                }
                break;
        }
        GUI.DrawTexture(new Rect(0, 40, 40, 40), Tex1x1);
    }

}

