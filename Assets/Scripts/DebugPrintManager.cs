using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;

public class DebugPrintHandler
{
    const int kUavSlotDebugPrintBuffer        = 6;
    const int kUavSlotDebugPrintCounterBuffer = 7;
    private void AllocateResources()
    {
        _debugPrintBuffer = new ComputeBuffer(_MessageCapacity, 4, ComputeBufferType.Default);
        _debugPrintCounterBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Default);
        {
            // debugPrintCounterBufferをクリアする
            uint[] counterData = new uint[1];
            counterData[0] = 0;
            _debugPrintCounterBuffer.SetData(counterData);
        }
        {
            // debugPrintBufferをクリアする
            byte[] clearData = new byte[_debugPrintBuffer.count * 4];
            _debugPrintBuffer.SetData(clearData);
        }
    }
    public int messageCapacity
    {
        get { return _MessageCapacity; }
        set
        {
            if (value == _MessageCapacity)
            {
                return;
            }
            _MessageCapacity = value;
            Release();
            AllocateResources();
        }
    }
    private int _MessageCapacity = 16;
    private ComputeBuffer _debugPrintBuffer = null;
    private ComputeBuffer _debugPrintCounterBuffer = null;
    private string _name = "";
    public string name
    {
        get { return _name; }
        set { _name = value; }
    }

    public DebugPrintHandler(int messageCapacity, string name = "")
    {
        _name = name;
        _MessageCapacity = messageCapacity;
        AllocateResources();
    }
    ~DebugPrintHandler()
    {
        Release();
    }
    public void Release()
    {
        if (_debugPrintBuffer != null)
        {
            _debugPrintBuffer.Release();
            _debugPrintBuffer = null;
        }
        if (_debugPrintCounterBuffer != null)
        {
            _debugPrintCounterBuffer.Release();
            _debugPrintCounterBuffer = null;
        }
    }

    public void AttachMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }
        material.SetBuffer("debugPrintBuffer"       , _debugPrintBuffer);
        material.SetBuffer("debugPrintCounterBuffer", _debugPrintCounterBuffer);
    }
    public void AttachComputeShader(ComputeShader shader, int kernelID)
    {
        if (shader == null)
        {
            return;
        }
        shader.SetBuffer(kernelID, "debugPrintBuffer", _debugPrintBuffer);
        shader.SetBuffer(kernelID, "debugPrintCounterBuffer", _debugPrintCounterBuffer);
    }
    // 注意: UnRegisterGraphicsを呼び出す場合, 全てのGraphics.SetRandomWriteTargetが解除される
    // そのため, ほかのRandomWriteTargetを使用している場合は再設定が必要
    public void RegisterGraphics()
    {
        Graphics.SetRandomWriteTarget(kUavSlotDebugPrintBuffer, _debugPrintBuffer       , false);
        Graphics.SetRandomWriteTarget(kUavSlotDebugPrintCounterBuffer, _debugPrintCounterBuffer, false);
    }
    public void UnRegisterGraphics()
    {
        Graphics.ClearRandomWriteTargets();
    }
    public void Dispatch()
    {
        if (_debugPrintBuffer == null || _debugPrintCounterBuffer == null)
        {
            return;
        }
        // debugPrintBufferからデータを取得する
        uint[] counterData = new uint[1];
        _debugPrintCounterBuffer.GetData(counterData);
        if (counterData[0] == 0)
        {
            return;
        }
        bool isTruncated = false;
        if (counterData[0] > _debugPrintBuffer.count)
        {
            counterData[0] = (uint)_debugPrintBuffer.count;
            isTruncated = true;
        }
        byte[] printfData = new byte[counterData[0] * 4];
        _debugPrintBuffer.GetData(printfData);
        // 取得したデータを文字列に変換する
        string printfString = System.Text.Encoding.UTF8.GetString(printfData);
        // 終端文字と改行コードで分割する
        string[] printfLines = printfString.Split(new char[] { '\0', '\n' });
        // Null文字と空行を削除する
        printfLines = printfLines.Where(x => !string.IsNullOrEmpty(x)).ToArray();
        Debug.Log("Debug Print From " + _name + ": Begin (lines: " + printfLines.Length + ", truncated: " + isTruncated + ")");
        // デバッグログに出力する
        for (int i = 0; i < printfLines.Length; i++)
        {
            Debug.Log("Debug Print From " + _name + ": " + printfLines[i]); 
        }
        Debug.Log("Debug Print From " + _name + ": End");
        // debugPrintCounterBufferをクリアする
        counterData[0] = 0;
        _debugPrintCounterBuffer.SetData(counterData);
        // debugPrintBufferをクリアする
        byte[] clearData = new byte[_debugPrintBuffer.count * 4];
        _debugPrintBuffer.SetData(clearData);
    }
}
public class DebugPrintManager : MonoBehaviour
{
    enum ExecuteMode
    {
        Always,
        OnFirstFrame
    }
    [SerializeField]
    private ExecuteMode       executeMode     = ExecuteMode.Always;
    [SerializeField]
    private int               messageCapacity = 16;
    [SerializeField]
    private Material[]        targetMaterials = null;
    private DebugPrintHandler handler         = null;
    private bool              _isFirstFrame   = true;
    private bool              _execute        = false;
    void Start()
    {
        handler = new DebugPrintHandler(messageCapacity, this.name);
    }
    
    // Update is called once per frame
    void Update()
    {
        handler.messageCapacity = messageCapacity;
        if (executeMode == ExecuteMode.Always)
        {
            _execute = true;
            return;
        }
        else if (executeMode == ExecuteMode.OnFirstFrame)
        {
            if (_isFirstFrame)
            {
                _execute      = true;
                _isFirstFrame = false;
                return;
            }
        }
        _execute = false;
    }

    private void OnPreRender()
    {
        if (targetMaterials == null || targetMaterials.Length == 0 || handler == null || !_execute)
        {
            return;
        }

        for (int i = 0; i < targetMaterials.Length; i++)
        {
            handler.AttachMaterial(targetMaterials[i]);
        }
        handler.RegisterGraphics();
    }

    private void OnPostRender()
    {
        if (targetMaterials == null || targetMaterials.Length == 0 || handler == null || !_execute)
        {
            return;
        }
        handler.Dispatch();
        handler.UnRegisterGraphics();
    }
    private void OnDestroy()
    {
        handler.Release();
    }
}
