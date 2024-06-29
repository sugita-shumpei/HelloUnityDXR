Shader "Unlit/HelloProcedural"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }

        Pass 
        { 
            Name "HelloProcedural"
            Tags { "LightMode"="RayTracing" }
            LOD 100
            HLSLPROGRAM 
            
            #include "UnityRayTracingMeshUtils.cginc"
            #include "RTUtils.hlsl"
            
            #pragma multi_compile_local _ RAY_TRACING_PROCEDURAL_GEOMETRY

            #pragma raytracing main
            struct RayPayloadData {
                float4 color;
            };
            struct RayAttributeData {
                float2 barycentrics;
            };

            #define PROCEDURAL_PRIMITIVE_TYPE_SPHERE 1

            uint   _ProceduralPrimitiveType;
            float3 _SphereCenter;
            float  _SphereRadius;
#if RAY_TRACING_PROCEDURAL_GEOMETRY
            [shader("intersection")]
            void IntersectionMain()
            {
                if (_ProceduralPrimitiveType == PROCEDURAL_PRIMITIVE_TYPE_SPHERE)
				{
					float3 center       = _SphereCenter;
					float  radius       = _SphereRadius;
					float3 rayOrigin    = WorldRayOrigin();
					float3 rayDirection = WorldRayDirection();
					float3 oc = rayOrigin - center;
					float a = dot(rayDirection, rayDirection);
					float b = 2.0 * dot(oc, rayDirection);
					float c = dot(oc, oc) - radius * radius;
					float discriminant = b * b - 4 * a * c;
					if (discriminant > 0)
					{
						float t = (-b - sqrt(discriminant)) / (2.0 * a);
						if (t < RayTCurrent() && t > RayTMin())
						{
							float3 hitPoint = rayOrigin + t * rayDirection;
							float3 normal   = (hitPoint - center) / radius;
                            // 頂点角と偏角に変換
                            float phi = atan2(normal.z, normal.x);
                            float theta = acos(normal.y);
                            // テクスチャ座標に変換
                            float u = 1.0 - (phi + 3.141592) / (2.0 * 3.141592);
                            float v = theta / 3.141592;
							RayAttributeData attr;
							attr.barycentrics = float2(u, v);
							ReportHit(t, PROCEDURAL_PRIMITIVE_TYPE_SPHERE, attr);
						}
                        t = (-b + sqrt(discriminant)) / (2.0 * a);
                        if (t < RayTCurrent() && t > RayTMin())
                        {
							float3 hitPoint = rayOrigin + t * rayDirection;
							float3 normal = (hitPoint - center) / radius;
							// 頂点角と偏角に変換
							float phi = atan2(normal.z, normal.x);
							float theta = acos(normal.y);
							// テクスチャ座標に変換
							float u = 1.0 - (phi + 3.141592) / (2.0 * 3.141592);
							float v = theta / 3.141592;
							RayAttributeData attr;
							attr.barycentrics = float2(u, v);
							ReportHit(t, PROCEDURAL_PRIMITIVE_TYPE_SPHERE, attr);
                        }
					}
				}
            }
#endif
            struct SurfaceData {
                float3 vPosition;
                float3 vNormal;
                float3 fNormal;
                float2 vUv;
            };
            SurfaceData GetSurface(RayAttributeData attrib, uint primitiveIndex) {
                SurfaceData surface;
#if RAY_TRACING_PROCEDURAL_GEOMETRY
                uint hitKind = HitKind();
                if (hitKind == PROCEDURAL_PRIMITIVE_TYPE_SPHERE) {
					surface.vPosition = WorldRayOrigin() + RayTCurrent() * WorldRayDirection();
					surface.fNormal   = normalize(surface.vPosition - _SphereCenter);
					surface.vNormal   = surface.fNormal;
					surface.vUv       = attrib.barycentrics;
				}
				else {
					surface.vPosition = WorldRayOrigin() +  RayTCurrent() * WorldRayDirection();
					surface.vUv     = attrib.barycentrics;
				}
#else
                float2 bary = attrib.barycentrics;
                uint3 indices = UnityRayTracingFetchTriangleIndices(primitiveIndex);
                float3 vpositions[] = {
                    UnityRayTracingFetchVertexAttribute3(indices.x, kVertexAttributePosition),
                    UnityRayTracingFetchVertexAttribute3(indices.y, kVertexAttributePosition),
                    UnityRayTracingFetchVertexAttribute3(indices.z, kVertexAttributePosition),
                };
                surface.vPosition = triangleInterpolation3(vpositions[0], vpositions[1], vpositions[2], attrib.barycentrics);
                surface.fNormal = transformObjectToWorldNormal(cross(vpositions[1] - vpositions[0], vpositions[2] - vpositions[0]));
                surface.vNormal = transformObjectToWorldNormal(triangleInterpolation3(
                    UnityRayTracingFetchVertexAttribute3(indices.x, kVertexAttributeNormal),
                    UnityRayTracingFetchVertexAttribute3(indices.y, kVertexAttributeNormal),
                    UnityRayTracingFetchVertexAttribute3(indices.z, kVertexAttributeNormal), attrib.barycentrics
                ));
                surface.vUv =  triangleInterpolation2(
                    UnityRayTracingFetchVertexAttribute2(indices.x,kVertexAttributeTexCoord0),
                    UnityRayTracingFetchVertexAttribute2(indices.y,kVertexAttributeTexCoord0),
                    UnityRayTracingFetchVertexAttribute2(indices.z,kVertexAttributeTexCoord0), bary
                );
#endif

                return surface;
            }
            [shader("closesthit")]
            void ClosestHitForTest(inout RayPayloadData payload : SV_RayPayload, in RayAttributeData attrib : SV_IntersectionAttributes)
            {
                SurfaceData surface = GetSurface(attrib, PrimitiveIndex());
                float2 uv = surface.vUv;
                payload.color = float4(uv.x, uv.y, 1.0f -0.5* uv.x - 0.5*uv.y,1);
            }

            ENDHLSL
        }
    }
}
