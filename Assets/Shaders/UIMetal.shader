// Metal para la UI (aumentos de plata y de oro), hermano de UIPrismatico.
// Como los marcos de plata y oro de TFT: metal pulido con bandas de luz anchas que se mueven
// despacio en diagonal y un destello blanco que cruza de vez en cuando.
// Para que siga viéndose pixel art, todo va sobre la rejilla de píxeles del canvas y el color
// sale de una rampa de 4 tonos fijos de la paleta (más el destello), sin degradados suaves.
// Los píxeles oscuros del sprite (contorno de Tinta) se conservan; los claros toman el metal,
// y lo más claro del sprite queda un paso más arriba en la rampa (el bisel del marco se ve).
Shader "RiftDeck/UIMetal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tinte", Color) = (1,1,1,1)
        _Sombra ("Sombra", Color) = (0.40, 0.39, 0.53, 1)
        _Medio ("Medio", Color) = (0.55, 0.53, 0.72, 1)
        _Claro ("Claro", Color) = (0.69, 0.65, 0.82, 1)
        _Luz ("Luz", Color) = (0.83, 0.76, 0.91, 1)
        _Destello ("Destello", Color) = (1, 0.97, 0.94, 1)
        _Velocidad ("Velocidad de las bandas", Float) = 0.12
        _Escala ("Ancho de las bandas (px)", Float) = 90
        _Brillo ("Destello que cruza", Range(0,1)) = 1
        _Umbral ("Luminancia mínima que se tiñe", Range(0,1)) = 0.3

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
            fixed4 _Color, _Sombra, _Medio, _Claro, _Luz, _Destello;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _Velocidad, _Escala, _Brillo, _Umbral;

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

            float3 rampa(int i)
            {
                if (i <= 0) return _Sombra.rgb;
                if (i == 1) return _Medio.rgb;
                if (i == 2) return _Claro.rgb;
                return _Luz.rgb;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // posición en píxeles del canvas: el efecto avanza de a un píxel de arte
                float2 p = floor(i.worldPosition.xy);
                float t = _Time.y;
                fixed4 c = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;
                float lum = dot(c.rgb, float3(0.299, 0.587, 0.114));

                // bandas anchas de luz en diagonal (metal pulido) que se corren despacio
                float w = (p.x * 0.7 + p.y) / _Escala - t * _Velocidad;
                float onda = 0.5 + 0.5 * sin(w * 6.2832);
                int nivel = (int)floor(onda * 2.99) + (lum > 0.9 ? 1 : 0);
                float3 metal = rampa(nivel);

                // destello blanco que cruza cada ~4 s, con un píxel de Luz a cada lado
                float banda = abs(frac((p.x - p.y) / 200 - t * 0.25) - 0.5);
                metal = lerp(metal, _Luz.rgb, step(banda, 0.03) * _Brillo);
                metal = lerp(metal, _Destello.rgb, step(banda, 0.015) * _Brillo);

                c.rgb = lerp(c.rgb, metal, step(_Umbral, lum));

                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a - 0.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
