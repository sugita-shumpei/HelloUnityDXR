#ifndef RANDOM_HLSL
#define RANDOM_HLSL
uint wangHash(uint seed)
{
    seed = (seed ^ 61) ^ (seed >> 16);
    seed *= 9;
    seed = seed ^ (seed >> 4);
    seed *= 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed;
}
uint  pcgHash(uint seed)
{
    uint state = seed * 747796405u + 2891336453u;
    uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
    return (word >> 22u) ^ word;
}

static uint          rand_seed;

void initSeed(uint seed)
{
    rand_seed = seed;
}
uint readSeed()
{
    return rand_seed;
}

uint randLCG()
{
    return rand_seed = (rand_seed * 1664525u + 1013904223u);
}
uint randXorshift()
{
    rand_seed ^= rand_seed << 13;
    rand_seed ^= rand_seed >> 17;
    rand_seed ^= rand_seed << 5;
    return rand_seed;
}
uint randPCG()
{
    uint state = rand_seed * 747796405u + 2891336453u;
    uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
    return rand_seed = (word >> 22u) ^ word;
}
float4 uintToNormalizedFloat4(uint value)
{
    return float4(
        (value & 0x000000FF) / 255.0f,
        ((value & 0x0000FF00) >> 8) / 255.0f,
        ((value & 0x00FF0000) >> 16) / 255.0f,
        ((value & 0xFF000000) >> 24) / 255.0f
    );
}
float  uintToNormalizedFloat(uint value) {
    return float(value) * (1.0 / 4294967296.0);
}
float3 uniformInCosineHemisphere(float2 uv) {
    float phi = 2 * 3.14159265 * uv.x;
    float cosTheta = sqrt(uv.y);
    float sinTheta = sqrt(1 - uv.y);
    return float3(cos(phi) * sinTheta, sin(phi) * sinTheta, cosTheta);
}
#endif