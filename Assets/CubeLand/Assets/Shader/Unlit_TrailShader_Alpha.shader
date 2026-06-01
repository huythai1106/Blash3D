Shader "Unlit/TrailShader_Alpha"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1,1,1,1) // Dùng Color để hiển thị bảng màu trên Inspector
    }
    
    SubShader
    {
        // 1. Cấu hình chuẩn cho Trail/Particle (Trong suốt, không bị lỗi Z-fighting)
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        
        // Trộn alpha tiêu chuẩn (Alpha Blending)
        Blend SrcAlpha OneMinusSrcAlpha
        
        // Hiển thị cả 2 mặt của Trail và tắt ghi đè chiều sâu
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Tích hợp thư viện chuẩn để xử lý ma trận và UV
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                // BẮT BUỘC phải có COLOR để nhận tín hiệu mờ dần từ TrailRenderer
                float4 color : COLOR; 
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _TintColor;

            v2f vert (appdata_t v)
            {
                v2f o;
                // Hàm chuẩn của Unity, tự động tối ưu hóa cho SRP Batcher/Dynamic Batching
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Tổ hợp màu: Texture * Màu đỉnh (Fade của Trail) * Tint Color (Tùy chỉnh)
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color * _TintColor;
                return col;
            }
            ENDCG
        }
    }
}