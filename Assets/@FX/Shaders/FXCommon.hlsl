#ifndef BLUES_WITH_YOU_FX_COMMON_INCLUDED
#define BLUES_WITH_YOU_FX_COMMON_INCLUDED

float FXHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float FXNoise2D(float2 p)
{
    float2 cell = floor(p);
    float2 local = frac(p);
    local = local * local * (3.0 - 2.0 * local);
    float a = FXHash21(cell);
    float b = FXHash21(cell + float2(1.0, 0.0));
    float c = FXHash21(cell + float2(0.0, 1.0));
    float d = FXHash21(cell + 1.0);
    return lerp(lerp(a, b, local.x), lerp(c, d, local.x), local.y);
}

float3 FXWaveHeightGradient(float2 worldXZ, float time, float scale)
{
    float2 p = worldXZ * scale;
    float a = p.x * 1.13 + time;
    float b = p.y * 1.37 - time * 1.21;
    float c = (p.x + p.y) * 0.71 + time * 0.73;
    float height = sin(a) * 0.50 + sin(b) * 0.34 + sin(c) * 0.16;
    float dx = (cos(a) * 0.565 + cos(c) * 0.1136) * scale;
    float dz = (cos(b) * 0.4658 + cos(c) * 0.1136) * scale;
    return float3(height, dx, dz);
}

half FXRadialMask(float2 uv, half feather)
{
    half radius = length((half2)uv * 2.0h - 1.0h);
    return 1.0h - smoothstep(1.0h - feather, 1.0h, radius);
}

#endif
