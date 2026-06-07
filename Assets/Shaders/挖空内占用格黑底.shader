Shader "项目/战斗/挖空内占用格黑底"
{
    Properties
    {
        _Color ("颜色", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define OCCLUSION_REVEAL_MAX 32

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _Color;
            int _OcclusionRevealEnabled;
            int _OcclusionRevealCount;
            float _OcclusionRevealRadiusPixels;
            float _OcclusionRevealSoftnessPixels;
            float4 _OcclusionRevealCenters[OCCLUSION_REVEAL_MAX];
            sampler2D _ReferenceMaskTex;
            float4x4 _ReferenceMaskWorldToLocal;
            float4 _ReferenceMaskBounds;
            float4 _ReferenceMaskUvRect;
            float4 _ReferenceMaskFlip;
            float _ReferenceMaskAlphaCutoff;
            float _DissolveNoiseScale;
            float _DissolveStrength;
            float _DissolveEdgeWidth;
            float _DissolveScrollSpeed;
            int _DissolveSmoothEdges;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.screenPos = ComputeScreenPos(output.vertex);
                output.worldPos = mul(unity_ObjectToWorld, input.vertex).xyz;
                return output;
            }

            fixed ResolveReferenceMask(float3 worldPos)
            {
                float2 localPos = mul(_ReferenceMaskWorldToLocal, float4(worldPos, 1)).xy;
                float2 localUv = (localPos - _ReferenceMaskBounds.xy) / max(_ReferenceMaskBounds.zw, float2(0.0001, 0.0001));
                localUv.x = lerp(localUv.x, 1 - localUv.x, step(0.5, _ReferenceMaskFlip.x));
                localUv.y = lerp(localUv.y, 1 - localUv.y, step(0.5, _ReferenceMaskFlip.y));

                fixed inside =
                    step(0, localUv.x) *
                    step(0, localUv.y) *
                    step(localUv.x, 1) *
                    step(localUv.y, 1);
                float2 spriteUv = _ReferenceMaskUvRect.xy + localUv * _ReferenceMaskUvRect.zw;
                fixed alpha = tex2D(_ReferenceMaskTex, spriteUv).a;
                return inside * step(max(_ReferenceMaskAlphaCutoff, 0), alpha);
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3 - 2 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float OrganicDissolveNoise(float2 p)
            {
                float2 flameP = float2(p.x * 0.85, p.y * 0.45);
                float2 warp = float2(
                    ValueNoise(flameP * 0.8 + float2(11.7, _Time.y * 0.12)),
                    ValueNoise(flameP * 0.65 + float2(27.3, _Time.y * 0.09))) - 0.5;

                flameP += warp * 1.8;
                float noise =
                    ValueNoise(flameP) * 0.55 +
                    ValueNoise(flameP * 2.15 + 19.31) * 0.3 +
                    ValueNoise(flameP * 4.2 + 43.17) * 0.15;

                return saturate(noise);
            }

            fixed ResolveRevealHole(float distancePixels, float2 pixelPosition, float radius, float softness)
            {
                float outerRadius = max(radius, 0.001);
                float fadeWidth = max(max(softness, _DissolveEdgeWidth), 0.001);
                float fadeStart = max(outerRadius - fadeWidth, 0);
                float revealDensity = 1 - smoothstep(fadeStart, outerRadius, distancePixels);
                revealDensity *= 1 - step(outerRadius, distancePixels);
                revealDensity *= saturate(_DissolveStrength) * 0.96;

                float cellSize = max(_DissolveNoiseScale, 1);
                float2 scrolledPixel = pixelPosition - float2(0, _Time.y * _DissolveScrollSpeed);
                float noise = OrganicDissolveNoise(scrolledPixel / cellSize);
                fixed softHole = smoothstep(noise - 0.12, noise + 0.12, revealDensity);
                fixed hardHole = step(noise, revealDensity);
                return _DissolveSmoothEdges != 0 ? softHole : hardHole;
            }

            fixed ResolveHoleAlpha(float2 pixelPosition)
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
                    fixed hole = ResolveRevealHole(distancePixels, pixelPosition, radius, softness);
                    alpha = max(alpha, hole);
                }

                return alpha;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 screenUv = input.screenPos.xy / input.screenPos.w;
                float2 pixelPosition = screenUv * _ScreenParams.xy;
                fixed alpha = ResolveHoleAlpha(pixelPosition);
                alpha *= ResolveReferenceMask(input.worldPos);
                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}
