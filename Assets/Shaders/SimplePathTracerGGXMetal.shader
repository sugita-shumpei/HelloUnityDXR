Shader "Unlit/SimplePathTracerGGXMetal"
{
    Properties {
		_Color ("Color"             , Color) = (1,1,1,1)
        _Roughness ("Roughness", Range(0,1)) = 0.5
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };
            
            float4 _Color;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = _Color ;
                // apply fog
                return col;
            }
            ENDCG
        }
        Pass
        {
            // �����Őݒ肵���p�X����raytracing shader���Ŏw�肷�邱�Ƃ�, ���̃p�X���g�p�����
            Name "SimplePathTracer"
            Tags { "LightMode" = "RayTracing" }
            
            HLSLPROGRAM
            #include "UnityRayTracingMeshUtils.cginc"
            #include "SimplePathTracerCommon.hlsl"
            #include "ggx.hlsl"
            // #pragma raytracing�����邱�Ƃ�, ���̏�����raytracing�ł���ƔF�������
            #pragma raytracing main
            struct RayAttributeData {
                float2 barycentrics;
            };
            float4 _Color;
            float _Roughness;

            [shader("closesthit")]
            void ClosestHitForSurface(inout RayPayloadData payload : SV_RayPayload, in RayAttributeData attrib : SV_IntersectionAttributes)
            {
                float3 ray_origin     = WorldRayOrigin();
                float3 ray_direction  = WorldRayDirection();
                float  ray_distance   = RayTCurrent();
                uint   primitiveIndex = PrimitiveIndex();
                float2 bary           = attrib.barycentrics;

                uint3  indices        = UnityRayTracingFetchTriangleIndices(primitiveIndex);
                
                float3 position       = ray_origin + ray_distance * ray_direction;
                float3 vpositions[]   = {
                    UnityRayTracingFetchVertexAttribute3(indices.x,kVertexAttributePosition),
                    UnityRayTracingFetchVertexAttribute3(indices.y,kVertexAttributePosition),
                    UnityRayTracingFetchVertexAttribute3(indices.z,kVertexAttributePosition),
                };
                float3 fnormal = transformObjectToWorldNormal(cross(vpositions[1]-vpositions[0], vpositions[2]-vpositions[0]));
                float3 vnormal = transformObjectToWorldNormal(triangleInterpolation3(
                    UnityRayTracingFetchVertexAttribute3(indices.x,kVertexAttributeNormal),
                    UnityRayTracingFetchVertexAttribute3(indices.y,kVertexAttributeNormal),
                    UnityRayTracingFetchVertexAttribute3(indices.z,kVertexAttributeNormal), bary
                ));
                float3 normal  = vnormal;
                float2 uv = triangleInterpolation2(
                    UnityRayTracingFetchVertexAttribute2(indices.x,kVertexAttributeTexCoord0),
                    UnityRayTracingFetchVertexAttribute2(indices.y,kVertexAttributeTexCoord0),
                    UnityRayTracingFetchVertexAttribute2(indices.z,kVertexAttributeTexCoord0), bary
                );
                if (dot(ray_direction, normal) > 0.0) {
					normal = -normal;
				}
                
                initSeed(payload.seed);
                float2 rand2        = float2 (uintToNormalizedFloat (randPCG()) , uintToNormalizedFloat (randPCG()));
                float3 wi           = onbWorldToLocal(-ray_direction,normal);
                float3 wo; float pdf;
                float3 weights      = SampleAndEval_BRDF_GGX_NdotO_Per_D_NdotM(wi,rand2,_Color,_Roughness*_Roughness,wo,pdf);
                if (isnan(weights.x) || isnan(weights.y) || isnan(weights.z)) {
					weights = float3(0,0,0);
				}
                payload.throughput *= weights;
                payload.origin      = position + 0.01 * normal;
                payload.direction   = onbLocalToWorld(wo,normal);
                payload.seed        = readSeed();
            }

            
            ENDHLSL
        }
    }
}
