// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "KT/MYVERYVERYCOOLSHADER"{
	Properties {
		_MainTex("Main Tex (Blend 0) (RGB)", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_XAmp ("XAmplitude", Range(0, 1)) = 0
		_YAmp ("YAmplitude", Range(0, 1)) = 0
		_XFreq ("XFrequency", Range(0, 10)) = 0
		_YFreq ("YFrequency", Range(0, 100)) = 0
		_XPhase ("XPhase", Range(0, 7)) = 0
		_YPhase ("YPhase", Range(0, 7)) = 0
	}

	SubShader {
		Tags { "RenderType" = "Opaque" }
		LOD 100
		Lighting Off

		Pass {
			CGPROGRAM

	#pragma vertex vert
	#pragma fragment frag
	#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			fixed4 _Color;
			float _XAmp;
			float _YAmp;
			float _XFreq;
			float _YFreq;
			float _XPhase;
			float _YPhase;

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				half2 modifiedTexcoord = i.texcoord;
				modifiedTexcoord.x += sin(_XPhase + i.texcoord.y * _XFreq) * _XAmp;
				modifiedTexcoord.y += sin(_YPhase + i.texcoord.x * _YFreq) * _YAmp;
				fixed4 tex1Col = tex2D(_MainTex, modifiedTexcoord);

				fixed4 c;
				c.rgba = tex1Col * _Color;

				return c;
			}
			ENDCG
		}
	}
}
