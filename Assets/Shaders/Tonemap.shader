Shader "Hidden/Tonemap"
{
    Properties
    {
        _MainTex         ("Texture"              , 2D)    = "white" {}
        _LuminanceAveTex ("Luminance Average Texture"    , 2D) = "white" {}
        _WhiteColor      ("White Color"          , Color) = (1, 1, 1, 1)
        _WhiteIntensity  ("White Intensity"      , float) = 1
        _Exposure        ("Exposure"             , float) = 1
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
            sampler2D _LuminanceAverageTex;
            float4 _WhiteColor;
            float _WhiteIntensity;
            float _Exposure;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float luminanceAverage    = tex2Dlod(_LuminanceAverageTex, float4(i.uv,0,100)).r;
                float luminance           = Luminance(col.rgb);
                float3 colorLuminance     = _Exposure * luminance/ luminanceAverage;
                float3 white              = _WhiteColor.rgb * _WhiteIntensity;
                float3 whiteLuminance     = _Exposure * Luminance(white)/ luminanceAverage;
                return float4 (col.rgb*(colorLuminance/luminance)* (float3(1,1,1)+colorLuminance/(whiteLuminance*whiteLuminance))/(float3(1,1,1)+whiteLuminance),1);
                //return float4(luminanceAverage, luminanceAverage, luminanceAverage, 1);
            }
            ENDCG
        }
    }
}
