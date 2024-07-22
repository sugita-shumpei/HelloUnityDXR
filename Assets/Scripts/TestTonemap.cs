using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class TestTonemap : MonoBehaviour
{
    private Texture2D _texture = null;
    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        _texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBAFloat, false);
        var pixels = _texture.GetPixelData<Vector4>(0);
        for (int i = 0; i < _texture.height; ++i)
        {
            for (int j = 0; j < _texture.width; ++j)
            {
                if (i < _texture.height / 2)
                {

                    pixels[i * _texture.width + j] = new Vector4(1.0F, 0.0f, 0.0f, 1.0f);
                }
                else
                {
                    pixels[i * _texture.width + j] = new Vector4(0.0F, 0.0f, 0.0f, 1.0f);
                }
            }
        }
        _texture.Apply();
        RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGBFloat, 0, Texture.GenerateAllMips);
        rtDesc.useMipMap = true;
        RenderTexture rt = RenderTexture.GetTemporary(rtDesc);
        Graphics.Blit(_texture, rt);
        var readBack2D = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
        RenderTexture activeRT = RenderTexture.active;
        Graphics.SetRenderTarget(rt, rt.mipmapCount - 1);
        readBack2D.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
        readBack2D.Apply();
        var pixel = readBack2D.GetPixelData<Vector4>(0);
        Debug.Log("Pixel: " + pixel[0]);
        RenderTexture.active = null;
        Graphics.Blit(rt, destination);
        RenderTexture.ReleaseTemporary(rt);
    }
}
