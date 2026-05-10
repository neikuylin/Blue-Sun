Shader "项目/特效/水纹半月扭曲"
{
    Properties
    {
        _Color ("水纹颜色", Color) = (1, 1, 1, 1)
        _DistortionStrength ("扭曲强度", Range(0, 1)) = 0.018
        _VisibleWidthScale ("可见弯月宽度倍率", Range(0.05, 1)) = 0.45
        _EdgeFade ("边缘淡出", Range(0.01, 0.5)) = 0.12
        _TintStrength ("颜色影响", Range(0, 1)) = 0.12
        _WaveFrequency ("波纹频率", Range(0.1, 4)) = 1.4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+60"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        GrabPass
        {
        }

        Pass
        {
            Cull Off
            Lighting Off
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;
            float4 _Color;
            float _DistortionStrength;
            float _VisibleWidthScale;
            float _EdgeFade;
            float _TintStrength;
            float _WaveFrequency;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 color : COLOR;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.screenPos = ComputeGrabScreenPos(output.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            float4 frag(v2f input) : SV_Target
            {
                float alpha = saturate(input.color.a);
                float tipFade = saturate(sin(saturate(input.uv.x) * UNITY_PI));
                float outerFade = smoothstep(0.0, max(_EdgeFade, 0.001), input.uv.y);
                float innerFade = smoothstep(0.0, max(_EdgeFade, 0.001), 1.0 - input.uv.y);
                float distortionMask = alpha * tipFade * outerFade * innerFade;

                float visibleScale = saturate(_VisibleWidthScale);
                float visibleStart = 1.0 - visibleScale;
                float visibleCoord = saturate((input.uv.y - visibleStart) / max(visibleScale, 0.001));
                float visibleOuterFade = smoothstep(0.0, max(_EdgeFade, 0.001), visibleCoord);
                float visibleInnerFade = smoothstep(0.0, max(_EdgeFade, 0.001), 1.0 - visibleCoord);
                float visibleMask = alpha * tipFade * visibleOuterFade * visibleInnerFade * step(visibleStart, input.uv.y);

                float side = input.uv.y * 2.0 - 1.0;
                float wave = sin((input.uv.x - 0.5) * UNITY_PI * 2.0 * _WaveFrequency);
                float2 offset = float2(side, wave * 0.35) * (_DistortionStrength * distortionMask);
                offset.y *= _GrabTexture_TexelSize.y / max(abs(_GrabTexture_TexelSize.x), 0.000001);

                float4 distortedScreenPos = input.screenPos;
                distortedScreenPos.xy += offset * input.screenPos.w;

                float3 background = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(input.screenPos)).rgb;
                float3 distorted = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(distortedScreenPos)).rgb;
                float3 tinted = lerp(distorted, distorted * input.color.rgb, saturate(_TintStrength));
                float3 filtered = lerp(background, tinted, distortionMask);
                float3 visibleTint = lerp(filtered, filtered * input.color.rgb, visibleMask * saturate(_TintStrength));
                float3 finalColor = lerp(filtered, visibleTint, visibleMask);
                return float4(finalColor, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
