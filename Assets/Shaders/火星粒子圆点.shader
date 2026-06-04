Shader "项目/特效/火星粒子圆点"
{
    Properties
    {
        _CoreColor ("核心颜色", Color) = (1,0.86,0.24,1)
        _GlowColor ("外发光颜色", Color) = (1,0.22,0.04,0.55)
        _CoreRadius ("核心半径", Range(0.02, 0.5)) = 0.18
        _GlowRadius ("外发光半径", Range(0.05, 0.8)) = 0.48
        _GlowSoftness ("外发光柔和度", Range(0.01, 1)) = 0.55
        _Intensity ("亮度", Range(0, 4)) = 1.4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            fixed4 _CoreColor;
            fixed4 _GlowColor;
            float _CoreRadius;
            float _GlowRadius;
            float _GlowSoftness;
            float _Intensity;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 centered = input.uv - 0.5;
                float distanceToCenter = length(centered);

                float core = 1 - smoothstep(_CoreRadius * 0.72, _CoreRadius, distanceToCenter);
                float glowOuter = 1 - smoothstep(_GlowRadius * (1 - _GlowSoftness), _GlowRadius, distanceToCenter);
                float glow = saturate(glowOuter - core * 0.25);

                fixed3 rgb = (_GlowColor.rgb * glow * _GlowColor.a + _CoreColor.rgb * core * _CoreColor.a) * _Intensity;
                float alpha = saturate(glow * _GlowColor.a + core * _CoreColor.a) * input.color.a;
                return fixed4(rgb * input.color.rgb, alpha);
            }
            ENDCG
        }
    }
}
