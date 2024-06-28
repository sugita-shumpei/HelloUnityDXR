#pragma once
#include "RTUtils.hlsl"
#include "Random.hlsl"
#include "ONB.hlsl"
struct RayPayloadData {
    float3  throughput;
    uint    seed;
    float3  emission;
    float3  radiance;
    float3  origin;
    float3  direction;
    int     done;
};
