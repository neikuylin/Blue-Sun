Shader "项目/渲染/受光不写深度引用Sprite遮罩"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite贴图", 2D) = "white" {}
        _Color ("颜色", Color) = (1,1,1,1)
        _AmbientStrength ("环境光强度", Range(0, 1)) = 0.45
        _LightStrength ("主光强度", Range(0, 2)) = 1
        _FacingAmount ("受光方向权重", Range(0, 1)) = 0.65
        _ReferenceMaskTex ("引用遮罩贴图", 2D) = "white" {}
        _ReferenceMaskAlphaCutoff ("遮罩透明判定", Range(0, 1)) = 0.5
        _ReferenceMaskInvert ("遮罩外部显示", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("深度测试", Float) = 8
        [MaterialToggle] PixelSnap ("像素对齐", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry-100"
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
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

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
            fixed _AmbientStrength;
            fixed _LightStrength;
            fixed _FacingAmount;
            sampler2D _MainTex;
            sampler2D _ReferenceMaskTex;
            float4x4 _ReferenceMaskWorldToLocal;
            float4 _ReferenceMaskBounds;
            float4 _ReferenceMaskUvRect;
            float4 _ReferenceMaskFlip;
            float _ReferenceMaskAlphaCutoff;
            float _ReferenceMaskInvert;

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

            fixed ResolveReferenceMask(float3 worldPos)
            {
                float3 maskLocal = mul(_ReferenceMaskWorldToLocal, float4(worldPos, 1)).xyz;
                float2 mask01 = (maskLocal.xy - _ReferenceMaskBounds.xy) / max(_ReferenceMaskBounds.zw, float2(0.0001, 0.0001));

                if (_ReferenceMaskFlip.x > 0.5)
                {
                    mask01.x = 1 - mask01.x;
                }

                if (_ReferenceMaskFlip.y > 0.5)
                {
                    mask01.y = 1 - mask01.y;
                }

                fixed inBounds =
                    step(0, mask01.x) *
                    step(0, mask01.y) *
                    step(mask01.x, 1) *
                    step(mask01.y, 1);

                float2 maskUv = _ReferenceMaskUvRect.xy + mask01 * _ReferenceMaskUvRect.zw;
                fixed maskAlpha = tex2D(_ReferenceMaskTex, maskUv).a;
                fixed inside = inBounds * step(_ReferenceMaskAlphaCutoff, maskAlpha);
                return lerp(inside, 1 - inside, step(0.5, _ReferenceMaskInvert));
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, input.texcoord) * input.color;
                texColor.a *= ResolveReferenceMask(input.worldPos);

                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * _AmbientStrength;
                fixed directional = saturate(_WorldSpaceLightPos0.y * 0.5 + 0.5);
                fixed3 mainLight = _LightColor0.rgb * lerp(1, directional, _FacingAmount) * _LightStrength;
                fixed3 lighting = saturate(ambient + mainLight);

                texColor.rgb *= lighting;
                return texColor;
            }
            ENDCG
        }
    }
}
