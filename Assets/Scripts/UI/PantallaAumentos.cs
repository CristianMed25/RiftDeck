using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Elección de aumento: tapa la partida con tres tarjetas del mismo nivel (plata, oro
    /// o prismático). Las tarjetas caen una tras otra; al tocar una, las otras se apagan,
    /// la elegida late y sale un destello.
    /// Los dos jugadores reciben las mismas tres opciones (eso lo decide quien llama).
    /// </summary>
    public class PantallaAumentos : MonoBehaviour
    {
        [Serializable]
        public class Opcion
        {
            public UnityEngine.UI.Button boton;
            public CanvasGroup grupo;
            public UnityEngine.UI.Image marco;
            public UnityEngine.UI.Image icono;
            public UnityEngine.UI.Image fondoIcono;
            public UnityEngine.UI.Image aura;
            public TMP_Text calidad;
            public TMP_Text nombre;
            public TMP_Text descripcion;
            [NonSerialized] public Vector2 posicion;
            [NonSerialized] public Vector2 posicionIcono;
            [NonSerialized] public Vector2 posicionDescripcion;
            [NonSerialized] public InclinacionAumento inclinacion;
            [NonSerialized] public Material relieve; // copia propia: cada tarjeta se inclina por su lado
        }

        [SerializeField] CanvasGroup grupo;
        /// <summary>Alto de cada línea del nombre (8 px de Press Start 2P + 2 de interlineado).</summary>
        const float AltoLineaNombre = 10;

        [SerializeField] TMP_Text titulo;
        [SerializeField] TMP_Text subtitulo;
        [SerializeField] Opcion[] opciones = new Opcion[3];
        [SerializeField] Material prismatico;
        [SerializeField] Material prismaticoRelieve;
        [Tooltip("Metal de los marcos de plata y de oro (shader UIMetal)")]
        [SerializeField] Material plata;
        [SerializeField] Material oro;
        [Tooltip("La fuente del título de calidad con el arcoíris (shader TMPPrismatico)")]
        [SerializeField] Material textoPrismatico;
        [SerializeField] UnityEngine.UI.Image destello;
        [SerializeField] Sprite[] cuadrosDestello;

        DatosAumento elegido;

        void Update()
        {
            // el aura late en 4 niveles (sin degradado continuo, como el resto del pixel art)
            float nivel = Mathf.Floor((Mathf.Sin(Time.unscaledTime * 4f) * 0.5f + 0.5f) * 4f) / 4f;
            foreach (var o in opciones)
            {
                if (o.aura != null && o.aura.enabled) o.aura.color = new Color(1, 1, 1, 0.55f + 0.45f * nivel);
                Inclinar(o);
            }
        }

        // el dedo inclina la tarjeta: el iridiscente sigue la inclinación y las líneas del ícono
        // se corren 1 px hacia ese lado, sobre el relleno y el aura quietos, para dar profundidad
        static void Inclinar(Opcion o)
        {
            var d = o.inclinacion.Direccion;
            if (o.relieve != null) o.relieve.SetVector("_Mirada", new Vector4(d.x, d.y, 0, o.inclinacion.Peso));
            var paso = new Vector2(Mathf.Round(d.x * 1.4f), Mathf.Round(d.y * 1.4f));
            o.icono.rectTransform.anchoredPosition = o.posicionIcono + paso;
        }

        void OnDestroy()
        {
            foreach (var o in opciones) if (o.relieve != null) Destroy(o.relieve);
        }

        void Awake()
        {
            foreach (var o in opciones)
            {
                o.posicion = ((RectTransform)o.boton.transform).anchoredPosition;
                o.posicionIcono = o.icono.rectTransform.anchoredPosition;
                o.posicionDescripcion = o.descripcion.rectTransform.anchoredPosition;
                o.inclinacion = o.boton.GetComponent<InclinacionAumento>();
                if (o.inclinacion == null) o.inclinacion = o.boton.gameObject.AddComponent<InclinacionAumento>();
            }
            for (int i = 0; i < opciones.Length; i++)
            {
                int indice = i;
                opciones[i].boton.onClick.AddListener(() => Tocar(indice));
            }
            grupo.alpha = 0;
            grupo.blocksRaycasts = false;
            if (destello != null) destello.enabled = false;
        }

        IReadOnlyList<DatosAumento> mostradas;

        void Tocar(int indice)
        {
            if (elegido == null && mostradas != null) elegido = mostradas[indice];
        }

        /// <summary>Muestra la oferta y espera a que el jugador elija.</summary>
        public IEnumerator Elegir(int jugador, IReadOnlyList<DatosAumento> ofertas, int numero, int total,
            Action<DatosAumento> alElegir)
        {
            elegido = null;
            mostradas = ofertas;
            titulo.text = "ELIGE UN AUMENTO";
            subtitulo.text = $"Jugador {jugador} · aumento {numero}/{total}";
            subtitulo.color = PaletaUI.DeJugador(jugador);
            for (int i = 0; i < opciones.Length; i++) Pintar(opciones[i], ofertas[i]);

            grupo.blocksRaycasts = true;
            foreach (var o in opciones) { o.grupo.alpha = 0; o.boton.interactable = false; }
            for (float t = 0; t < 0.2f; t += Time.deltaTime) { grupo.alpha = t / 0.2f; yield return null; }
            grupo.alpha = 1;

            // caen una tras otra
            const float retraso = 0.12f, duracion = 0.3f;
            for (float t = 0; t < retraso * (opciones.Length - 1) + duracion; t += Time.deltaTime)
            {
                for (int i = 0; i < opciones.Length; i++)
                {
                    float k = Mathf.Clamp01((t - retraso * i) / duracion);
                    float caida = (1 - k) * (1 - k);
                    Colocar(opciones[i], new Vector2(0, Mathf.Round(30 * caida)));
                    opciones[i].grupo.alpha = k;
                }
                yield return null;
            }
            foreach (var o in opciones) { Colocar(o, Vector2.zero); o.grupo.alpha = 1; o.boton.interactable = true; }

            while (elegido == null) yield return null;

            int ganadora = 0;
            for (int i = 0; i < ofertas.Count; i++) if (ofertas[i] == elegido) ganadora = i;
            foreach (var o in opciones) o.boton.interactable = false;
            var rt = (RectTransform)opciones[ganadora].boton.transform;
            if (destello != null) StartCoroutine(Destello(rt));
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                float k = t / 0.5f;
                for (int i = 0; i < opciones.Length; i++)
                    if (i != ganadora) opciones[i].grupo.alpha = 1f - k;
                // late una vez: 1 → 1.08 → 1 (en pasos de 1/24 para no perder nitidez)
                float escala = 1f + Mathf.Round(Mathf.Sin(k * Mathf.PI) * 2f) / 24f;
                rt.localScale = new Vector3(escala, escala, 1);
                yield return null;
            }
            rt.localScale = Vector3.one;
            yield return new WaitForSeconds(0.35f);

            for (float t = 0; t < 0.2f; t += Time.deltaTime) { grupo.alpha = 1f - t / 0.2f; yield return null; }
            grupo.alpha = 0;
            grupo.blocksRaycasts = false;
            mostradas = null;
            alElegir?.Invoke(elegido);
        }

        void Pintar(Opcion o, DatosAumento a)
        {
            bool prisma = a.calidad == CalidadAumento.Prismatico;
            // el marco lo pinta su material; sin material, se tiñe del color de la calidad
            var metal = a.calidad switch
            {
                CalidadAumento.Plata => plata,
                CalidadAumento.Oro => oro,
                _ => prismatico,
            };
            o.marco.material = metal;
            o.marco.color = metal != null ? Color.white : DatosAumento.ColorDe(a.calidad);
            o.calidad.text = DatosAumento.NombreDe(a.calidad).ToUpperInvariant();
            o.calidad.color = prisma ? PaletaUI.Blanco : DatosAumento.ColorDe(a.calidad);
            // el título del prismático lleva el arcoíris; los demás, la fuente tal cual
            o.calidad.fontSharedMaterial = prisma && textoPrismatico != null ? textoPrismatico : o.calidad.font.material;
            o.nombre.text = a.nombre;
            o.descripcion.text = a.descripcion;
            // un nombre largo baja en dos líneas y empuja la descripción
            o.nombre.ForceMeshUpdate();
            int lineasExtra = Mathf.Max(0, o.nombre.textInfo.lineCount - 1);
            o.descripcion.rectTransform.anchoredPosition = o.posicionDescripcion + Vector2.down * (lineasExtra * AltoLineaNombre);
            o.icono.enabled = a.icono != null;
            int escala = 1;
            if (a.icono != null)
            {
                o.icono.sprite = a.icono;
                // los íconos chicos se agrandan en múltiplos enteros, hasta unos 48 px
                var tam = a.icono.rect.size;
                escala = Mathf.Max(1, Mathf.RoundToInt(48f / Mathf.Max(tam.x, tam.y)));
                o.icono.rectTransform.sizeDelta = tam * escala;
            }
            if (a.relieve != null && prismaticoRelieve != null)
            {
                if (o.relieve == null) o.relieve = new Material(prismaticoRelieve);
                Capa(o.fondoIcono, a.relieve, escala, o.relieve);
            }
            else
                Capa(o.fondoIcono, a.silueta, escala, prismatico);
            Capa(o.aura, a.aura, escala, null);
        }

        static void Capa(UnityEngine.UI.Image imagen, Sprite sprite, int escala, Material material)
        {
            if (imagen == null) return;
            imagen.enabled = sprite != null;
            if (sprite == null) return;
            imagen.sprite = sprite;
            imagen.material = material;
            imagen.rectTransform.sizeDelta = sprite.rect.size * escala;
        }

        static void Colocar(Opcion o, Vector2 desplazamiento) =>
            ((RectTransform)o.boton.transform).anchoredPosition = o.posicion + desplazamiento;

        IEnumerator Destello(RectTransform sobre)
        {
            destello.rectTransform.position = sobre.position;
            destello.enabled = true;
            foreach (var cuadro in cuadrosDestello)
            {
                destello.sprite = cuadro;
                destello.SetNativeSize();
                yield return new WaitForSeconds(1f / 20f);
            }
            destello.enabled = false;
        }
    }
}
