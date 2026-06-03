Shader "项目/特效/武器火焰附魔Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite贴图", 2D) = "white" {}
        _Color ("颜色", Color) = (1,1,1,1)
        _DarkFireColor ("暗部火焰颜色", Color) = (0.75,0.08,0.02,1)
        _MainFireColor ("主火焰颜色", Color) = (1,0.32,0.04,1)
        _CoreFireColor ("核心火焰颜色", Color) = (1,0.9,0.28,1)
        _FireIntensity ("火焰强度", Range(0, 4)) = 1.15
        _FireSpeed ("火焰速度", Range(0, 10)) = 2.4
        _FireScale ("火焰密度", Range(0.1, 40)) = 11
        _FlowDirection ("流动方向", Vector) = (0,1,0,0)
        _OriginalKeep ("原图保留强度", Range(0, 1)) = 0.72
        _OuterFireRange ("外扩火焰范围", Range(0, 8)) = 2
        _OuterFireIntensity ("外扩火焰强度", Range(0, 4)) = 1.3
        _FlickerStrength ("闪烁强度", Range(0, 1)) = 0.22
        _FlickerSpeed ("闪烁速度", Range(0, 12)) = 4
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
            #pragma target 3.0
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
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _DarkFireColor;
            fixed4 _MainFireColor;
            fixed4 _CoreFireColor;
            float _FireIntensity;
            float _FireSpeed;
            float _FireScale;
            float4 _FlowDirection;
            float _OriginalKeep;
            float _OuterFireRange;
            float _OuterFireIntensity;
            float _FlickerStrength;
            float _FlickerSpeed;

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

            float FireNoise(float2 uv, float2 direction)
            {
                float t = _Time.y * _FireSpeed;
                float2 side = float2(-direction.y, direction.x);
                float2 p = float2(dot(uv, side), dot(uv, direction)) * _FireScale;
                p.y -= t;

                float2 warp = float2(
                    ValueNoise(p * 0.7 + float2(12.7, t * 0.18)),
                    ValueNoise(p * 0.55 + float2(41.3, -t * 0.12))) - 0.5;

                p += warp * 1.35;
                float noise =
                    ValueNoise(p) * 0.52 +
                    ValueNoise(p * 2.1 + 17.13) * 0.31 +
                    ValueNoise(p * 4.4 + 63.7) * 0.17;

                return saturate(noise);
            }

            float NearbyAlpha(float2 uv, float rangePixels)
            {
                float2 stepUv = _MainTex_TexelSize.xy * max(rangePixels, 0);
                float alpha = 0;
                alpha = max(alpha, tex2D(_MainTex, uv + float2(stepUv.x, 0)).a);
                alpha = max(alpha, tex2D(_MainTex, uv + float2(-stepUv.x, 0)).a);
                alpha = max(alpha, tex2D(_MainTex, uv + float2(0, stepUv.y)).a);
                alpha = max(alpha, tex2D(_MainTex, uv + float2(0, -stepUv.y)).a);
                alpha = max(alpha, tex2D(_MainTex, uv + stepUv).a);
                alpha = max(alpha, tex2D(_MainTex, uv - stepUv).a);
                alpha = max(alpha, tex2D(_MainTex, uv + float2(stepUv.x, -stepUv.y)).a);
                alpha = max(alpha, tex2D(_MainTex, uv + float2(-stepUv.x, stepUv.y)).a);
                alpha = max(alpha, tex2D(_MainTex, uv + stepUv * 0.5).a);
                alpha = max(alpha, tex2D(_MainTex, uv - stepUv * 0.5).a);
                return saturate(alpha);
            }

            fixed3 ResolveFireColor(float fire)
            {
                fixed3 lowToMain = lerp(_DarkFireColor.rgb, _MainFireColor.rgb, smoothstep(0.18, 0.72, fire));
                return lerp(lowToMain, _CoreFireColor.rgb, smoothstep(0.72, 1, fire));
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 baseColor = tex2D(_MainTex, input.texcoord) * input.color;
                float2 direction = _FlowDirection.xy;
                direction = direction / max(length(direction), 0.0001);

                float fire = FireNoise(input.texcoord, direction);
                float flicker = 1 - _FlickerStrength + _FlickerStrength * (0.5 + 0.5 * sin(_Time.y * _FlickerSpeed + fire * 6.2831853));
                float fireMask = smoothstep(0.16, 0.94, fire);
                fixed3 fireColor = ResolveFireColor(fire) * _FireIntensity * flicker * fireMask;

                float innerAlpha = baseColor.a;
                float nearbyAlpha = NearbyAlpha(input.texcoord, _OuterFireRange);
                float outerAlpha = saturate(nearbyAlpha - innerAlpha) * smoothstep(0.05, 1, _OuterFireRange);
                float outerNoise = smoothstep(0.24, 0.95, fire) * _OuterFireIntensity * flicker;

                fixed3 innerRgb = lerp(baseColor.rgb, baseColor.rgb + fireColor, saturate(_FireIntensity * (1 - _OriginalKeep)));
                fixed3 outerRgb = fireColor;

                fixed4 result;
                result.rgb = lerp(outerRgb, innerRgb, step(0.001, innerAlpha));
                result.a = saturate(innerAlpha + outerAlpha * outerNoise);

                if (innerAlpha > 0.001)
                {
                    result.rgb = innerRgb;
                    result.a = innerAlpha;
                }

                return result;
            }
            ENDCG
        }
    }
}
