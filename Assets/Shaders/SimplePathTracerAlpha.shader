Shader "Unlit/SimplePathTracerAlpha"
{
    Properties {
        _MainTex       ("Texture", 2D) = "white" {}
		_Color         ("Color"         , Color) = (1,1,1,1)
        _EmissionColor ("Emission Color", Color) = (0,0,0,0)
        _EmissionIntensity ("Emission Intensity", Range(0.01, 100)) = 1
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
            UNITY_DECLARE_TEX2D(_MainTex);

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
                float4 texColor = UNITY_SAMPLE_TEX2D(_MainTex, i.uv);
                if (texColor.a < 0.5){
                    discard;
                }
                // sample the texture
                fixed4 col = _Color   *texColor;
                // apply fog
                return col;
            }
            ENDCG
        }
        Pass
        {
            // ここで設定したパス名をraytracing shader側で指定することで, このパスが使用される
            Name "SimplePathTracer"
            Tags { "LightMode" = "RayTracing" }
            
            HLSLPROGRAM
            #include "UnityCG.cginc"
            #include "UnityRayTracingMeshUtils.cginc"
            #include "SimplePathTracerCommon.hlsl"
            // #pragma raytracingをつけることで, この処理がraytracingであると認識される
            #pragma raytracing main
            struct RayAttributeData {
                float2 barycentrics;
            };
            UNITY_DECLARE_TEX2D(_MainTex);
            float4 _Color;
            float4 _EmissionColor;
            float  _EmissionIntensity;
            
            [shader("anyhit")]
            void AnyHitForAlpha(inout RayPayloadData payload : SV_RayPayload, in RayAttributeData attrib : SV_IntersectionAttributes)
			{
                uint   primitiveIndex = PrimitiveIndex();
                float2 bary           = attrib.barycentrics;
                uint3  indices        = UnityRayTracingFetchTriangleIndices(primitiveIndex);
                float2 uv = triangleInterpolation2(
                    UnityRayTracingFetchVertexAttribute2(indices.x,kVertexAttributeTexCoord0),
                    UnityRayTracingFetchVertexAttribute2(indices.y,kVertexAttributeTexCoord0),
                    UnityRayTracingFetchVertexAttribute2(indices.z,kVertexAttributeTexCoord0), bary
                );
                float alpha = UNITY_SAMPLE_TEX2D_LOD(_MainTex, uv,0).a;
                if (alpha < 0.5){
                    IgnoreHit();
                }
			}

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
                if (dot (ray_direction, fnormal) > 0.0) {
					fnormal           =-fnormal;
				}
                float2 uv = triangleInterpolation2(
                    UnityRayTracingFetchVertexAttribute2(indices.x,kVertexAttributeTexCoord0),
                    UnityRayTracingFetchVertexAttribute2(indices.y,kVertexAttributeTexCoord0),
                    UnityRayTracingFetchVertexAttribute2(indices.z,kVertexAttributeTexCoord0), bary
                );
                
                initSeed(payload.seed);
                float2 rand2          = float2 (uintToNormalizedFloat (randPCG()) , uintToNormalizedFloat (randPCG()));
                float3 wi             = uniformInCosineHemisphere (rand2);
                float3 prv_throughput = payload.throughput;
                float3 emission       = _EmissionColor * _EmissionIntensity;
                float a_emission     = (emission.x + emission.y + emission.z) / 3.0;
                if (a_emission != 0.0) { 
                    payload.done = true;
                }
                ray_direction          = onbLocalToWorld(wi, fnormal);
                payload.throughput    *= _Color * UNITY_SAMPLE_TEX2D_LOD(_MainTex, uv,0).xyz;
                payload.origin         = position + 0.01 * fnormal;
                payload.direction      = ray_direction;
                payload.radiance      += prv_throughput * emission;
                payload.seed           = readSeed();
            }

            
            ENDHLSL
        }
    }
}
