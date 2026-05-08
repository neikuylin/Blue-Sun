Shader "项目/特效/粒子Alpha遮挡挖空"
{
    Properties
    {
        _MainTex ("粒子贴图", 2D) = "white" {}
        _Color ("颜色", Color) = (1,1,1,1)
        _DissolveNoiseScale ("挖空边缘颗粒尺寸（像素）", Range(1, 32)) = 6
        _DissolveStrength ("挖空边缘颗粒强度", Range(0, 1)) = 0.45
        _DissolveEdgeWidth ("挖空颗粒边缘宽度（像素）", Range(0, 128)) = 18
        _DissolveScrollSpeed ("挖空颗粒滚动速度（像素/秒）", Range(-256, 256)) = 48
        [Toggle] _DissolveSmoothEdges ("挖空颗粒边缘融合", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            #define OCCLUSION_REVEAL_MAX 32

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float eyeDepth : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            int _OcclusionRevealEnabled;
            int _OcclusionRevealCount;
            int _OcclusionRevealDepthMode;
            float _OcclusionRevealRadiusPixels;
            float _OcclusionRevealSoftnessPixels;
            float _OcclusionRevealAnchorScreenY;
            float4 _OcclusionRevealCenters[OCCLUSION_REVEAL_MAX];
            float _DissolveNoiseScale;
            float _DissolveStrength;
            float _DissolveEdgeWidth;
            float _DissolveScrollSpeed;
            int _DissolveSmoothEdges;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;
                output.screenPos = ComputeScreenPos(output.vertex);
                output.eyeDepth = -UnityObjectToViewPos(input.vertex.xyz).z;
                return output;
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

            fixed ResolveOcclusionRevealAlpha(float4 screenPos, float eyeDepth)
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

                    float2 center = _OcclusionRevealCenters[i].xy;
                    if (_OcclusionRevealDepthMode == 2 && _OcclusionRevealAnchorScreenY > _OcclusionRevealCenters[i].w)
                    {
                        continue;
                    }

                    float distancePixels = distance(pixelPosition, center);
                    fixed hole = ResolveRevealHole(distancePixels, pixelPosition, radius, softness);

                    alphaMultiplier *= 1 - hole;
                }

                return alphaMultiplier;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, input.texcoord) * input.color;
                texColor.a *= ResolveOcclusionRevealAlpha(input.screenPos, input.eyeDepth);
                return texColor;
            }
            ENDCG
        }
    }
}
