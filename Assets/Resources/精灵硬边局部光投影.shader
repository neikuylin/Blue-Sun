Shader "项目/渲染/精灵硬边局部光投影"
{
    Properties
    {
        _Color ("颜色", Color) = (1,1,1,1)
        _Intensity ("强度", Range(0, 8)) = 1
        _Threshold ("硬边阈值", Range(0, 1)) = 0.18
        _Softness ("边缘过渡宽度", Range(0.001, 0.25)) = 0.03
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("深度测试", Float) = 8
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest [_ZTest]
        Blend One One
        ColorMask RGB

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            fixed _Intensity;
            fixed _Threshold;
            fixed _Softness;
            fixed _SpotEnabled;
            fixed2 _SpotDirection;
            fixed _SpotOuterCos;
            fixed _SpotSoftness;

            v2f vert(appdata input)
            {
                v2f output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed ResolveSpotMask(fixed2 centeredUv)
            {
                if (_SpotEnabled < 0.5)
                {
                    return 1;
                }

                fixed lengthSq = dot(centeredUv, centeredUv);
                if (lengthSq < 0.0001)
                {
                    return 1;
                }

                fixed2 direction = normalize(_SpotDirection);
                fixed2 sampleDirection = normalize(centeredUv);
                fixed angleCos = dot(sampleDirection, direction);
                return smoothstep(_SpotOuterCos, _SpotOuterCos + _SpotSoftness, angleCos);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed2 centeredUv = input.uv * 2 - 1;
                fixed distance01 = saturate(length(centeredUv));
                fixed attenuation = 1 - distance01;
                fixed radialMask = smoothstep(_Threshold, _Threshold + _Softness, attenuation);
                fixed spotMask = ResolveSpotMask(centeredUv);
                fixed lightMask = radialMask * spotMask;

                fixed3 color = _Color.rgb * _Intensity * lightMask;
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
}
