Shader "Custom/Neon"
{
    // Unlit emissive with a rim-glow term that fakes a soft outer halo —
    // surfaces brighten at grazing angles, so the geometry reads as a glowing
    // shape instead of a flat-shaded primitive. Honors the same blend-state
    // properties as Custom/Default so BasePart.ApplyColor's transparency
    // toggling still drives this material correctly.
    Properties
    {
        _Color    ("Color",        Color)        = (1, 0.42, 0.05, 1)
        _Glow     ("Glow Strength", Range(0, 8)) = 3.0
        _RimPower ("Rim Power",     Range(0.5, 8)) = 2.0
        _RimBoost ("Rim Boost",     Range(0, 8)) = 3.0

        [HideInInspector] _SrcBlend ("Src Blend", Float) = 1
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 0
        [HideInInspector] _ZWrite   ("ZWrite",    Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _Glow;
                float  _RimPower;
                float  _RimBoost;
            CBUFFER_END

            struct A
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct V
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            V vert(A IN)
            {
                V OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs vn = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS   = vn.normalWS;
                return OUT;
            }

            half4 frag(V IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                float NdotV = saturate(dot(N, V));
                float rim = pow(1.0 - NdotV, _RimPower);
                float intensity = _Glow + rim * _RimBoost;
                half3 col = _Color.rgb * intensity;
                return half4(col, _Color.a);
            }
            ENDHLSL
        }
    }
}
