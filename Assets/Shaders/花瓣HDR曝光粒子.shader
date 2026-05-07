Shader "项目/特效/花瓣HDR曝光粒子"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _Color ("Material Tint", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        [HDR] _BloomColor ("Bloom Color", Color) = (1,1,1,1)
        _BloomIntensity ("Bloom Intensity", Float) = 8
        _ExposureThreshold ("Exposure Marker Threshold", Range(0, 1)) = 0.97
        [HideInInspector] _SrcBlend ("Src Blend", Float) = 5
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 10
        [HideInInspector] _ZWrite ("Z Write", Float) = 0
        [HideInInspector] _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Cull [_Cull]
        Lighting Off
        ZWrite [_ZWrite]
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _BloomColor;
            float _BloomIntensity;
            float _ExposureThreshold;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 particleColor : TEXCOORD1;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                output.particleColor = input.color;
                return output;
            }

            float4 frag(v2f input) : SV_Target
            {
                float4 tex = tex2D(_MainTex, input.uv);
                float4 color = tex * input.color;

                float exposureMarker = min(input.particleColor.r, min(input.particleColor.g, input.particleColor.b));
                float exposure = smoothstep(_ExposureThreshold, 1.0, exposureMarker);
                float3 hdrColor = color.rgb * max(_BloomIntensity, 1.0) * _BloomColor.rgb;
                color.rgb = lerp(color.rgb, hdrColor, exposure);

                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
