Shader "项目/特效/武器火焰附魔模型"
{
    Properties
    {
        _MainTex ("主贴图", 2D) = "white" {}
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
        _Metallic ("金属度", Range(0,1)) = 0
        _Glossiness ("光滑度", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 250

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _DarkFireColor;
        fixed4 _MainFireColor;
        fixed4 _CoreFireColor;
        float _FireIntensity;
        float _FireSpeed;
        float _FireScale;
        float4 _FlowDirection;
        float _OriginalKeep;
        float _FlickerStrength;
        float _FlickerSpeed;
        half _Metallic;
        half _Glossiness;

        struct Input
        {
            float2 uv_MainTex;
        };

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
            return saturate(
                ValueNoise(p) * 0.52 +
                ValueNoise(p * 2.1 + 17.13) * 0.31 +
                ValueNoise(p * 4.4 + 63.7) * 0.17);
        }

        fixed3 ResolveFireColor(float fire)
        {
            fixed3 lowToMain = lerp(_DarkFireColor.rgb, _MainFireColor.rgb, smoothstep(0.18, 0.72, fire));
            return lerp(lowToMain, _CoreFireColor.rgb, smoothstep(0.72, 1, fire));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            float2 direction = _FlowDirection.xy;
            direction = direction / max(length(direction), 0.0001);

            float fire = FireNoise(IN.uv_MainTex, direction);
            float fireMask = smoothstep(0.16, 0.94, fire);
            float flicker = 1 - _FlickerStrength + _FlickerStrength * (0.5 + 0.5 * sin(_Time.y * _FlickerSpeed + fire * 6.2831853));
            fixed3 fireColor = ResolveFireColor(fire) * _FireIntensity * flicker * fireMask;
            fixed3 finalColor = lerp(baseColor.rgb, baseColor.rgb + fireColor, saturate(_FireIntensity * (1 - _OriginalKeep)));

            o.Albedo = saturate(finalColor);
            o.Emission = fireColor * 0.55;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG

        Pass
        {
            Name "外扩火焰"
            Tags { "LightMode" = "Always" }
            Cull Front
            ZWrite Off
            Blend SrcAlpha One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _DarkFireColor;
            fixed4 _MainFireColor;
            fixed4 _CoreFireColor;
            float _FireIntensity;
            float _FireSpeed;
            float _FireScale;
            float4 _FlowDirection;
            float _OuterFireRange;
            float _OuterFireIntensity;
            float _FlickerStrength;
            float _FlickerSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

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
                float2 warp = float2(ValueNoise(p * 0.7 + float2(12.7, t * 0.18)), ValueNoise(p * 0.55 + float2(41.3, -t * 0.12))) - 0.5;
                p += warp * 1.35;
                return saturate(ValueNoise(p) * 0.52 + ValueNoise(p * 2.1 + 17.13) * 0.31 + ValueNoise(p * 4.4 + 63.7) * 0.17);
            }

            fixed3 ResolveFireColor(float fire)
            {
                fixed3 lowToMain = lerp(_DarkFireColor.rgb, _MainFireColor.rgb, smoothstep(0.18, 0.72, fire));
                return lerp(lowToMain, _CoreFireColor.rgb, smoothstep(0.72, 1, fire));
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 expanded = v.vertex.xyz + normalize(v.normal) * (_OuterFireRange * 0.01);
                o.pos = UnityObjectToClipPos(float4(expanded, 1));
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 direction = _FlowDirection.xy;
                direction = direction / max(length(direction), 0.0001);
                float fire = FireNoise(i.uv, direction);
                float flicker = 1 - _FlickerStrength + _FlickerStrength * (0.5 + 0.5 * sin(_Time.y * _FlickerSpeed + fire * 6.2831853));
                fixed3 color = ResolveFireColor(fire) * _OuterFireIntensity * flicker;
                float alpha = smoothstep(0.32, 1, fire) * saturate(_OuterFireRange / 8) * saturate(_OuterFireIntensity);
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }

    FallBack "Standard"
}
