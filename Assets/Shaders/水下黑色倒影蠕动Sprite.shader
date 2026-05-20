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
        [HideInInspector] _DirectionalFlow ("方向蠕动开关", Float) = 0
        [HideInInspector] _FlowDirection ("方向蠕动向量", Vector) = (0, 0, 0, 0)
        _DirectionalIntensity ("方向激烈程度", Range(0, 4)) = 1.8
        _DirectionalWaveScale ("方向蠕动密度", Range(0.1, 20)) = 8
        _DirectionalSpeed ("方向推进速度", Range(0, 8)) = 2.4
        _DirectionalSidePull ("横切撕扯", Range(0, 3)) = 1.1
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
            float _DirectionalFlow;
            float4 _FlowDirection;
            float _DirectionalIntensity;
            float _DirectionalWaveScale;
            float _DirectionalSpeed;
            float _DirectionalSidePull;

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

                if (_DirectionalFlow > 0.5)
                {
                    float2 flowDirection = _FlowDirection.xy / max(length(_FlowDirection.xy), 0.0001);
                    float2 sideDirection = float2(-flowDirection.y, flowDirection.x);
                    float2 centeredUv = uv - 0.5;
                    float mainAxis = dot(centeredUv, flowDirection);
                    float sideAxis = dot(centeredUv, sideDirection);
                    float directionalTime = _Time.y * _DirectionalSpeed;
                    float waveScale = max(0.001, _DirectionalWaveScale);

                    float sweep = sin((mainAxis * waveScale - directionalTime) * 6.2831853);
                    float sideWave = ValueNoise(float2(sideAxis * waveScale * 1.7, directionalTime * 0.85)) * 2 - 1;
                    float brokenWave = ValueNoise(float2(mainAxis * waveScale + directionalTime * 0.42, sideAxis * waveScale)) * 2 - 1;
                    float directionalWeight = 0.65 + 0.35 * abs(sweep);

                    float2 directionalOffset =
                        flowDirection * (sweep * 0.7 + brokenWave * 0.45) +
                        sideDirection * sideWave * _DirectionalSidePull;

                    offset += directionalOffset * _DistortStrength * _DirectionalIntensity * edgeWeight * directionalWeight;
                }

                fixed4 texColor = tex2D(_MainTex, uv + offset);
                texColor *= input.color;
                return texColor;
            }
            ENDCG
        }
    }
}
