#ifndef BLUE_SUN_OCCLUSION_REVEAL_COMMON_INCLUDED
#define BLUE_SUN_OCCLUSION_REVEAL_COMMON_INCLUDED

#ifndef OCCLUSION_REVEAL_MAX
#define OCCLUSION_REVEAL_MAX 32
#endif

int _OcclusionRevealEnabled;
int _OcclusionRevealCount;
int _OcclusionRevealDepthMode;
float _OcclusionRevealRadiusPixels;
float _OcclusionRevealSoftnessPixels;
float _OcclusionRevealAnchorDepthKey;
float4 _OcclusionRevealCenters[OCCLUSION_REVEAL_MAX];
float _DissolveNoiseScale;
float _DissolveStrength;
float _DissolveEdgeWidth;
float _DissolveScrollSpeed;
int _DissolveSmoothEdges;

float OcclusionRevealHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float OcclusionRevealValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3 - 2 * f);

    float a = OcclusionRevealHash21(i);
    float b = OcclusionRevealHash21(i + float2(1, 0));
    float c = OcclusionRevealHash21(i + float2(0, 1));
    float d = OcclusionRevealHash21(i + float2(1, 1));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

float OcclusionRevealOrganicNoise(float2 p)
{
    float2 flameP = float2(p.x * 0.85, p.y * 0.45);
    float2 warp = float2(
        OcclusionRevealValueNoise(flameP * 0.8 + float2(11.7, _Time.y * 0.12)),
        OcclusionRevealValueNoise(flameP * 0.65 + float2(27.3, _Time.y * 0.09))) - 0.5;

    flameP += warp * 1.8;
    float noise =
        OcclusionRevealValueNoise(flameP) * 0.55 +
        OcclusionRevealValueNoise(flameP * 2.15 + 19.31) * 0.3 +
        OcclusionRevealValueNoise(flameP * 4.2 + 43.17) * 0.15;

    return saturate(noise);
}

fixed OcclusionRevealHole(float distancePixels, float2 pixelPosition, float radius, float softness)
{
    float outerRadius = max(radius, 0.001);
    float fadeWidth = max(max(softness, _DissolveEdgeWidth), 0.001);
    float fadeStart = max(outerRadius - fadeWidth, 0);
    float revealDensity = 1 - smoothstep(fadeStart, outerRadius, distancePixels);
    revealDensity *= 1 - step(outerRadius, distancePixels);
    revealDensity *= saturate(_DissolveStrength) * 0.96;

    float cellSize = max(_DissolveNoiseScale, 1);
    float2 scrolledPixel = pixelPosition - float2(0, _Time.y * _DissolveScrollSpeed);
    float noise = OcclusionRevealOrganicNoise(scrolledPixel / cellSize);
    fixed softHole = smoothstep(noise - 0.12, noise + 0.12, revealDensity);
    fixed hardHole = step(noise, revealDensity);
    return _DissolveSmoothEdges != 0 ? softHole : hardHole;
}

fixed OcclusionRevealAlphaMultiplier(float4 screenPos)
{
    if (_OcclusionRevealEnabled == 0 || _OcclusionRevealCount <= 0 || _OcclusionRevealDepthMode == 0)
    {
        return 1;
    }

    float2 screenUv = screenPos.xy / screenPos.w;
    float2 pixelPosition = screenUv * _ScreenParams.xy;
    float radius = max(_OcclusionRevealRadiusPixels, 0);
    float softness = max(_OcclusionRevealSoftnessPixels, 0);
    fixed alphaMultiplier = 1;

    for (int i = 0; i < OCCLUSION_REVEAL_MAX; i++)
    {
        if (i >= _OcclusionRevealCount)
        {
            break;
        }

        if ((_OcclusionRevealDepthMode == 2 || _OcclusionRevealDepthMode == 3) &&
            _OcclusionRevealAnchorDepthKey > _OcclusionRevealCenters[i].w)
        {
            continue;
        }

        float distancePixels = distance(pixelPosition, _OcclusionRevealCenters[i].xy);
        fixed hole = OcclusionRevealHole(distancePixels, pixelPosition, radius, softness);
        alphaMultiplier *= 1 - hole;
    }

    return alphaMultiplier;
}

fixed OcclusionRevealShadowAlpha(float2 pixelPosition)
{
    if (_OcclusionRevealEnabled == 0 || _OcclusionRevealCount <= 0)
    {
        return 0;
    }

    float radius = max(_OcclusionRevealRadiusPixels, 0);
    float softness = max(_OcclusionRevealSoftnessPixels, 0);
    fixed alpha = 0;

    for (int i = 0; i < OCCLUSION_REVEAL_MAX; i++)
    {
        if (i >= _OcclusionRevealCount)
        {
            break;
        }

        float distancePixels = distance(pixelPosition, _OcclusionRevealCenters[i].xy);
        fixed hole = OcclusionRevealHole(distancePixels, pixelPosition, radius, softness);
        alpha = max(alpha, hole);
    }

    return alpha;
}

#endif
