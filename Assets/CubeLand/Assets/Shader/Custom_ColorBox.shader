Shader "Custom/ColorBox" {
	Properties {
		[Header(Base)] _MainTex ("Base Texture", 2D) = "white" {}
		_MainColor ("Base Color", Vector) = (1,1,1,1)
		[Toggle(_USE_BASE_MULTIPLY)] _UseBaseMultiply ("Use Base Multiply", Float) = 0
		_BaseMultiplyTex ("Base Multiply Texture", 2D) = "white" {}
		_BaseMultiplyStrength ("Base Multiply Strength", Range(0, 1)) = 1
		_Alpha ("Alpha", Range(0, 1)) = 1
		[Space(8)] [Header(Overlay)] [Toggle(_USE_OVERLAY)] _UseOverlay ("Use Overlay", Float) = 1
		_OverlayTex ("Overlay Texture", 2D) = "black" {}
		_OverlayColor ("Overlay Color", Vector) = (1,1,1,1)
		_OverlayVisibility ("Overlay Visibility", Range(0, 1)) = 1
		_OverlayIntensity ("Overlay Intensity", Range(0, 2)) = 1
		[Toggle(_USE_OVERLAY_MASK2)] _UseOverlayMask2 ("Use Overlay Mask 2", Float) = 0
		_OverlayMask2Tex ("Overlay Mask 2 Texture", 2D) = "white" {}
		_OverlayMask2Scale ("Overlay Mask 2 Scale", Range(0.1, 8)) = 1
		_OverlayMask2ScaleXYZ ("Overlay Mask 2 Scale XYZ", Vector) = (1,1,1,0)
		_OverlayMask2Softness ("Overlay Mask 2 Softness", Range(0.001, 0.5)) = 0.08
		[Space(8)] [Header(Lighting)] _Smoothness ("Smoothness", Range(0, 1)) = 0.4
		_Specular ("Specular", Range(0, 1)) = 0.2
		[Space(8)] [Header(Shadow Ramp)] [Toggle(_USE_SHADOW_RAMP)] _UseShadowRamp ("Use Shadow Ramp", Float) = 1
		_RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.75
		_RampSmoothing ("Ramp Smoothing", Range(0.001, 1)) = 0.35
		_ShadowTint ("Shadow Tint", Vector) = (1,1,1,1)
		[Space(8)] [Header(Melt)] [Toggle(_USE_MELT)] _UseMelt ("Use Melt", Float) = 1
		_MeltAmount ("Melt Amount", Range(0, 1)) = 0
		_MeltSoftness ("Melt Softness", Range(0.001, 0.5)) = 0.08
		_MeltAxis ("Melt Axis (0=X, 1=Y, 2=Z)", Range(0, 2)) = 1
		[Space(8)] [Header(Normal Map)] [Toggle(_USE_NORMALMAP)] _UseNormalMap ("Use Normal Map", Float) = 1
		_BumpMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Normal Strength", Range(0, 2)) = 1
		[Space(8)] [Header(Rendering)] [Toggle(_DOUBLE_SIDED)] _DoubleSided ("Double Sided", Float) = 0
		[HideInInspector] _Surface ("__surface", Float) = 1
		[HideInInspector] _Cull ("__cull", Float) = 2
		[HideInInspector] _AlphaClip ("__alphaclip", Float) = 0
		[HideInInspector] _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
		[HideInInspector] _SrcBlend ("__src", Float) = 5
		[HideInInspector] _DstBlend ("__dst", Float) = 10
		[HideInInspector] _ZWrite ("__zw", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_MatrixMVP;

			struct Vertex_Stage_Input
			{
				float3 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixMVP, float4(input.pos, 1.0));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState _MainTex_sampler;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(_MainTex_sampler, float2(input.uv.x, input.uv.y));
			}

			ENDHLSL
		}
	}
	//CustomEditor "FrozenShaderGUI"
}