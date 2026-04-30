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

            #define OCCLUSION_REVEAL_MAX 32

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
            int _OcclusionRevealEnabled;
            int _OcclusionRevealCount;
            float _OcclusionRevealRadiusPixels;
            float _OcclusionRevealSoftnessPixels;
            float4 _OcclusionRevealCenters[OCCLUSION_REVEAL_MAX];
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

            fixed ResolveOcclusionRevealAlpha(float4 screenPos)
            {
                if (_OcclusionRevealEnabled == 0 || _OcclusionRevealCount <= 0)
                {
                    return 1;
                }

                float2 screenUv = screenPos.xy / screenPos.w;
                float2 pixelPosition = screenUv * _ScreenParams.xy;
                float radius = max(_OcclusionRevealRadiusPixels, 0);
                float softness = max(_OcclusionRevealSoftnessPixels, 0);
                fixed alphaMultiplier = 1;

                for (int i = 0; i < OCCLUSION_REVEAL_MAX; i++)
                {
                    if (i >= _OcclusionRevealCount)
                    {
                        break;
                    }

                    float2 center = _OcclusionRevealCenters[i].xy;
                    float distancePixels = distance(pixelPosition, center);
                    fixed hole;
                    if (softness <= 0.001)
                    {
                        hole = distancePixels <= radius ? 1 : 0;
                    }
                    else
                    {
                        hole = 1 - smoothstep(radius, radius + softness, distancePixels);
                    }

                    alphaMultiplier *= 1 - hole;
                }

                return alphaMultiplier;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, input.texcoord) * input.color;
                texColor.a *= ResolveOcclusionRevealAlpha(input.screenPos);

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

            #define OCCLUSION_REVEAL_MAX 32

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
            int _OcclusionRevealEnabled;
            int _OcclusionRevealCount;
            float _OcclusionRevealRadiusPixels;
            float _OcclusionRevealSoftnessPixels;
            float4 _OcclusionRevealCenters[OCCLUSION_REVEAL_MAX];
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

            fixed ResolveOcclusionRevealAlpha(float4 screenPos)
            {
                if (_OcclusionRevealEnabled == 0 || _OcclusionRevealCount <= 0)
                {
                    return 1;
                }

                float2 screenUv = screenPos.xy / screenPos.w;
                float2 pixelPosition = screenUv * _ScreenParams.xy;
                float radius = max(_OcclusionRevealRadiusPixels, 0);
                float softness = max(_OcclusionRevealSoftnessPixels, 0);
                fixed alphaMultiplier = 1;

                for (int i = 0; i < OCCLUSION_REVEAL_MAX; i++)
                {
                    if (i >= _OcclusionRevealCount)
                    {
                        break;
                    }

                    float2 center = _OcclusionRevealCenters[i].xy;
                    float distancePixels = distance(pixelPosition, center);
                    fixed hole;
                    if (softness <= 0.001)
                    {
                        hole = distancePixels <= radius ? 1 : 0;
                    }
                    else
                    {
                        hole = 1 - smoothstep(radius, radius + softness, distancePixels);
                    }

                    alphaMultiplier *= 1 - hole;
                }

                return alphaMultiplier;
            }

            fixed4 fragAdd(v2f_add input) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, input.texcoord) * input.color;
                UNITY_LIGHT_ATTENUATION(attenuation, input, input.worldPos);
                attenuation *= ResolveOcclusionRevealAlpha(input.screenPos);

                fixed3 localLight = _LightColor0.rgb * attenuation * _LocalLightStrength;
                return fixed4(texColor.rgb * localLight, texColor.a);
            }
            ENDCG
        }
    }
}
