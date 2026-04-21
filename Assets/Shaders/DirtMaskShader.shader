Shader "Custom/URPDirtMasked"
{
    Properties
    {
        _BaseMap("Dirt Image / Texture", 2D) = "white" {}
        _MaskTex("Eraser Mask (from script)", 2D) = "white" {}
        _BaseColor("Color Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        // This is a Transparent shader, meant to sit on top of the magic circle
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample the base dirt texture
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                
                // Sample our dynamic mask texture from the DirtEraser script
                half4 maskColor = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, IN.uv);
                
                // Multiply the alpha of the dirt by the mask alpha
                // Assuming script clears mask with Color.clear (which has 0 alpha)
                baseColor.a *= maskColor.a;

                return baseColor;
            }
            ENDHLSL
        }
    }
}
