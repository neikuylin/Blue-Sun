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
            };

            fixed4 _Color;
            int _OcclusionRevealEnabled;
            int _OcclusionRevealCount;
            float _OcclusionRevealRadiusPixels;
            float _OcclusionRevealSoftnessPixels;
            float4 _OcclusionRevealCenters[OCCLUSION_REVEAL_MAX];

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.screenPos = ComputeScreenPos(output.vertex);
                return output;
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
                    fixed hole = softness > 0
                        ? 1 - smoothstep(max(0, radius - softness), radius, distancePixels)
                        : step(distancePixels, radius);
                    alpha = max(alpha, hole);
                }

                return alpha;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 screenUv = input.screenPos.xy / input.screenPos.w;
                float2 pixelPosition = screenUv * _ScreenParams.xy;
                fixed alpha = ResolveHoleAlpha(pixelPosition);
                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}
