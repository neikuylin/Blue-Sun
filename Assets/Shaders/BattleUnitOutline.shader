Shader "Battle/UnitOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Float) = 0.03
        _StencilRef ("Stencil Ref", Float) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Geometry+11" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
        Cull Front
        Lighting Off
        ZWrite On
        ZTest LEqual
        Blend Off

        Pass
        {
            Stencil
            {
                Ref [_StencilRef]
                Comp NotEqual
                Pass Keep
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _OutlineColor;
            float _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 normal = normalize(v.normal);
                float4 expandedVertex = v.vertex + float4(normal * _OutlineWidth, 0.0);
                o.pos = UnityObjectToClipPos(expandedVertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}
