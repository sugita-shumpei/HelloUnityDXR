#ifndef UNITY_DISTRIBUTION_INCLUDED
#define UNITY_DISTRIBUTION_INCLUDED

float3 UniformInUnitSphere(float2 uv)
{
    float cos_tht = 1.0 - 2.0 * uv.x;
    float sin_tht = sqrt(1.0 - cos_tht * cos_tht);
    float phi     = 2.0 * 3.14159265359 * uv.y;
    return float3(sin_tht * cos(phi), sin_tht * sin(phi), cos_tht);
}
float3 UniformInUnitHemiSphere(float2 uv)
{
    float cos_tht = 1.0 - uv.x;
    float sin_tht = sqrt(1.0 - cos_tht * cos_tht);
    float phi     = 2.0 * 3.14159265359 * uv.y;
    return float3(sin_tht * cos(phi), sin_tht * sin(phi), cos_tht);
}
float3 UniformInCosineHemiSphere(float2 uv)
{
    float cos_tht = sqrt(1.0 - uv.x);
    float sin_tht = sqrt(uv.x);
    float phi     = 2.0 * 3.14159265359 * uv.y;
    return float3(sin_tht * cos(phi), sin_tht * sin(phi), cos_tht);
}
float3 UniformInPhongHemiSphere(float2 uv, float shinness)
{
    float cos_tht  = pow(1.0 - uv.x, 1.0 / (shinness + 1.0));
    float  sin_tht = sqrt(1.0 - cos_tht * cos_tht);
    float  phi     = 2.0 * 3.14159265359 * uv.y;
    return float3(sin_tht * cos(phi), sin_tht * sin(phi), cos_tht);
}
float3 UniformInGGXNormalCosineHemiSphere(float2 uv, float alpha)
{
    float cos_tht2= max((1.0 - uv.x) / (1.0 + (alpha * alpha - 1.0) * uv.x), 0.0);
    float sin_tht = sqrt(max(1.0 - cos_tht2,0.0));
	float cos_tht = sqrt(cos_tht2);
    float phi     = 2.0 * 3.14159265359 * uv.y;
    return float3(sin_tht * cos(phi), sin_tht * sin(phi), cos_tht);
}
float3 UniformInAnisotropicGGXNormalCosineHemiSphere(float2 uv, float alpha_x, float alpha_y)
{
    //float cos_tht2 = max((1.0 - uv.x) / (1.0 + (alpha_x * alpha_x - 1.0) * uv.x), 0.0);
    //float sin_tht  = sqrt(max(1.0 - cos_tht2, 0.0));
    //float cos_tht  = sqrt(cos_tht2);
    //float phi = 2.0 * 3.14159265359 * uv.y;
    //return float3(sin_tht * cos(phi), sin_tht * sin(phi), cos_tht);
    float uni_phi         = 2.0 * 3.14159265359 * uv.y;
    float cos_uni_phi     = cos(uni_phi);
    float sin_uni_phi     = sin(uni_phi);
	float ay_sin_uni_phi  = alpha_y * sin_uni_phi;
	float ax_cos_uni_phi  = alpha_x * cos_uni_phi;
	float ay_sin_uni_phi2 = ay_sin_uni_phi * ay_sin_uni_phi;
	float ax_cos_uni_phi2 = ax_cos_uni_phi * ax_cos_uni_phi;
	float inv_len_2       = 1.0 / (ax_cos_uni_phi2 + ay_sin_uni_phi2);
	float inv_len  = sqrt(inv_len_2);
	float cos_phi2 = ax_cos_uni_phi2 * inv_len_2;
	float sin_phi2 = ay_sin_uni_phi2 * inv_len_2;
	float cos_phi = ax_cos_uni_phi * inv_len;
	float sin_phi = ay_sin_uni_phi * inv_len;
	float cos_phi2_per_ax_2 = cos_phi2 / (alpha_x * alpha_x);
	float sin_phi2_per_ay_2 = sin_phi2 / (alpha_y * alpha_y);
	float tan_tht2 = max(uv.x / (1.0 - uv.x), 0.0) / (cos_phi2_per_ax_2 + sin_phi2_per_ay_2);
	float cos_tht2 = 1.0 / (1.0 + tan_tht2);
    float sin_tht = sqrt(max(1.0 - cos_tht2, 0.0));
    float cos_tht = sqrt(cos_tht2);
    return float3(sin_tht * cos_phi, sin_tht * sin_phi, cos_tht);
}
float  DensityInUnitSphere(float3 dir)
{
    return 1.0 / (4.0 * 3.14159265359);
}
float  DensityInUnitHemiSphere(float3 dir)
{
    if (dir.z <= 0.0)
    {
        return 0.0;
    }
    return 1.0 / (2.0 * 3.14159265359);
}
float DensityInCosineHemiSphere(float3 dir)
{
    return max(dir.z, 0.0) / 3.14159265359;
}
float DensityInPhongHemiSphere(float3 dir, float shinness)
{
    if (dir.z <= 0.0)
    {
        return float3(0, 0, 0);
    }
    return (shinness + 1.0) * pow(dir.z, shinness) / 2.0;
}
float DensityInGGXNormalCosineHemiSphere(float3 dir, float alpha)
{
    float cos_tht  = max(dir.z, 0.0);
    float cos_tht2 = cos_tht * cos_tht;
    float sin_tht  = sqrt(1.0 - cos_tht2);
    float alpha2   = alpha * alpha;
    float temp     = 1.0 + cos_tht2 * (alpha2 - 1.0);
    float D = alpha2 / (3.14159265359 * temp * temp);
    return D * cos_tht;
}

float DensityInAnisotropicGGXNormalCosineHemiSphere(float3 dir, float alpha_x, float alpha_y) {
    float xn  = dir.x / alpha_x;
    float yn  = dir.y / alpha_y;
    float zn  = max(dir.z, 0.0);
    float xn2 = xn * xn;
    float yn2 = yn * yn;
    float zn2 = zn * zn;
    float v1 = (xn2 + yn2 + zn2);
    return zn / (3.14159265359 * alpha_x * alpha_y * v1 * v1);
}

#endif
