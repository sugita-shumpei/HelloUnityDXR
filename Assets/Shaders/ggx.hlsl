#ifndef UNITY_GGX_INCLUDED
#define UNITY_GGX_INCLUDED
#include "distribution.hlsl"
#include "UnityCG.cginc"

#if !defined(_GGX_G1_FACTOR_MODE_SMITH) &&  !defined(_GGX_G1_FACTOR_MODE_SCHLICK)
#define     _GGX_G1_FACTOR_MODE_SCHLICK 1
#endif

#if !defined(_GGX_G2_FACTOR_MODE_JOINT) &&  !defined(_GGX_G2_FACTOR_MODE_SEPARATE)
#define     _GGX_G2_FACTOR_MODE_SEPARATE 1
#endif

#if defined(_GGX_G1_FACTOR_MODE_SMITH)
#undef      _GGX_G1_FACTOR_MODE_SCHLICK
#endif
#if defined(_GGX_G1_FACTOR_MODE_SCHLICK)
#undef      _GGX_G1_FACTOR_MODE_SMITH
#endif

#if defined(_GGX_G2_FACTOR_MODE_JOINT)
#undef      _GGX_G2_FACTOR_MODE_SEPARATE
#endif
#if defined(_GGX_G2_FACTOR_MODE_SEPARATE)
#undef      _GGX_G2_FACTOR_MODE_JOINT
#endif
// Torrance-Sparrow Model 
// x        : 反射点の位置
// i        : 入射方向, o: 出射方向
// m        : ハーフベクトル(m = (i+o)/|i+o|)
// n        : 面法線
// θm       : 面法線nとハーフベクトルmのなす角
// θv       : 面法線nと任意の方向vのなす角
// α        : alpha, 通常はroughnessの二乗
// B(x,i,o) : 双方向散乱分布関数, B(x,i,o)=D(m)G(i,o,m)F(i,m) / (4 * (n, i) * (n, o))
// D(m)     : Normal Distribution Function(マイクロファセット法線分布関数）
// G(i,o,m) : Masking Shadow Function(双方向性あり)
// 単方向のマスク関数(G1)による近似G~ G1(i,m)*G1(o,m)が行われる
// 
// 実装(D): ormal Distribution Function
// 
float  D_GGX(float cos_m,float a) {
    if (cos_m > 0.0) {
        float  cos_m_2 = cos_m * cos_m;
        float  a_2  = a * a;
        float  temp = (1.0 + (a_2 - 1.0) * cos_m_2);
        return a_2 * UNITY_INV_PI / (temp* temp);
    }
    else {
        return 0.0;
    }
}
float D_GGX_anisotropic(float3 w, float ax, float ay) {
	float xn  = w.x / ax;
	float yn  = w.y / ay;
	float zn  = w.z;
	float xn2 = xn * xn;
	float yn2 = yn * yn;
	float zn2 = zn * zn;
    float v1 = (xn2 + yn2 + zn2);
	return UNITY_INV_PI / (ax * ay * v1 * v1);
}
float Alpha_GGX_anisotropic(float3 w, float ax, float ay) {
    float xn   = w.x;
	float yn   = w.y;
    float zn   = w.z;
	float cos2 = zn * zn;
    float sin2 = max(1.0 - cos2, 0.0);
	if (sin2 == 0.0) { return 0.0; }
	float ax2  = ax * ax;
	float ay2  = ay * ay;
	float xn2  = xn * xn;
	float yn2  = yn * yn;
	float v1   = xn2 * ax2 + yn2 * ay2;
    float v2   = v1 / sin2;
	return sqrt(v2);
}
// 
// 実装(G): Masking Shadow Function
// 
float G1_GGX_Smith(float cos_v, float a) {
    float cos_v_2 = cos_v * cos_v;	
    float sin_v_2 = 1.0 - cos_v_2;
    float tan_v_2 = sin_v_2 / cos_v_2;
    return 2.0 * cos_v / (cos_v + sqrt(cos_v_2 + a*a* sin_v_2));
}
float  G1_GGX_Schlick(float cos_v, float a) {
    cos_v   = saturate(cos_v);
    float k = a * 0.5;
    return cos_v / ((1.0 - k) * cos_v + k);
}
float  G1_GGX(float cos_v,float a) {
#if defined(_GGX_G1_FACTOR_MODE_SMITH)
    return G1_GGX_Smith(cos_v,a);
#endif
#if defined(_GGX_G1_FACTOR_MODE_SCHLICK)
    return G1_GGX_Schlick(cos_v,a);
#endif

}
float  G2_GGX_Smith_Joint(float cos_i, float cos_o, float a) {
    float cos_i_2 = cos_i * cos_i;
    if (cos_i_2 == 0.0) { return 0.0; }
    float cos_o_2 = cos_o * cos_o;
    if (cos_o_2 == 0.0) { return 0.0; }
    float sin_i_2 = max(1.0 - cos_i_2, 0.0);
    float tan_i_2 = sin_i_2 / cos_i_2;
    float sin_o_2 = max(1.0 - cos_o_2, 0.0);
    float tan_o_2 = sin_o_2 / cos_o_2;
    float a_2 = a * a;
    return 2.0 / (sqrt(1.0 + a_2 * tan_i_2) + sqrt(1.0 + a_2 * tan_o_2));
}
float  G2_GGX_Joint(float cos_i, float cos_o, float a) {
    return G2_GGX_Smith_Joint(cos_i, cos_o, a);
}
float  G2_GGX_Separate(float cos_i, float cos_o, float a) {
    return G1_GGX(cos_i,a) * G1_GGX(cos_o,a);
}
float  G2_GGX(float cos_i, float cos_o, float a) {
#if defined(_GGX_G2_FACTOR_MODE_JOINT)
    return G2_GGX_Joint(cos_i, cos_o, a);
#endif
#if defined(_GGX_G2_FACTOR_MODE_SEPARATE)
    return G2_GGX_Separate(cos_i, cos_o, a);
#endif
}
// 
// 実装(F): Fresnel項
// 
float  Fresnel_Schlick(float cosine, float f0)
{
    float d = max(1.0 - cosine,0.0);
    float d2 = d * d;
    float d4 = d2 * d2;
    float d5 = d4 * d;
    return f0 + (1.0 - f0) * d5;
}
float  Fresnel_Schlick(float cosine, float f0, float f90)
{
    float d = max(1.0 - cosine, 0.0);
    float d2 = d  *  d;
    float d4 = d2 * d2;
    float d5 = d4 *  d;
    return f0 + (f90 - f0) * d5;
}
float3 Fresnel_Schlick3(float cosine, float3 f0)
{
    float d  = max(1.0 - cosine, 0.0);
    float d2 = d * d;
    float d4 = d2 * d2;
    float d5 = d4 * d;
    return f0 + (1.0 - f0) * d5;
}
float3 Fresnel_Schlick3(float cosine, float3 f0, float3 f90)
{
    float d  = max(1.0 - cosine, 0.0);
    float d2 = d * d;
    float d4 = d2 * d2;
    float d5 = d4 * d;
    return f0 + (f90 - f0) * d5;
}
// 
// BSDF
// 
float3 Eval_BRDF_GGX(float3 wi, float3 wm, float3 f0, float a)
{
    float3 wo     = normalize(reflect(-wi, wm));
    float cos_m_n = wm.z;
    float cos_i_n = wi.z;
    float cos_o_n = wo.z;
    float cos_i_m = dot(wi, wm);
    float cos_o_m = cos_i_m;
    float d       = D_GGX(cos_m_n,a);
    float step_i  = (cos_i_n > 0.0) * (cos_i_m > 0.0);
    float step_o  = (cos_o_n > 0.0) * (cos_o_m > 0.0);
    float g       = step_i * step_o * G2_GGX(cos_i_n, cos_o_n,a);
    float3 f      = Fresnel_Schlick3(cos_o_n, f0);
    if (g == 0.0) { return float3(0.0, 0.0, 0.0); }
    return d * g * f * 0.25 / (cos_i_n * cos_o_n);
}
// importance sampling(D(m)|m・n|)
// 
float  Eval_PDF_GGX_D_NdotM(float cos_m, float a){
    return DensityInGGXNormalCosineHemiSphere(cos_m, a);
}
float3 Sample_PDF_GGX_D_NdotM(float2 rnd, float a)
{
    return UniformInGGXNormalCosineHemiSphere(rnd, a);

}
float4 SampleAndEval_PDF_GGX_D_NdotM(float2 rnd, float a) {
    float3 m = UniformInGGXNormalCosineHemiSphere(rnd, a);
    return float4(m.x, m.y, m.z, DensityInGGXNormalCosineHemiSphere(m.z,a));
}

float3 SampleAndEval_BRDF_GGX_Per_D_NdotM      (float3 wi, float2 rnd, float3 f0, float a, out float3 wo, out float pdf)
{
    float4 wm_amd_pdf = SampleAndEval_PDF_GGX_D_NdotM(rnd, a);
    float3 wm = normalize(float3(wm_amd_pdf.x, wm_amd_pdf.y, wm_amd_pdf.z));
    float d_cos_m = wm_amd_pdf.w;
    wo = normalize(reflect(-wi, wm));
    float cos_m_n  = wm.z;
    float cos_i_n  = wi.z;
    float cos_o_n  = wo.z;
    float cos_i_m  = dot(wi, wm);
    float cos_o_m  = cos_i_m;
    float step_i   = (cos_i_n > 0.0) * (cos_i_m > 0.0);
    float step_o   = (cos_o_n > 0.0) * (cos_o_m > 0.0);
    float  g       = step_i * step_o * G2_GGX(cos_i_n, cos_o_n, a);
    float3 f       = Fresnel_Schlick3(cos_i_m, f0);
    // BRDF        = F * G * D   / 4 * (cos_i_n * cos_o_n)
    // PDF         = D * cos_m_n / 4 * (cos_o_m)
    // BRDF_PER_PDF= F * G * cos_o_m / (cos_i_n * cos_o_n * cos_m_n)
    float tmp2     = (cos_i_n * cos_o_n * cos_m_n);
    if (tmp2 ==  0.0) { pdf = 0.0; return float3(0.0, 0.0, 0.0); }
    pdf = d_cos_m * 0.25 / cos_o_m;
    return g * f * cos_o_m / tmp2;
}
float3 SampleAndEval_BRDF_GGX_NdotO_Per_D_NdotM(float3 wi, float2 rnd, float3 f0, float a, out float3 wo, out float pdf)
{
    float4 wm_amd_pdf = SampleAndEval_PDF_GGX_D_NdotM(rnd,a);
    float3 wm         = normalize(float3(wm_amd_pdf.x, wm_amd_pdf.y, wm_amd_pdf.z));
    float d_cos_m     = wm_amd_pdf.w;
    wo                = normalize(reflect(-wi, wm));
    float cos_m_n = wm.z;
    float cos_i_n = wi.z;
    float cos_o_n = wo.z;
    float cos_i_m = dot(wi, wm);
    float cos_o_m = cos_i_m;
    float step_i  = (cos_i_n > 0.0) * (cos_i_m > 0.0);
    float step_o  = (cos_o_n > 0.0) * (cos_o_m > 0.0);
    float  g      = step_i * step_o * G2_GGX(cos_i_n, cos_o_n, a);
    float3 f      = Fresnel_Schlick3(cos_i_m, f0);
    // BRDF        = F * G * D   / 4 * (cos_i_n * cos_o_n)
    // PDF         = D * cos_m_n / 4 * (cos_o_m)
    // BRDF_PER_PDF= F * G * cos_o_m / (cos_i_n * cos_o_n * cos_m_n)
    // BRDF_Cos_PER_PDF= F * G * cos_o_m / (cos_i_n * cos_m_n)
    float tmp2 = (cos_i_n * cos_m_n);
    if (tmp2  == 0.0) { pdf = 0.0; return float3(0.0, 0.0, 0.0); }
    pdf = d_cos_m * 0.25 / cos_o_m;
    return g * f * cos_o_m / tmp2;
}
float  Eval_PDF_GGX_D_NdotM_anisotropic(float3 w, float ax, float ay) {
    return DensityInAnisotropicGGXNormalCosineHemiSphere(w, ax, ay);
}
float3 Sample_PDF_GGX_D_NdotM_anisotropic(float2 rnd, float ax, float ay) {
    return UniformInAnisotropicGGXNormalCosineHemiSphere(rnd, ax, ay);
}
float4 SampleAndEval_PDF_GGX_D_NdotM_anisotropic(float2 rnd, float ax, float ay) {
    float3 m = UniformInAnisotropicGGXNormalCosineHemiSphere(rnd, ax, ay);
    return float4(m.x, m.y, m.z, DensityInAnisotropicGGXNormalCosineHemiSphere(m, ax, ay));
}
float3 SampleAndEval_BRDF_GGX_Per_D_NdotM_anisotropic(float3 wi, float2 rnd, float3 f0, float ax, float ay, out float3 wo, out float pdf) {
	float4 wm_amd_pdf = SampleAndEval_PDF_GGX_D_NdotM_anisotropic(rnd, ax, ay);
	float3 wm = normalize(float3(wm_amd_pdf.x, wm_amd_pdf.y, wm_amd_pdf.z));
	float d_cos_m = wm_amd_pdf.w;
	wo = normalize(reflect(-wi, wm));
	float cos_m_n = wm.z;
	float cos_i_n = wi.z;
	float cos_o_n = wo.z;
	float cos_i_m = dot(wi, wm);
	float cos_o_m = cos_i_m;
	float step_i = (cos_i_n > 0.0) * (cos_i_m > 0.0);
	float step_o = (cos_o_n > 0.0) * (cos_o_m > 0.0);
	float alpha = Alpha_GGX_anisotropic(wm, ax, ay);
	float g = step_i * step_o * G2_GGX(cos_i_n, cos_o_n, alpha);
	float3 f = Fresnel_Schlick3(cos_i_m, f0);
	// BRDF        = F * G * D   / 4 * (cos_i_n * cos_o_n)
	// PDF         = D * cos_m_n / 4 * (cos_o_m)
	// BRDF_PER_PDF= F * G * cos_o_m / (cos_i_n * cos_o_n * cos_m_n)
	float tmp2 = (cos_i_n * cos_o_n * cos_m_n);
	if (tmp2 == 0.0) { pdf = 0.0; return float3(0.0, 0.0, 0.0); }
	pdf = d_cos_m * 0.25 / cos_o_m;
	return g * f * cos_o_m / tmp2;
}
float3 SampleAndEval_BRDF_GGX_NdotO_Per_D_NdotM_anisotropic(float3 wi, float2 rnd, float3 f0, float ax, float ay, out float3 wo, out float pdf) {
	float4 wm_amd_pdf = SampleAndEval_PDF_GGX_D_NdotM_anisotropic(rnd, ax, ay);
	float3 wm = normalize(float3(wm_amd_pdf.x, wm_amd_pdf.y, wm_amd_pdf.z));
	float d_cos_m = wm_amd_pdf.w;
	wo = normalize(reflect(-wi, wm));
	float cos_m_n = wm.z;
	float cos_i_n = wi.z;
	float cos_o_n = wo.z;
	float cos_i_m = dot(wi, wm);
	float cos_o_m = cos_i_m;
	float step_i = (cos_i_n > 0.0) * (cos_i_m > 0.0);
	float step_o = (cos_o_n > 0.0) * (cos_o_m > 0.0);
	float alpha  = Alpha_GGX_anisotropic(wm, ax, ay);
	float g = step_i * step_o * G2_GGX(cos_i_n, cos_o_n, alpha);
	float3 f = Fresnel_Schlick3(cos_i_m, f0);
	// BRDF        = F * G * D   / 4 * (cos_i_n * cos_o_n)
	// PDF         = D * cos_m_n / 4 * (cos_o_m)
	// BRDF_PER_PDF= F * G * cos_o_m / (cos_i_n * cos_o_n * cos_m_n)
	// BRDF_Cos_PER_PDF= F * G * cos_o_m / (cos_i_n * cos_m_n)
	float tmp2 = (cos_i_n * cos_m_n);
	if (tmp2 == 0.0) { pdf = 0.0; return float3(0.0, 0.0, 0.0); }
	pdf = d_cos_m * 0.25 / cos_o_m;
	return g * f * cos_o_m / tmp2;
}
#endif