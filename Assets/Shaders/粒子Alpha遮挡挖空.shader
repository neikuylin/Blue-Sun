Shader "项目/特效/粒子Alpha遮挡挖空"
{
    Properties
    {
        _MainTex ("粒子贴图", 2D) = "white" {}
        _Color ("颜色", Color) = (1,1,1,1)
        _DissolveNoiseScale ("挖空边缘颗粒尺寸（像素）", Range(1, 32)) = 6
        _DissolveStrength ("挖空边缘颗粒强度", Range(0, 1)) = 0.45
        _DissolveEdgeWidth ("挖空颗粒边缘宽度（像素）", Range(0, 128)) = 18
        _DissolveScrollSpeed ("挖空颗粒滚动速度（像素/秒）", Range(-256, 256)) = 48
        [Toggle] _DissolveSmoothEdges ("挖空颗粒边缘融合", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "OcclusionRevealCommon.cginc"

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
                float4 screenPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;
                output.screenPos = ComputeScreenPos(output.vertex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, input.texcoord) * input.color;
                texColor.a *= OcclusionRevealAlphaMultiplier(input.screenPos);
                return texColor;
            }
            ENDCG
        }
    }
}
