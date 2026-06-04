Shader "项目/特效/武器火焰实体拖尾"
{
    Properties
    {
        _ColorA ("外侧颜色", Color) = (1,0.16,0.02,0.75)
        _ColorB ("内侧颜色", Color) = (1,0.82,0.18,0.95)
        _Age01 ("生命周期进度", Range(0, 1)) = 0
        _Softness ("边缘柔和", Range(0.01, 1)) = 0.35
        _NoiseScale ("火焰噪声密度", Range(0.1, 40)) = 8
        _NoiseStrength ("火焰破碎强度", Range(0, 1)) = 0.28
        _Intensity ("亮度", Range(0, 6)) = 1.8
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("深度测试", Float) = 8
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+120"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [_ZTest]
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

            fixed4 _ColorA;
            fixed4 _ColorB;
            float _Age01;
            float _Softness;
            float _NoiseScale;
            float _NoiseStrength;
            float _Intensity;

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                float2 u = f * f * (3 - 2 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

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
                float center = 1 - abs(input.uv.y - 0.5) * 2;
                float edge = smoothstep(0, _Softness, center);
                float fade = saturate(1 - _Age01);
                float headFade = saturate(1 - input.uv.x * 0.15);
                float noise = Noise(input.uv * _NoiseScale + float2(_Time.y * 1.7, -_Time.y * 2.1));
                float broken = saturate(1 - noise * _NoiseStrength * (0.4 + _Age01));

                fixed4 color = lerp(_ColorA, _ColorB, center);
                float alpha = color.a * edge * fade * fade * headFade * broken * input.color.a;
                return fixed4(color.rgb * input.color.rgb * _Intensity * broken, alpha);
            }
            ENDCG
        }
    }
}
