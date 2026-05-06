Shader "Custom/TrailNeon"
{
    // Single-mesh additive trail. The shader fades alpha along the object's
    // local +Z axis (front of the trail, near the bullet, is brightest; the
    // back end fades to zero). One BasePart per bullet replaces the per-frame
    // ghost spawning entirely.
    //
    // Drive the per-instance tint via _Color (BasePart.ApplyColor writes it).
    // Hard-coded additive blend since this shader doesn't expose
    // _SrcBlend/_DstBlend, so BasePart.ApplyColor's blend writes are no-ops.
    Properties
    {
        _Color ("Color",      Color)        = (1, 0.42, 0.05, 1)
        _Glow  ("Glow",       Range(0, 8))  = 4
        _Fade  ("Fade Power", Range(0.5, 6)) = 1.6
        _Rim   ("Rim Boost",  Range(0, 4))  = 1.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+20" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend One One     // additive — overlapping trails brighten naturally
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _Glow;
                float  _Fade;
                float  _Rim;
            CBUFFER_END

            struct A
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct V
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            V vert(A IN)
            {
                V OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vn = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = vp.positionCS;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS   = vn.normalWS;
                return OUT;
            }

            half4 frag(V IN) : SV_Target
            {
                // Unit-cube object space spans Z in [-0.5, +0.5].
                // Map so the +Z face (front, near the bullet) = 1, back = 0.
                float t = saturate(IN.positionOS.z + 0.5);
                float fade = pow(t, _Fade);

                // Rim lift so silhouettes glow instead of going flat-shaded.
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                float NdotV = saturate(dot(N, V));
                float rim = (1.0 - NdotV) * _Rim;

                half3 col = _Color.rgb * _Glow * fade * (1.0 + rim);
                return half4(col, fade);
            }
            ENDHLSL
        }
    }
}
