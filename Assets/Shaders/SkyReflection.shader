// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/SkyReflection"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f {
                half3 worldRefl : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            v2f vert (float4 vertex : POSITION, float3 normal : NORMAL)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                // 頂点のワールド空間位置を計算します
                float3 worldPos = mul(unity_ObjectToWorld, vertex).xyz;
                // ワールド空間のビュー方向を計算します
                float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
                // ワールド空間法線
                float3 worldNormal = UnityObjectToWorldNormal(normal);
                // ワールド空間レフレクションベクトル
                o.worldRefl = reflect(-worldViewDir, worldNormal);
                return o;
            }
        
            fixed4 frag (v2f i) : SV_Target
            {
                // デフォルトのリフレクションキューブマップをサンプリングして、リフレクションベクトルを使用します
                half4 skyData = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, i.worldRefl);
                // キューブマップデータを実際のカラーにデコードします
                half3 skyColor = DecodeHDR (skyData, unity_SpecCube0_HDR);
                // 出力します
                fixed4 c = 0;
                c.rgb = skyColor;
                return c;
            }
            ENDCG
        }
    }
}