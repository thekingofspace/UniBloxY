Shader "Custom/Highlight"
{
    Properties
    {
        _Color              ("Base Color",             Color)        = (0.8, 0.8, 0.8, 1.0)
        _MainTex            ("Main Texture",           2D)           = "white" {}
        _FillColor          ("Fill Color",             Color)        = (0.2, 0.8, 1.0, 1.0)
        _OutlineColor       ("Outline Color",          Color)        = (1.0, 1.0, 1.0, 1.0)
        _FillTransparency   ("Fill Transparency",      Range(0, 1)) = 0.5
        _OutlineTransparency("Outline Transparency",   Range(0, 1)) = 0.0
        _OutlineWidth       ("Outline Width",          Range(0, 0.2)) = 0.05
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Overlay" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        LOD 100

        // -------- Pass 1: Lit body, always on top, tinted by fill --------
        Pass
        {
            Name "HighlightLit"
            Tags { "LightMode"="UniversalForward" }

            ZTest Always
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _FORWARD_PLUS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float4 _FillColor;
                float4 _OutlineColor;
                float  _FillTransparency;
                float  _OutlineTransparency;
                float  _OutlineWidth;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct A
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct V
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            V vert(A IN)
            {
                V OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vn = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS   = vn.normalWS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(V IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float3 albedo = _Color.rgb * tex.rgb;

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 lighting = mainLight.color *
                    saturate(dot(N, mainLight.direction)) *
                    mainLight.shadowAttenuation *
                    mainLight.distanceAttenuation;

                #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX) || USE_FORWARD_PLUS
                    InputData inputData = (InputData)0;
                    inputData.positionWS = IN.positionWS;
                    inputData.normalWS = N;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light l = GetAdditionalLight(lightIndex, IN.positionWS);
                        lighting += l.color *
                            saturate(dot(N, l.direction)) *
                            l.distanceAttenuation *
                            l.shadowAttenuation;
                    LIGHT_LOOP_END
                #endif

                float3 ambient = SampleSH(N) * 0.6 + 0.15;
                float3 lit = saturate(albedo * (lighting + ambient));

                // Roblox semantics: alpha-blend the fill color OVER the lit body.
                // FillTransparency = 0 → fully covered by fill, = 1 → no fill.
                float fillA = saturate(1.0 - _FillTransparency) * _FillColor.a;
                float3 finalRGB = lerp(lit, _FillColor.rgb, fillA);

                return half4(finalRGB, _Color.a);
            }
            ENDHLSL
        }

        // -------- Pass 2: Outline (silhouette via inflated normals) ------
        Pass
        {
            Name "HighlightOutline"
            Tags { "LightMode"="SRPDefaultUnlit" }

            ZTest Always
            ZWrite Off
            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float4 _FillColor;
                float4 _OutlineColor;
                float  _FillTransparency;
                float  _OutlineTransparency;
                float  _OutlineWidth;
            CBUFFER_END

            struct A
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct V { float4 positionCS : SV_POSITION; };

            V vert(A IN)
            {
                V OUT;
                float3 inflatedOS = IN.positionOS.xyz + IN.normalOS * _OutlineWidth;
                OUT.positionCS = TransformObjectToHClip(inflatedOS);
                return OUT;
            }

            half4 frag(V IN) : SV_Target
            {
                float a = (1.0 - saturate(_OutlineTransparency)) * _OutlineColor.a;
                return half4(_OutlineColor.rgb, a);
            }
            ENDHLSL
        }
    }
}
