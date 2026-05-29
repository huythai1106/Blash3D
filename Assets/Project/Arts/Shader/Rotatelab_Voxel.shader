Shader "Rotatelab/Voxel" {
    Properties {
        // Đã đổi thành _Color để khớp với mpb.SetColor("_Color", color);
        [TCP2HeaderHelp(Base)] _Color ("Color", Color) = (1,1,1,1) 
        [TCP2ColorNoAlpha] _HColor ("Highlight Color", Color) = (0.75,0.75,0.75,1)
        [TCP2ColorNoAlpha] _SColor ("Shadow Color", Color) = (0.2,0.2,0.2,1)
        _BaseMap ("Albedo", 2D) = "white" {}
        
        [TCP2Separator] [TCP2Header(Ramp Shading)] _RampThreshold ("Threshold", Range(0.01, 1)) = 0.5
        _RampSmoothing ("Smoothing", Range(0.001, 1)) = 0.5
        
        [TCP2Separator] [TCP2HeaderHelp(Specular)] [TCP2ColorNoAlpha] [HDR] _SpecularColor ("Specular Color", Color) = (0.5,0.5,0.5,1)
        _SpecularRoughnessPBR ("Roughness", Range(0, 1)) = 0.5
        
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
        #pragma surface surf Toon fullforwardshadows
        #pragma target 3.0
        
        // BẬT TÍNH NĂNG INSTANCING TỪ BÊN TRONG SHADER
        #pragma multi_compile_instancing 

        float4 _HColor;
        float4 _SColor;
        sampler2D _BaseMap;
        float _RampThreshold;
        float _RampSmoothing;
        float4 _SpecularColor;
        float _SpecularRoughnessPBR;
        float4 _RimColor;
        float _RimMin;
        float _RimMax;
        sampler2D _BumpMap;

        // KHAI BÁO BỘ ĐỆM INSTANCING (Cực kỳ quan trọng để MaterialPropertyBlock không phá vỡ Batching)
        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
        UNITY_INSTANCING_BUFFER_END(Props)

        struct Input {
            float2 uv_BaseMap;
            float3 viewDir;
            float3 worldNormal;
            INTERNAL_DATA
        };

        // --- CUSTOM LIGHTING MODEL (TOON) ---
        float4 LightingToon(SurfaceOutput s, float3 lightDir, float3 viewDir, float atten) {
            float NdotL = dot(s.Normal, lightDir) * 0.5 + 0.5; 
            
            float ramp = smoothstep(_RampThreshold - _RampSmoothing * 0.5, _RampThreshold + _RampSmoothing * 0.5, NdotL);
            float3 toonLight = lerp(_SColor.rgb, _HColor.rgb, ramp);

            float3 halfVector = normalize(lightDir + viewDir);
            float NdotH = max(0, dot(s.Normal, halfVector));
            float gloss = 1.0 - _SpecularRoughnessPBR;
            
            float specPower = exp2(10.0 * gloss + 1.0); 
            float specTerm = pow(NdotH, specPower) * gloss;
            float3 specularLight = _SpecularColor.rgb * specTerm * ramp; 

            float3 finalColor = s.Albedo * _LightColor0.rgb * toonLight * atten;
            finalColor += specularLight * _LightColor0.rgb * atten;

            return float4(finalColor, s.Alpha);
        }

        // --- CẤU HÌNH BỀ MẶT ---
        void surf(Input IN, inout SurfaceOutput o) {
            
            // ĐỌC MÀU TỪ MATERIAL PROPERTY BLOCK CỦA TỪNG KHỐI VOXEL
            float4 instanceColor = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
            
            float4 texColor = tex2D(_BaseMap, IN.uv_BaseMap);
            
            // Nhân màu texture với màu truyền từ C# vào
            o.Albedo = texColor.rgb * instanceColor.rgb;
            o.Alpha = texColor.a * instanceColor.a;

            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BaseMap));

            float NdotV = max(0.0, dot(o.Normal, normalize(IN.viewDir)));
            float fresnel = 1.0 - NdotV;
            float rimIntensity = smoothstep(_RimMin, _RimMax, fresnel);
            
            o.Emission = _RimColor.rgb * rimIntensity * _RimColor.a;
        }
        ENDCG
    }
    
    Fallback "Diffuse"
    CustomEditor "ToonyColorsPro.ShaderGenerator.MaterialInspector_SG2"
}