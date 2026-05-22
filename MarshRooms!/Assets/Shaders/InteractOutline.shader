Shader "Custom/InteractOutline"
{
    Properties
    {
        _MainTex        ("Main Texture",    2D)     = "white" {}
        _OutlineColor   ("Outline Color",   Color)  = (1, 1, 0, 1)
        _OutlineThickness ("Outline Thickness", Float) = 1.0
        _OutlineEnabled ("Outline Enabled", Float)  = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _MainTex_TexelSize;   // Unity auto-fills: (1/w, 1/h, w, h)
            float4    _OutlineColor;
            float     _OutlineThickness;
            float     _OutlineEnabled;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);

                // Early-out: outline is off, just render normally
                if (_OutlineEnabled < 0.5)
                    return texColor * i.color;

                // If this pixel is already opaque, return it as-is
                if (texColor.a > 0.5)
                    return texColor * i.color;

                // Sample the four cardinal neighbours
                float2 up    = i.uv + float2( 0,  1) * _MainTex_TexelSize.xy * _OutlineThickness;
                float2 down  = i.uv + float2( 0, -1) * _MainTex_TexelSize.xy * _OutlineThickness;
                float2 right = i.uv + float2( 1,  0) * _MainTex_TexelSize.xy * _OutlineThickness;
                float2 left  = i.uv + float2(-1,  0) * _MainTex_TexelSize.xy * _OutlineThickness;

                float alphaUp    = tex2D(_MainTex, up   ).a;
                float alphaDown  = tex2D(_MainTex, down ).a;
                float alphaRight = tex2D(_MainTex, right).a;
                float alphaLeft  = tex2D(_MainTex, left ).a;

                // If any neighbour is solid, this transparent pixel is on the outline
                float outline = step(0.01, alphaUp + alphaDown + alphaRight + alphaLeft);

                // Blend outline colour; keep transparent where there is no outline
                fixed4 outlinePixel = fixed4(_OutlineColor.rgb, _OutlineColor.a * outline);
                return outlinePixel;
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
