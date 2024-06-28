#ifndef RTUTILS_HLSL
#define RTUTILS_HLSL
float2 triangleInterpolation2(float2 value_0, float2 value_1, float2 value_2, float2 uv)
{
    return value_0 + (value_1 - value_0) * uv.x + (value_2 - value_0) * uv.y;
}
float3 triangleInterpolation3(float3 value_0, float3 value_1, float3 value_2, float2 uv)
{
    return value_0 + (value_1 - value_0) * uv.x + (value_2 - value_0) * uv.y;
}
float4 triangleInterpolation4(float4 value_0, float4 value_1, float4 value_2, float2 uv)
{
    return value_0 + (value_1 - value_0) * uv.x + (value_2 - value_0) * uv.y;
}

float3 transformObjectToWorldPosition(float3 position)
{
    return mul(ObjectToWorld3x4(), float4(position, 1));
}
float3 transformObjectToWorldNormal(float3 normal)
{
    return normalize(mul(ObjectToWorld3x4(), float4(normal, 0)));
}
float3 transformObjectToWorldDirection(float3 direction)
{
    return mul(ObjectToWorld3x4(), float4(direction, 0));
}

float3 transformWorldToObjectPosition(float3 position)
{
    return mul(WorldToObject3x4(), float4(position, 1));
}
float3 transformWorldToObjectNormal(float3 normal)
{
    return normalize(mul(WorldToObject3x4(), float4(normal, 0)));
}
float3 transformWorldToObjectDirection(float3 direction)
{
    return mul(WorldToObject3x4(), float4(direction, 0));
}
#endif