Shader "Unlit/SimpleGBuffer"
{
    
    Properties {
		_Color ("Color", Color) = (1,1,1,1)
	}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex   : SV_POSITION;
                float3 worldPos : POSITION1;
                float3 normal   : NORMAL;
                float  depth    : COLOR0;
            };

            struct fout {
                float4 color    : COLOR0;
				float4 worldPos : COLOR1;
                float4 normal   : COLOR2;
                float4 depth    : COLOR3;
			};

            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal   = UnityObjectToWorldNormal(v.normal);
                o.depth    = abs(o.vertex.z/o.vertex.w);
                o.uv = v.uv;
                return o;
            }

            fout frag (v2f i) : SV_Target
            {
                fout o;
				o.worldPos = float4(i.worldPos, 1.0);
				o.normal = float4(i.normal, 1.0);
                o.depth = float4(i.depth,0,0, 1.0);
				o.color = _Color;
				return o;
            }
            ENDCG
        }
    }
}
