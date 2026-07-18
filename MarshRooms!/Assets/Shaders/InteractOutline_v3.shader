Shader "Custom/InteractOutline"
{
    Properties
    {
        _MainTex            ("Main Texture",        2D)     = "white" {}
        _OutlineColor       ("Outline Color",       Color)  = (1, 1, 1, 1)
        _OutlineThickness   ("Outline Thickness",   Float)  = 1.0
        _OutlineEnabled     ("Outline Enabled",     Float)  = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
            "RenderPipeline"  = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // In URP 17 we include CombinedShapeLightShared ONLY — 
            // it pulls in LightingUtility internally, including it again causes the redefinition error
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _OutlineColor;
                float  _OutlineThickness;
                float  _OutlineEnabled;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
                float4 lightingUV   : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color;
                OUT.lightingUV  = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            FragmentOutput frag(Varyings IN)
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 finalColor;

                if (_OutlineEnabled < 0.5)
                {
                    finalColor = texColor * IN.color;
                }
                else if (texColor.a > 0.5)
                {
                    finalColor = texColor * IN.color;
                }
                else
                {
                    float2 up    = IN.uv + float2( 0,  1) * _MainTex_TexelSize.xy * _OutlineThickness;
                    float2 down  = IN.uv + float2( 0, -1) * _MainTex_TexelSize.xy * _OutlineThickness;
                    float2 right = IN.uv + float2( 1,  0) * _MainTex_TexelSize.xy * _OutlineThickness;
                    float2 left  = IN.uv + float2(-1,  0) * _MainTex_TexelSize.xy * _OutlineThickness;

                    float aU = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, up   ).a;
                    float aD = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, down ).a;
                    float aR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, right).a;
                    float aL = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, left ).a;

                    float outline = step(0.01, aU + aD + aR + aL);
                    finalColor = half4(_OutlineColor.rgb, _OutlineColor.a * outline);
                }

                // CombinedShapeLightShared.hlsl defines FragmentOutput and ColorOutput/LightOutput
                // This is the URP 17 way to apply 2D lighting
                return ColorFragmentTarget(finalColor, half4(0, 0, 0, 0), IN.lightingUV);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _OutlineColor;
                float  _OutlineThickness;
                float  _OutlineEnabled;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                clip(texColor.a - 0.01);
                return half4(0.5, 0.5, 1, 1);
            }
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
