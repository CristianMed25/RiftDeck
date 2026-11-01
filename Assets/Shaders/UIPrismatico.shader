// Brillo prismático para la UI (aumentos y arcanos de calidad prismática).
// Referencia principal: los aumentos prismáticos de TFT, que son iridiscentes "nacarados"
// (rosa, lavanda, celeste, cian, menta y amarillo pálido, como aceite sobre agua), con
// destellos blancos y un aura violeta (el aura va aparte, como sprite).
// Además, del "policromo" de Balatro: el tono se mueve con un ruido animado.
// Para que siga viéndose pixel art, todo se calcula sobre la rejilla de píxeles del canvas
// (640×360) y los colores son 6 tonos fijos, sin degradados suaves.
// El color del sprite se conserva en los píxeles oscuros (contorno de Tinta) y los claros
// toman el arcoíris.
// Modo relieve (_RELIEVE): el sprite es un mapa de normales (tools/aumentos.py → relieve) y
// el tono sale del ángulo entre la normal y la "mirada" (Fresnel), que se mece como si la
// carta se inclinara bajo la luz. Cada facet toma su color y el arcoíris sigue el volumen,
// que es lo que hace que los prismáticos de TFT se vean 3D. Encima va un brillo especular.
Shader "RiftDeck/UIPrismatico"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tinte", Color) = (1,1,1,1)
        _Velocidad ("Velocidad del arcoíris", Float) = 0.25
        _Escala ("Ancho de las bandas (px)", Float) = 60
        _Saturacion ("Intensidad del color", Range(0,1)) = 1
        _Ruido ("Ruido (policromo)", Range(0,1)) = 0.5
        _Brillo ("Franja de brillo", Range(0,1)) = 0.6
        _Umbral ("Luminancia mínima que se tiñe", Range(0,1)) = 0.3
        [Toggle(_RELIEVE)] _Relieve ("Relieve (el sprite es un mapa de normales)", Float) = 0
        _Inclinacion ("Inclinación de la carta", Range(0,1)) = 0.45
        _Bandas ("Vueltas de arcoíris por el volumen", Float) = 1.2
        _Especular ("Brillo especular", Range(0,1)) = 0.9
        // xy = hacia dónde se inclina la carta (-1..1), w = cuánto manda el dedo sobre el vaivén solo
        _Mirada ("Inclinación por el dedo", Vector) = (0,0,0,0)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
            "PreviewType"="Plane" "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma shader_feature_local _ _RELIEVE

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _Velocidad, _Escala, _Saturacion, _Ruido, _Brillo, _Umbral;
            float _Inclinacion, _Bandas, _Especular;
            float4 _Mirada;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            float ruido(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x),
                            lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
            }

            // paleta nacarada de los prismáticos de TFT
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

            fixed4 frag(v2f i) : SV_Target
            {
                // posición en píxeles del canvas: el efecto avanza de a un píxel de arte
                float2 p = floor(i.worldPosition.xy);
                float t = _Time.y;

                #ifdef _RELIEVE
                fixed4 m = tex2D(_MainTex, i.texcoord);
                float3 n = float3(m.rg * 2 - 1, 0);
                n.z = sqrt(saturate(1 - dot(n.xy, n.xy)));

                // la carta se mece sola (la mirada da vueltas lentas alrededor del frente), o la
                // inclina el dedo, estilo Balatro
                float2 vaiven = float2(sin(t * 0.9), cos(t * 0.7));
                float2 incl = lerp(vaiven, _Mirada.xy * 1.6, _Mirada.w);
                float3 v = normalize(float3(incl * _Inclinacion, 1));
                float fres = 1 - saturate(dot(n, v));
                float hr = fres * _Bandas + m.b * 0.35 + (p.x * 0.8 + p.y) / (_Escala * 3) + t * _Velocidad;
                float3 col = lerp(1, tono(hr), _Saturacion * 0.8); // un poco nacarado

                // luz que gira: sombreado en 3 niveles y punto especular en los hombros del relieve
                // con el dedo, la luz queda del lado contrario a la inclinación: el brillo corre al ladear
                float3 L = normalize(float3(lerp(float2(cos(t * 1.1), sin(t * 1.1)), float2(0.5, 0.7) - _Mirada.xy * 1.2, _Mirada.w), 1.4)); // desplazada: de frente no brilla toda la carta
                col *= 0.7 + 0.3 * floor(saturate(dot(n, L)) * 2.99) / 2;
                float spec = pow(saturate(dot(reflect(-L, n), v)), 20);
                float bandaR = frac((p.x - p.y) / 160 - t * 0.35);
                float blanco = max(step(0.8, spec) * _Especular, step(abs(bandaR - 0.5), 0.03) * _Brillo * 0.5);
                col = lerp(col, 1, blanco);
                #ifndef UNITY_COLORSPACE_GAMMA
                col = GammaToLinearSpace(col); // los tonos están pensados en sRGB
                #endif

                fixed4 r = fixed4(col, m.a * i.color.a);
                #ifdef UNITY_UI_CLIP_RECT
                r.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(r.a - 0.001);
                #endif
                return r;
                #else
                fixed4 c = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                float h = (p.x * 0.8 + p.y) / _Escala + t * _Velocidad
                        + _Ruido * ruido(p / 24 + t * 0.3);
                float lum = dot(c.rgb, float3(0.299, 0.587, 0.114));
                float3 arcoiris = lerp(1, tono(h), _Saturacion) * (0.6 + 0.4 * lum);

                // franja diagonal de brillo que cruza cada ~3 s
                float banda = frac((p.x - p.y) / 160 - t * 0.35);
                float brillo = step(abs(banda - 0.5), 0.03) * _Brillo;

                float tenir = step(_Umbral, lum);
                c.rgb = lerp(c.rgb, arcoiris + brillo, tenir);

                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a - 0.001);
                #endif
                return c;
                #endif
            }
            ENDCG
        }
    }
}
