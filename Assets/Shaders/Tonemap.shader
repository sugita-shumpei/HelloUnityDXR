Shader "Hidden/Tonemap"
{
    Properties
    {
        _MainTex     ("Texture"          , 2D)    = "white" {}
        _AveLuminance("Luminance Average", float) = 0
        _MaxLuminance("Luminance Maximum", float) = 0
        _Exposure    ("Exposure"         , float) = 1
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
            #pragma multi_compile_local _ _USE_SMALL_TEXTURE
            #pragma multi_compile_local _ _TONEMODE_LINEAR _TONEMODE_REINHARD _TONEMODE_UNCHARTED2 _TONEMODE_ACES

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
            sampler2D _SmallTex;
            float     _AveLuminance;
            float     _MaxLuminance;
            float     _Exposure;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float  baseLuminance = Luminance(col.rgb) ;
            #if _USE_SMALL_TEXTURE
                float3 smallTexValue = tex2D(_SmallTex, float2(0,0)).rgb;
                float  maxLuminance = smallTexValue.r;
                float  aveLuminance = smallTexValue.g;
            #else
                float  maxLuminance = _MaxLuminance;
                float  aveLuminance = _AveLuminance;
            #endif
                float  inputLuminance  = _Exposure*baseLuminance / aveLuminance;
                
            #if   _TONEMODE_LINEAR
                return float4 (col.rgb * _Exposure / aveLuminance, col.a);
            #elif _TONEMODE_REINHARD
                float  whiteLuminance  = _Exposure*maxLuminance / aveLuminance;
                float  outputLuminance = (inputLuminance / (1+inputLuminance)) * (1+inputLuminance / (whiteLuminance*whiteLuminance));
                return float4 (col.rgb * outputLuminance / baseLuminance, col.a);
            #elif _TONEMODE_ACES
                const float a = 2.51;
                const float b = 0.03;
                const float c = 2.43;
                const float d = 0.59;
                const float e = 0.14;
                // float3 in_col = col.rgb *0.6 *_Exposure / aveLuminance;
                // return float4(saturate(in_col * (a * in_col + b) / (in_col * (c * in_col + d) + e)),col.a);
                float tempLuminance = inputLuminance * 0.6;
                float outputLuminance = saturate((tempLuminance * (a * tempLuminance + b)) / (tempLuminance * (c * tempLuminance + d) + e));
                return float4(col.rgb * outputLuminance / baseLuminance, col.a);
            #else
                return col;
            #endif
            }
            ENDCG
        }
    }
}
