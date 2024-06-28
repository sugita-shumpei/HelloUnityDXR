Shader "Unlit/DebugPrintTest"
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
            HLSLPROGRAM
            #pragma target   5.0
            #pragma vertex   vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "DebugPrint.hlsl"

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
                // screen POSITION
                float2 screenPos = (i.vertex.xy / _ScreenParams.xy);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                if (screenPos.x > 0.51 && screenPos.x < 0.52 && screenPos.y > 0.51 && screenPos.y < 0.52){
                    // DebugPrintTest
                    debugPrint('[','D','e','b','u','g','P','r','i','n','t','T','e','s','t',']', ' ');
                    debugPrint('n','a','n','=')        ; debugPrint1f(0.0 /0.0)     ; debugPrint(' ');
                    debugPrint('-','i','n','f','=')    ; debugPrint1f(-1.0/0.0)     ; debugPrint(' ');
                    debugPrint('v','1','=')            ; debugPrint1f( 1.02E+4)     ; debugPrint(' ');
                    debugPrint('v','2','=')            ; debugPrint1f(-1.02E+5)     ; debugPrint(' ');
                    debugPrint('v','3','=')            ; debugPrint1f( 1.02E+6)     ; debugPrint(' ');
                    debugPrint('v','4','=')            ; debugPrint1f(-1.02E-4)     ; debugPrint(' ');
                    debugPrint('v','5','=')            ; debugPrint1f( 1.02E-5)     ; debugPrint(' ');
                    debugPrint('v','6','=')            ; debugPrint1f(-1.02E-6)     ; 
                    debugPrintEndl();
                    flushPrintStream();
                }
                if (getPrintErrorCode()!=0){
                    col = fixed4(1,0,0,1);
                    return col;
                }
                return col;
            }
            ENDHLSL
        }
    }
}
