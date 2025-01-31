// Simplified Diffuse shader. Differences from regular Diffuse one:
// - no Main Color

Shader "KT/Mobile/DiffuseTintWave" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_Color ("Tint", Color) = (1,1,1,1)
	_XPhase ("XPhase", Range(0, 7)) = 0
	_YPhase ("YPhase", Range(0, 7)) = 0
}
SubShader {
	Tags { "RenderType"="Opaque" }
	LOD 150

CGPROGRAM
// Mobile improvement: noforwardadd
// http://answers.unity3d.com/questions/1200437/how-to-make-a-conditional-pragma-surface-noforward.html
// http://gamedev.stackexchange.com/questions/123669/unity-surface-shader-conditinally-noforwardadd
#pragma surface surf Lambert

sampler2D _MainTex;
float _XPhase;
float _YPhase;
fixed4 _Color;

struct Input {
	float2 uv_MainTex;
};

void surf (Input IN, inout SurfaceOutput o) {
	IN.uv_MainTex.y += sin(_YPhase + IN.uv_MainTex.x * 3) * .1;
	IN.uv_MainTex.x += sin(_XPhase + IN.uv_MainTex.y * 3) * .05;

	fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
	o.Albedo = c.rgb;
	o.Alpha = c.a;
}
ENDCG
}

Fallback "Mobile/VertexLit"
}
