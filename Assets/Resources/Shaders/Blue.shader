Shader "Custom/Blue"
{
    Properties
    {
        _Color       ("Base Color",  Color) = (0.15, 0.35, 0.95, 1)
        _LineColor   ("Line Color",  Color) = (0.02, 0.05, 0.20, 1)
        _Density     ("Cells Per Unit", Float) = 4.0
        _LineWidth   ("Line Width",  Range(0.0, 0.2)) = 0.04
        _ShadeStrength ("Cell Shade Variation", Range(0.0, 1.0)) = 0.25
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipelineMask"="UniversalPipelineMask" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _LineColor;
                float  _Density;
                float  _LineWidth;
                float  _ShadeStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 positionOS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs vn = GetVertexNormalInputs(IN.normalOS);
                OUT.positionHCS = vp.positionCS;
                OUT.positionWS  = vp.positionWS;
                OUT.positionOS  = IN.positionOS.xyz;
                OUT.normalWS    = vn.normalWS;
                return OUT;
            }

            float hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            half4 frag(Varyings IN) : SV_Target
            {

                float3 absN = abs(normalize(IN.normalWS));
                float3 cellPos = IN.positionOS * _Density;
                float3 cellId  = floor(cellPos);
                float3 fracPos = frac(cellPos);
                float3 distToEdge = min(fracPos, 1.0 - fracPos);

                float3 mask = 1.0 - step(0.6, absN);
                float edge = 1.0;
                edge = min(edge, mask.x > 0.5 ? distToEdge.x : 1.0);
                edge = min(edge, mask.y > 0.5 ? distToEdge.y : 1.0);
                edge = min(edge, mask.z > 0.5 ? distToEdge.z : 1.0);

                float lineMask = 1.0 - smoothstep(_LineWidth * 0.5, _LineWidth, edge);

                float h = hash13(cellId + 0.13);
                float3 baseCol = _Color.rgb * lerp(1.0 - _ShadeStrength, 1.0 + _ShadeStrength * 0.4, h);
                float3 albedo = lerp(baseCol, _LineColor.rgb, lineMask);

                float3 N = normalize(IN.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float ndotl = saturate(dot(N, mainLight.direction));
                float3 lighting = mainLight.color * ndotl * mainLight.shadowAttenuation;

                float3 ambient = SampleSH(N) * 0.6 + 0.15;

                float3 final = albedo * (lighting + ambient);
                return half4(final, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct V { float4 positionCS : SV_POSITION; };

            V ShadowVert(A IN)
            {
                V OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nrmWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 posCS = TransformWorldToHClip(ApplyShadowBias(posWS, nrmWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    posCS.z = min(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    posCS.z = max(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                OUT.positionCS = posCS;
                return OUT;
            }

            half4 ShadowFrag(V IN) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; };

            V DepthVert(A IN) { V o; o.positionCS = TransformObjectToHClip(IN.positionOS.xyz); return o; }
            half4 DepthFrag(V IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
