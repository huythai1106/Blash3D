Shader "Unlit/TrailShader" {
    Properties {
        _MainTex ("Particle Texture", 2D) = "white" {}
        // Sửa Vector thành Color để hiển thị Color Picker trên Inspector
        _TintColor ("Tint Color", Color) = (1,1,1,1) 
    }

    SubShader {
        // Cấu hình chuẩn cho Trail / Particle
        Tags { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
        }
        LOD 100

        // Tắt ZWrite để các vệt trail đè lên nhau không bị lỗi z-fighting hoặc viền đen
        ZWrite Off
        
        // Alpha Blending cơ bản (Nếu muốn sáng chói như lửa/laser thì đổi thành: Blend SrcAlpha One)
        Blend SrcAlpha OneMinusSrcAlpha 
        
        // Render cả mặt trước và sau (rất quan trọng với Trail)
        Cull Off 

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float4 color : COLOR; // Nhận màu Gradient từ TrailRenderer
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _TintColor;

            v2f vert (appdata_t v)
            {
                v2f o;
                // Transform đỉnh sang Clip Space
                o.vertex = UnityObjectToClipPos(v.vertex); 
                // Xử lý UV Tiling/Offset nếu có
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex); 
                // Nhân Vertex Color (từ component) với Tint Color
                o.color = v.color * _TintColor; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample texture và nhân với tổng màu đã tính ở Vertex
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;
                return col;
            }
            ENDCG
        }
    }
}