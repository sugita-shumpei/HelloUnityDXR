Shader "Hidden/LuminanceHistogram"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 frag (v2f i) : SV_Target
            {
                // 0   1   2   3   4  
                // 0  1/4 1/2 3/4  1
                // 0      1/2      1
                float maxLuminance = 0.0;
                float aveLuminance = 0.0;
                // -4 -3 -2 -1 0 1 2 3 
                for (int y = -4; y <= 3; y++)
				{
					for (int x = -4; x <= 3; x++)
					{
                        float2 pointUV = (float2(x,y)+float2(floor(i.uv.x * _MainTex_TexelSize.z) + 0.5, floor(i.uv.y * _MainTex_TexelSize.w) + 0.5)) * _MainTex_TexelSize.xy;
						float2 maxAndAve = tex2D(_MainTex,pointUV ).xy;
						maxLuminance = max(maxLuminance, maxAndAve.x);
						aveLuminance += maxAndAve.y;
					}
				}
                aveLuminance /= 64.0;
                fixed4 col = fixed4(maxLuminance, aveLuminance, 0.0, 1.0);
                return col;
            }
            ENDCG
        }
    }
}
