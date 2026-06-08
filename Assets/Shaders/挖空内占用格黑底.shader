Shader "项目/战斗/挖空内占用格黑底"
{
    Properties
    {
        _Color ("颜色", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "OcclusionRevealCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _Color;
            sampler2D _ReferenceMaskTex;
            float4x4 _ReferenceMaskWorldToLocal;
            float4 _ReferenceMaskBounds;
            float4 _ReferenceMaskUvRect;
            float4 _ReferenceMaskFlip;
            float _ReferenceMaskAlphaCutoff;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.screenPos = ComputeScreenPos(output.vertex);
                output.worldPos = mul(unity_ObjectToWorld, input.vertex).xyz;
                return output;
            }

            fixed ResolveReferenceMask(float3 worldPos)
            {
                float2 localPos = mul(_ReferenceMaskWorldToLocal, float4(worldPos, 1)).xy;
                float2 localUv = (localPos - _ReferenceMaskBounds.xy) / max(_ReferenceMaskBounds.zw, float2(0.0001, 0.0001));
                localUv.x = lerp(localUv.x, 1 - localUv.x, step(0.5, _ReferenceMaskFlip.x));
                localUv.y = lerp(localUv.y, 1 - localUv.y, step(0.5, _ReferenceMaskFlip.y));

                fixed inside =
                    step(0, localUv.x) *
                    step(0, localUv.y) *
                    step(localUv.x, 1) *
                    step(localUv.y, 1);
                float2 spriteUv = _ReferenceMaskUvRect.xy + localUv * _ReferenceMaskUvRect.zw;
                fixed alpha = tex2D(_ReferenceMaskTex, spriteUv).a;
                return inside * step(max(_ReferenceMaskAlphaCutoff, 0), alpha);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 screenUv = input.screenPos.xy / input.screenPos.w;
                float2 pixelPosition = screenUv * _ScreenParams.xy;
                fixed alpha = OcclusionRevealShadowAlpha(pixelPosition);
                alpha *= ResolveReferenceMask(input.worldPos);
                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}
