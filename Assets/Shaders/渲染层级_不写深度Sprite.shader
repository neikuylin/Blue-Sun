Shader "项目/渲染/渲染层级受光不写深度Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite贴图", 2D) = "white" {}
        _Color ("颜色", Color) = (1,1,1,1)
        _AmbientStrength ("环境光强度", Range(0, 1)) = 0.45
        _LightStrength ("主光强度", Range(0, 2)) = 1
        _LocalLightStrength ("局部光强度", Range(0, 2)) = 1
        _FacingAmount ("受光方向权重", Range(0, 1)) = 0.65
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("深度测试", Float) = 8
        _DissolveNoiseScale ("挖空边缘颗粒尺寸（像素）", Range(1, 32)) = 6
        _DissolveStrength ("挖空边缘颗粒强度", Range(0, 1)) = 0.45
        _DissolveEdgeWidth ("挖空颗粒边缘宽度（像素）", Range(0, 128)) = 18
        _DissolveScrollSpeed ("挖空颗粒滚动速度（像素/秒）", Range(-256, 256)) = 48
        [Toggle] _DissolveSmoothEdges ("挖空颗粒边缘融合", Float) = 1
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

            fixed4 _Color;
            fixed _AmbientStrength;
            fixed _LightStrength;
            fixed _FacingAmount;
            sampler2D _MainTex;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                output.screenPos = ComputeScreenPos(output.vertex);

                #ifdef PIXELSNAP_ON
                output.vertex = UnityPixelSnap(output.vertex);
                #endif

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, input.texcoord) * input.color;
                texColor.a *= OcclusionRevealAlphaMultiplier(input.screenPos);

                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * _AmbientStrength;
                fixed directional = saturate(_WorldSpaceLightPos0.y * 0.5 + 0.5);
                fixed3 mainLight = _LightColor0.rgb * lerp(1, directional, _FacingAmount) * _LightStrength;
                fixed3 lighting = saturate(ambient + mainLight);

                texColor.rgb *= lighting;
                return texColor;
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode" = "ForwardAdd" }
            Blend SrcAlpha One
            ColorMask RGB

            CGPROGRAM
            #pragma vertex vertAdd
            #pragma fragment fragAdd
            #pragma target 3.0
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile_fwdadd
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "OcclusionRevealCommon.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f_add
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                UNITY_SHADOW_COORDS(3)
            };

            fixed4 _Color;
            fixed _LocalLightStrength;
            sampler2D _MainTex;

            v2f_add vertAdd(appdata_t input)
            {
                v2f_add output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                output.worldPos = mul(unity_ObjectToWorld, input.vertex).xyz;
                output.screenPos = ComputeScreenPos(output.pos);

                #ifdef PIXELSNAP_ON
                output.pos = UnityPixelSnap(output.pos);
                #endif

                UNITY_TRANSFER_SHADOW(output, input.texcoord);
                return output;
            }

            fixed4 fragAdd(v2f_add input) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, input.texcoord) * input.color;
                UNITY_LIGHT_ATTENUATION(attenuation, input, input.worldPos);
                attenuation *= OcclusionRevealAlphaMultiplier(input.screenPos);

                fixed3 localLight = _LightColor0.rgb * attenuation * _LocalLightStrength;
                return fixed4(texColor.rgb * localLight, texColor.a);
            }
            ENDCG
        }
    }
}
