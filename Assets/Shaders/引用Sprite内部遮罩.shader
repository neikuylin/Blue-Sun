Shader "项目/渲染/引用Sprite内部遮罩"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite贴图", 2D) = "white" {}
        _Color ("颜色", Color) = (1,1,1,1)
        _ReferenceMaskTex ("引用遮罩贴图", 2D) = "white" {}
        _ReferenceMaskAlphaCutoff ("遮罩透明判定", Range(0, 1)) = 0.5
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
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            sampler2D _ReferenceMaskTex;
            float4x4 _ReferenceMaskWorldToLocal;
            float4 _ReferenceMaskBounds;
            float4 _ReferenceMaskUvRect;
            float4 _ReferenceMaskFlip;
            float _ReferenceMaskAlphaCutoff;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                output.worldPos = mul(unity_ObjectToWorld, input.vertex).xyz;

                #ifdef PIXELSNAP_ON
                output.vertex = UnityPixelSnap(output.vertex);
                #endif

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.texcoord) * input.color;
                float3 maskLocal = mul(_ReferenceMaskWorldToLocal, float4(input.worldPos, 1)).xyz;
                float2 mask01 = (maskLocal.xy - _ReferenceMaskBounds.xy) / max(_ReferenceMaskBounds.zw, float2(0.0001, 0.0001));

                if (_ReferenceMaskFlip.x > 0.5)
                {
                    mask01.x = 1 - mask01.x;
                }

                if (_ReferenceMaskFlip.y > 0.5)
                {
                    mask01.y = 1 - mask01.y;
                }

                fixed inside =
                    step(0, mask01.x) *
                    step(0, mask01.y) *
                    step(mask01.x, 1) *
                    step(mask01.y, 1);
                float2 maskUv = _ReferenceMaskUvRect.xy + mask01 * _ReferenceMaskUvRect.zw;
                fixed maskAlpha = tex2D(_ReferenceMaskTex, maskUv).a;
                color.a *= inside * step(_ReferenceMaskAlphaCutoff, maskAlpha);
                return color;
            }
            ENDCG
        }
    }
}
