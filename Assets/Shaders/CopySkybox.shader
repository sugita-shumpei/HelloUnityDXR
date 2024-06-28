Shader "Hidden/CopySkybox"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {   
        CGINCLUDE
        #include "UnityCG.cginc"
        struct appdata
        {
            float4 vertex: POSITION;
            float3 uv    : TEXCOORD0;
        };

        struct v2f
        {
            float3 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };

        sampler2D _MainTex;

        float3 CalculateCubeFaceUV2Position2 (float2 uv, int cube_face_index)
        {
            float u = (2.0 * uv.x - 1.0);
            float v = (2.0 * uv.y - 1.0);
            float3 position = float3(0, 0, 0);
            switch (cube_face_index)
            {
                case 0:
                    position = float3(1, -v, -u);
                    break;
                case 1:
                    position = float3(-1, -v, u);
                    break;
                case 2:
                    position = float3(u, 1, v);
                    break;
                case 3:
                    position = float3(u, -1, -v);
                    break;
                case 4:
                    position = float3(u, -v, 1);
                    break;
                case 5:
                    position = float3(-u, -v, -1);
                    break;
                default:
                    break;
            }
            return position;
        }
        v2f vert (appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }
        float4 fragCommon (v2f i, int faceIndex)
		{
            float4 skyData = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, CalculateCubeFaceUV2Position2(i.uv,faceIndex),0);
            float3 skyColor = DecodeHDR (skyData, unity_SpecCube0_HDR);
			return float4(10*skyColor,1);
		}
        float4 fragPosX (v2f i) : SV_Target{return fragCommon(i,0);}
        float4 fragNegX (v2f i) : SV_Target{return fragCommon(i,1);}
        float4 fragPosY (v2f i) : SV_Target{return fragCommon(i,2);}
        float4 fragNegY (v2f i) : SV_Target{return fragCommon(i,3);}
        float4 fragPosZ (v2f i) : SV_Target{return fragCommon(i,4);}
        float4 fragNegZ (v2f i) : SV_Target{return fragCommon(i,5);}
        ENDCG
        Pass { 
            Cull Off ZWrite Off ZTest Always
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment fragPosX
            ENDCG
        }
        Pass { 
			Cull Off ZWrite Off ZTest Always
            CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragNegX
            ENDCG
		}
        Pass {
            Cull Off ZWrite Off ZTest Always
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragPosY
            ENDCG
        }
        Pass {
			Cull Off ZWrite Off ZTest Always
            CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragNegY
            ENDCG
		}
        Pass {
			Cull Off ZWrite Off ZTest Always
            CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragPosZ
            ENDCG
		}
		Pass {
            Cull Off ZWrite Off ZTest Always
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragNegZ
            ENDCG
        }
    }
}