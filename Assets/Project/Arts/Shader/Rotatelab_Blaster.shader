Shader "Rotatelab/Blaster" {
   Properties {
        // Đổi Vector -> Color để hiện Color Picker
        [TCP2HeaderHelp(Base)] _BaseColor ("Color", Color) = (1,1,1,1)
        [TCP2ColorNoAlpha] _HColor ("Highlight Color", Color) = (1,1,1,1)
        [TCP2ColorNoAlpha] _SColor ("Shadow Color", Color) = (0.2,0.2,0.2,1)
        
        [Toggle(_ALBEDO_MAP_OFF)] _AlbedoMapOff ("Disable Albedo Map (UV)", Float) = 0
        [KeywordEnum(UV0, UV1, UV2, UV3)] _AlbedoUV ("Albedo UV Channel", Float) = 0
        _BaseMap ("Albedo", 2D) = "white" {}
        
        [TCP2Separator] [TCP2Header(Ramp Shading)] _RampThreshold ("Threshold", Range(0.01, 1)) = 0.5
        _RampSmoothing ("Smoothing", Range(0.001, 1)) = 0.5
        
        // Đổi Vector -> Color cho Specular và Rim
        [TCP2Separator] [TCP2HeaderHelp(Specular)] [TCP2ColorNoAlpha] [HDR] _SpecularColor ("Specular Color", Color) = (0.5,0.5,0.5,1)
        _SpecularToonSize ("Toon Size", Range(0, 1)) = 0.1
        _SpecularToonSmoothness ("Toon Smoothness", Range(0.001, 1)) = 1
        
        [TCP2Separator] [TCP2HeaderHelp(Rim Lighting)] [TCP2ColorNoAlpha] [HDR] _RimColor ("Rim Color", Color) = (0.8,0.8,0.8,0.5)
        _RimMin ("Rim Min", Range(0, 2)) = 0.5
        _RimMax ("Rim Max", Range(0, 2)) = 1
        
        [TCP2Separator] [TCP2HeaderHelp(Normal Mapping)] [NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
        [TCP2Separator] [HideInInspector] __dummy__ ("unused", Float) = 0
    }

    SubShader {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200

        CGPROGRAM
        // Khai báo Surface Shader sử dụng Custom Lighting Model tên là 'Toon'
        #pragma surface surf Toon fullforwardshadows
        #pragma target 3.0

        // Liên kết biến từ Properties
        sampler2D _BaseMap;
        sampler2D _BumpMap;
        float4 _BaseColor;
        float4 _HColor;
        float4 _SColor;
        
        float _RampThreshold;
        float _RampSmoothing;
        
        float4 _SpecularColor;
        float _SpecularToonSize;
        float _SpecularToonSmoothness;
        
        float4 _RimColor;
        float _RimMin;
        float _RimMax;

        struct Input {
            float2 uv_BaseMap;
            float2 uv_BumpMap;
            float3 viewDir;
        };

        // --- CUSTOM LIGHTING MODEL (Tái tạo logic Toony Colors Pro) ---
        float4 LightingToon(SurfaceOutput s, float3 lightDir, float3 viewDir, float atten) {
            // 1. Diffuse (Half-Lambert)
            float NdotL = dot(s.Normal, lightDir) * 0.5 + 0.5;
            
            // 2. Toon Ramp (Phân tách mảng sáng tối)
            float ramp = smoothstep(_RampThreshold - _RampSmoothing, _RampThreshold + _RampSmoothing, NdotL);
            float3 rampColor = lerp(_SColor.rgb, _HColor.rgb, ramp);
            
            // 3. Toon Specular (Phản quang cục bộ kiểu Anime/Cartoony)
            float3 halfVector = normalize(lightDir + viewDir);
            float NdotH = max(0, dot(s.Normal, halfVector));
            float specRaw = pow(NdotH, 64.0); // Phong power
            float specSmooth = smoothstep(_SpecularToonSize - _SpecularToonSmoothness, _SpecularToonSize + _SpecularToonSmoothness, specRaw);
            float3 specularColor = _SpecularColor.rgb * specSmooth;

            // 4. Tổng hợp ánh sáng
            float3 finalColor = s.Albedo * _LightColor0.rgb * rampColor * atten;
            finalColor += specularColor * _LightColor0.rgb * atten;

            return float4(finalColor, s.Alpha);
        }

        // --- BƯỚC XỬ LÝ BỀ MẶT (Surface) ---
        void surf(Input IN, inout SurfaceOutput o) {
            // Lấy màu từ Texture và Color
            float4 texColor = tex2D(_BaseMap, IN.uv_BaseMap);
            o.Albedo = texColor.rgb * _BaseColor.rgb;
            o.Alpha = texColor.a * _BaseColor.a;

            // Normal Map
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));

            // Rim Lighting (Phát sáng viền)
            float NdotV = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            float rimIntensity = smoothstep(_RimMin, _RimMax, NdotV);
            o.Emission = _RimColor.rgb * rimIntensity * _RimColor.a;
        }
        ENDCG
    }
    
    Fallback "Diffuse"
    // Nếu bạn có ToonyColorsPro trong project, hãy bỏ comment dòng dưới đây để bật UI của nó
    CustomEditor "ToonyColorsPro.ShaderGenerator.MaterialInspector_SG2"
}