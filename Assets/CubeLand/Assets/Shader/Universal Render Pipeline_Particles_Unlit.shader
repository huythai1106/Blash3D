Shader "Custom/URP_Particles_Unlit_Fixed" 
{
    Properties 
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        [HDR] _BaseColor ("Base Color", Color) = (1,1,1,1)
        
        // Các properties phụ trợ (Có thể bật tắt tùy nhu cầu)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10 // OneMinusSrcAlpha
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2 // Back
    }

    SubShader 
    {
        // Khai báo chuẩn cho URP Transparent
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True"
        }
        LOD 100

        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        Cull [_Cull]

        Pass 
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Thư viện lõi BẮT BUỘC của URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes 
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR; // Cực kỳ quan trọng để Particle System đổi màu/alpha
            };

            struct Varyings 
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            // Khai báo Texture chuẩn URP
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            // CBUFFER giúp tương thích SRP Batcher (Giảm SetPass Call triệt để)
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes input) 
            {
                Varyings output;
                
                // Hàm chuẩn của URP thay cho mul(unity_MatrixMVP, ...)
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target 
            {
                // Lấy màu từ Texture
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                
                // Trộn Texture * Màu của Material * Màu của Particle System (Vertex Color)
                half4 finalColor = texColor * _BaseColor * input.color;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}