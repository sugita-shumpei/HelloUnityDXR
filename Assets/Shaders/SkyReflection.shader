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
                // ���_�̃��[���h��Ԉʒu���v�Z���܂�
                float3 worldPos = mul(unity_ObjectToWorld, vertex).xyz;
                // ���[���h��Ԃ̃r���[�������v�Z���܂�
                float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
                // ���[���h��Ԗ@��
                float3 worldNormal = UnityObjectToWorldNormal(normal);
                // ���[���h��ԃ��t���N�V�����x�N�g��
                o.worldRefl = reflect(-worldViewDir, worldNormal);
                return o;
            }
        
            fixed4 frag (v2f i) : SV_Target
            {
                // �f�t�H���g�̃��t���N�V�����L���[�u�}�b�v���T���v�����O���āA���t���N�V�����x�N�g�����g�p���܂�
                half4 skyData = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, i.worldRefl);
                // �L���[�u�}�b�v�f�[�^�����ۂ̃J���[�Ƀf�R�[�h���܂�
                half3 skyColor = DecodeHDR (skyData, unity_SpecCube0_HDR);
                // �o�͂��܂�
                fixed4 c = 0;
                c.rgb = skyColor;
                return c;
            }
            ENDCG
        }
    }
}