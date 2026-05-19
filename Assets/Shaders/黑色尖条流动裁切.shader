Shader "项目/特效/黑色尖条流动裁切"
{
    Properties
    {
        _Color ("颜色", Color) = (1,1,1,1)
        _ClipRect ("裁切范围", Vector) = (-1,-1,1,1)
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
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 localPos : TEXCOORD0;
            };

            fixed4 _Color;
            float4 _ClipRect;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.color = input.color * _Color;
                output.localPos = input.vertex.xy;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                clip(input.localPos.x - _ClipRect.x);
                clip(input.localPos.y - _ClipRect.y);
                clip(_ClipRect.z - input.localPos.x);
                clip(_ClipRect.w - input.localPos.y);
                return input.color;
            }
            ENDCG
        }
    }
}
