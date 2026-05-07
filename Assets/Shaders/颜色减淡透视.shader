Shader "项目/特效/颜色减淡透视"
{
    Properties
    {
        [PerRendererData] _MainTex ("遮罩贴图", 2D) = "white" {}
        _Color ("减淡颜色倍率", Color) = (1, 1, 1, 0.75)
        [Toggle] _UseSpriteColor ("吸取SpriteRenderer颜色", Float) = 1
        _Strength ("减淡强度", Range(0, 0.95)) = 0.45
        _Opacity ("整体影响", Range(0, 1)) = 1
        _FresnelStrength ("边缘增强", Range(0, 1)) = 0.25
        _FresnelPower ("边缘范围", Range(0.5, 8)) = 3
        _MaxBrightness ("最大亮度", Range(1, 8)) = 3
        [HideInInspector] _RendererColor ("SpriteRenderer颜色", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+50"
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
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _RendererColor;
            float _UseSpriteColor;
            float _Strength;
            float _Opacity;
            float _FresnelStrength;
            float _FresnelPower;
            float _MaxBrightness;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                float4 color : COLOR;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.screenPos = ComputeGrabScreenPos(output.vertex);

                float3 worldPos = mul(unity_ObjectToWorld, input.vertex).xyz;
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.viewDir = UnityWorldSpaceViewDir(worldPos);
                output.color = input.color * _RendererColor;
                return output;
            }

            float3 ColorDodge(float3 background, float3 dodgeColor, float strength, float maxBrightness)
            {
                float3 denominator = max(1.0 - saturate(dodgeColor) * saturate(strength), 0.02);
                return min(background / denominator, max(maxBrightness, 1.0));
            }

            float4 frag(v2f input) : SV_Target
            {
                float3 background = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(input.screenPos)).rgb;
                float4 texColor = tex2D(_MainTex, input.uv);
                float mask = texColor.a * input.color.a * _Color.a;
                float3 materialDodgeColor = _Color.rgb;
                float3 rendererDodgeColor = saturate(input.color.rgb * _Color.rgb);
                float3 dodgeColor = lerp(materialDodgeColor, rendererDodgeColor, saturate(_UseSpriteColor));

                float3 normalDir = normalize(input.worldNormal);
                float3 viewDir = normalize(input.viewDir);
                float fresnel = pow(1.0 - saturate(dot(normalDir, viewDir)), max(_FresnelPower, 0.001));

                float edgeBoost = 1.0 + fresnel * _FresnelStrength;
                float effect = saturate(mask * _Opacity * edgeBoost);
                float dodgeStrength = saturate(_Strength * edgeBoost);

                float3 dodged = ColorDodge(background, dodgeColor, dodgeStrength, _MaxBrightness);
                float3 finalColor = lerp(background, dodged, effect);
                return float4(finalColor, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
