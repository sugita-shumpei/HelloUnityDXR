Shader "Hidden/Gaussian"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        CGINCLUDE
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
        float4    _MainTex_TexelSize;

        // 4x4 Gaussian Kernel

        fixed4 fragX (v2f i) : SV_Target
        {
            float2 base  = i.uv;
            float  delta = _MainTex_TexelSize.x;
            float4 col1  = tex2D(_MainTex, base + float2(delta * 0.25, 0));
            float4 col2  = tex2D(_MainTex, base - float2(delta * 0.25, 0));
            float4 col3  = tex2D(_MainTex, base + float2(delta * 1.25, 0));
            float4 col4  = tex2D(_MainTex, base - float2(delta * 1.25, 0));
            return (col1 + col2 + col3 + col4) / 4.0;
        }

        fixed4 fragY (v2f i) : SV_Target
		{
            float delta = _MainTex_TexelSize.y;
            float4 col1 = tex2D(_MainTex, i.uv + float2(0, delta * 0.25));
            float4 col2 = tex2D(_MainTex, i.uv - float2(0, delta * 0.25));
            float4 col3 = tex2D(_MainTex, i.uv + float2(0, delta * 1.25));
            float4 col4 = tex2D(_MainTex, i.uv - float2(0, delta * 1.25));
            return (col1 + col2 + col3 + col4) / 4.0;
		}
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragX
            ENDCG
        }
        Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragY
			ENDCG
		}
    }
}
