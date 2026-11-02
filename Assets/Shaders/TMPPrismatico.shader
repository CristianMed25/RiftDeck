// El arcoíris de UIPrismatico para textos de TextMeshPro con fuente Raster (Bitmap).
// Es el shader TextMeshPro/Mobile/Bitmap con el color cambiado: los mismos 6 tonos nacarados
// de los prismáticos de TFT, en bandas diagonales que avanzan de a un píxel del canvas, con
// ruido de policromo y la franja de brillo. Se usa en el título "PRISMÁTICO" de los aumentos.
Shader "RiftDeck/TMPPrismatico" {

Properties {
	_MainTex		    ("Font Atlas", 2D) = "white" {}
	_Color		        ("Text Color", Color) = (1,1,1,1)
	_DiffusePower	    ("Diffuse Power", Range(1.0,4.0)) = 1.0
	_Velocidad          ("Velocidad del arcoíris", Float) = 0.25
	_Escala             ("Ancho de las bandas (px)", Float) = 40
	_Ruido              ("Ruido (policromo)", Range(0,1)) = 0.5
	_Brillo             ("Franja de brillo", Range(0,1)) = 0.8

	_VertexOffsetX      ("Vertex OffsetX", float) = 0
	_VertexOffsetY      ("Vertex OffsetY", float) = 0
	_MaskSoftnessX      ("Mask SoftnessX", float) = 0
	_MaskSoftnessY      ("Mask SoftnessY", float) = 0

	_ClipRect           ("Clip Rect", vector) = (-32767, -32767, 32767, 32767)

	_StencilComp        ("Stencil Comparison", Float) = 8
	_Stencil            ("Stencil ID", Float) = 0
	_StencilOp          ("Stencil Operation", Float) = 0
	_StencilWriteMask   ("Stencil Write Mask", Float) = 255
	_StencilReadMask    ("Stencil Read Mask", Float) = 255

	_CullMode           ("Cull Mode", Float) = 0
	_ColorMask          ("Color Mask", Float) = 15
}

SubShader {

	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

	Stencil
	{
		Ref[_Stencil]
		Comp[_StencilComp]
		Pass[_StencilOp]
		ReadMask[_StencilReadMask]
		WriteMask[_StencilWriteMask]
	}

	Lighting Off
	Cull [_CullMode]
	ZTest [unity_GUIZTestMode]
	ZWrite Off
	Fog { Mode Off }
	Blend SrcAlpha OneMinusSrcAlpha
	ColorMask[_ColorMask]

	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma fragmentoption ARB_precision_hint_fastest

		#pragma multi_compile __ UNITY_UI_CLIP_RECT
		#pragma multi_compile __ UNITY_UI_ALPHACLIP

		#include "UnityCG.cginc"
		#include "UnityUI.cginc"

		struct appdata_t
		{
			float4 vertex : POSITION;
			fixed4 color : COLOR;
			float2 texcoord0 : TEXCOORD0;
			float2 texcoord1 : TEXCOORD1;
		};

		struct v2f
		{
			float4 vertex		: POSITION;
			fixed4 color		: COLOR;
			float2 texcoord0	: TEXCOORD0;
			float4 mask			: TEXCOORD2;
			float2 pos			: TEXCOORD3;
		};

		sampler2D 	_MainTex;
		fixed4		_Color;
		float		_DiffusePower;
		float		_Velocidad, _Escala, _Ruido, _Brillo;

		uniform float		_VertexOffsetX;
		uniform float		_VertexOffsetY;
		uniform float4		_ClipRect;
		uniform float		_MaskSoftnessX;
		uniform float		_MaskSoftnessY;
		uniform float		_UIMaskSoftnessX;
		uniform float		_UIMaskSoftnessY;

		float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

		float ruido(float2 p)
		{
			float2 i = floor(p), f = frac(p);
			f = f * f * (3 - 2 * f);
			return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x),
			            lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
		}

		// paleta nacarada de los prismáticos de TFT (la misma de UIPrismatico)
		float3 tono(float h)
		{
			int i = (int)floor(frac(h) * 6);
			if (i == 0) return float3(1.00, 0.42, 0.80); // rosa
			if (i == 1) return float3(0.64, 0.42, 1.00); // lavanda
			if (i == 2) return float3(0.42, 0.62, 1.00); // celeste
			if (i == 3) return float3(0.34, 0.90, 1.00); // cian
			if (i == 4) return float3(0.52, 1.00, 0.68); // menta
			return float3(1.00, 0.90, 0.48);             // amarillo pálido
		}

		v2f vert (appdata_t v)
		{
			v2f OUT;
			float4 vert = v.vertex;
			vert.x += _VertexOffsetX;
			vert.y += _VertexOffsetY;
			OUT.pos = vert.xy;

			vert.xy += (vert.w * 0.5) / _ScreenParams.xy;
			OUT.vertex = UnityPixelSnap(UnityObjectToClipPos(vert));
			OUT.color = v.color * _Color;
			OUT.color.rgb *= _DiffusePower;
			OUT.texcoord0 = v.texcoord0;

			float2 pixelSize = OUT.vertex.w;
			const float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
			const half2 maskSoftness = half2(max(_UIMaskSoftnessX, _MaskSoftnessX), max(_UIMaskSoftnessY, _MaskSoftnessY));
			OUT.mask = float4(vert.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * maskSoftness + pixelSize.xy));
			return OUT;
		}

		fixed4 frag (v2f IN) : COLOR
		{
			// posición en píxeles del canvas: el arcoíris avanza de a un píxel de arte
			float2 p = floor(IN.pos);
			float t = _Time.y;
			float h = (p.x * 0.8 + p.y) / _Escala + t * _Velocidad + _Ruido * ruido(p / 24 + t * 0.3);
			float3 col = tono(h);
			float banda = frac((p.x - p.y) / 160 - t * 0.35);
			col = lerp(col, 1, step(abs(banda - 0.5), 0.03) * _Brillo);
			#ifndef UNITY_COLORSPACE_GAMMA
			col = GammaToLinearSpace(col); // los tonos están pensados en sRGB
			#endif

			fixed4 color = fixed4(col * IN.color.rgb, IN.color.a * tex2D(_MainTex, IN.texcoord0).a);

			#if UNITY_UI_CLIP_RECT
				half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
				color *= m.x * m.y;
			#endif

			#if UNITY_UI_ALPHACLIP
				clip(color.a - 0.001);
			#endif

			return color;
		}
		ENDCG
	}
}

CustomEditor "TMPro.EditorUtilities.TMP_BitmapShaderGUI"
}
