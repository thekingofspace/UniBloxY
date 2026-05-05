Shader "Custom/UIGradient"
{
    // Designed for UnityEngine.UI.Image / Text. Sprite alpha drives the cutout,
    // vertex color (Graphic.color) tints, and uniforms add a vertical gradient
    // + an inner glow that fades from the rect's edge.
    Properties
    {
        _Color      ("Top Color",     Color) = (0.20, 0.55, 1.00, 1)
        _ColorB     ("Bottom Color",  Color) = (0.05, 0.10, 0.35, 1)
        _GlowColor  ("Glow Color",    Color) = (0.45, 0.85, 1.00, 0.6)
        _GlowPower  ("Glow Power",    Range(0.5, 16)) = 6
        _Sheen      ("Sheen Strength", Range(0, 1)) = 0.25
        _MainTex    ("Sprite Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _ColorB;
            float4 _GlowColor;
            float  _GlowPower;
            float  _Sheen;

            v2f vert(appdata IN)
            {
                v2f OUT;
                OUT.pos = UnityObjectToClipPos(IN.vertex);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float4 tex = tex2D(_MainTex, IN.uv);

                // Vertical gradient between top/bottom colors.
                float3 grad = lerp(_Color.rgb, _ColorB.rgb, IN.uv.y);

                // Inner glow: distance from the nearest edge, falling off to 0 in the middle.
                float dx = min(IN.uv.x, 1.0 - IN.uv.x);
                float dy = min(IN.uv.y, 1.0 - IN.uv.y);
                float edgeDist = min(dx, dy);
                float edge = 1.0 - saturate(edgeDist * _GlowPower);
                edge *= edge;

                // Diagonal sheen sweep across the rect.
                float sheen = saturate(1.0 - abs((IN.uv.x + IN.uv.y) - 1.0) * 4.0) * _Sheen;

                float3 col = grad + _GlowColor.rgb * edge * _GlowColor.a + sheen;
                col *= IN.color.rgb;

                float a = tex.a * IN.color.a;
                return fixed4(col, a);
            }
            ENDCG
        }
    }
}
