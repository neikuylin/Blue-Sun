Shader "项目/特效/水下黑色倒影蠕动Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite贴图", 2D) = "white" {}
        _Color ("颜色", Color) = (0,0,0,0.65)
        _DistortStrength ("蠕动强度", Range(0, 0.25)) = 0.035
        _DistortSpeed ("蠕动速度", Range(0, 5)) = 1.2
        _DistortScale ("蠕动密度", Range(0.1, 20)) = 6
        _HorizontalPull ("横向拉扯", Range(0, 3)) = 1.4
        _VerticalPull ("纵向拉扯", Range(0, 3)) = 0.55
        _EdgeWobble ("边缘扰动", Range(0, 1)) = 0.35
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("深度测试", Float) = 8
        [MaterialToggle] PixelSnap ("像素对齐", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [_ZTest]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

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
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _DistortStrength;
            float _DistortSpeed;
            float _DistortScale;
            float _HorizontalPull;
            float _VerticalPull;
            float _EdgeWobble;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;

                #ifdef PIXELSNAP_ON
                output.vertex = UnityPixelSnap(output.vertex);
                #endif

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

            fixed4 frag(v2f input) : SV_Target
            {
                float time = _Time.y * _DistortSpeed;
                float2 uv = input.texcoord;
                float2 noiseUv = uv * _DistortScale;

                float horizontal = ValueNoise(noiseUv + float2(time * 0.75, time * 0.18)) * 2 - 1;
                float vertical = ValueNoise(noiseUv + float2(19.7 - time * 0.22, 8.3 + time * 0.62)) * 2 - 1;
                float edge = abs(uv.x - 0.5) * 2;
                float edgeWeight = lerp(1, edge, _EdgeWobble);

                float2 offset = float2(
                    horizontal * _HorizontalPull,
                    vertical * _VerticalPull) * _DistortStrength * edgeWeight;

                fixed4 texColor = tex2D(_MainTex, uv + offset);
                texColor *= input.color;
                return texColor;
            }
            ENDCG
        }
    }
}
