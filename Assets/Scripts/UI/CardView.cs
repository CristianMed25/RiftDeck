using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RiftDeck.Combate;

namespace RiftDeck.UI
{
    /// <summary>
    /// Carta: costo arriba a la izquierda, daño arriba a la derecha, ícono del tipo al
    /// centro y franja de color abajo. La raíz es un hueco fijo para el Layout Group; lo
    /// que se mueve (inspeccionar, arrastrar, volar a la zona de jugada) es el hijo
    /// "Visual", que lleva su propio Canvas para dibujarse encima de las demás.
    /// Los toques se los pasa a <see cref="ManoCartas"/>, que decide qué hacer.
    /// </summary>
    public class CardView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerClickHandler
    {
        const int EscalaIcono = 2;
        const float Suavizado = 22f;
        const float EscalaInspeccion = 4f / 3f;
        const float SubidaInspeccion = 30f;
        const int OrdenInspeccion = 5;
        const int OrdenArrastre = 10;
        const int OrdenPresentar = 12;
        const float EscalaReparto = 0.5f;
        const float DuracionVuelta = 0.25f;
        const float EscalaPresentar = 2f;

        [SerializeField] RectTransform visual;
        [SerializeField] UnityEngine.UI.Image franja;
        [SerializeField] UnityEngine.UI.Image icono;
        [SerializeField] UnityEngine.UI.Image brillo;
        [SerializeField] TMP_Text costo;
        [SerializeField] TMP_Text dano;
        [SerializeField] CanvasGroup grupo;
        [SerializeField] Canvas capa;
        [Tooltip("Arte a tamaño real que tapa la cara (cartas de tarot)")]
        [SerializeField] UnityEngine.UI.Image arte;
        [Tooltip("Marco de las cartas especiales, debajo del arte (puede llevar el material prismático)")]
        [SerializeField] UnityEngine.UI.Image marco;
        [Tooltip("Reverso que tapa la carta mientras se reparte (el dorso equipado en la Tienda)")]
        [SerializeField] UnityEngine.UI.Image dorso;
        [Header("Costo")]
        [SerializeField] NumeroPixel cifrasCosto;
        [Tooltip("Cifras de 9 px para la gema de las cartas normales")]
        [SerializeField] Sprite[] cifrasGrandes;
        [Tooltip("Cifras de 5x7 para el anillo de costo de las cartas de Spire of Ash")]
        [SerializeField] Sprite[] cifrasChicas;

        public DatosCarta Datos { get; private set; }
        public CartaSO Carta { get; private set; }
        public ManoCartas Mano { get; set; }
        public RectTransform Visual => visual;
        public RectTransform Raiz => (RectTransform)transform;
        public int IndiceMano { get; set; }

        readonly List<DatosEstado> estados = new List<DatosEstado>();
        public IReadOnlyList<DatosEstado> Estados => estados;

        public int Costo
        {
            get
            {
                int c = Datos.costo;
                foreach (var e in estados) c += e.costoExtra;
                return Mathf.Max(0, c);
            }
        }

        Vector2 desplazamiento, desplazamientoObjetivo, agarre;
        float escala = 1f, escalaObjetivo = 1f;
        float vuelta = 1f;
        bool inspeccionada, arrastrando, presentando;
        bool cartaGenerada;
        Vector2 posicionCosto;
        Color colorGema;
        bool costoEnAnillo;

        void Awake()
        {
            foreach (var grafico in GetComponentsInChildren<Graphic>(true))
                grafico.raycastTarget = false;

            if (costo == null || dano == null || visual == null || franja == null || icono == null ||
                brillo == null || grupo == null || capa == null)
            {
                Debug.LogError("CardView: el prefab no tiene todos sus referencias visuales asignadas.", this);
                return;
            }
            var rt = (RectTransform)costo.transform.parent;
            if (rt == null)
            {
                Debug.LogError("CardView: el contador de costo debe tener un padre RectTransform.", this);
                return;
            }
            posicionCosto = rt.anchoredPosition;
            if (rt.TryGetComponent<UnityEngine.UI.Image>(out var gema)) colorGema = gema.color;

            if (!TryGetComponent<Graphic>(out var graficoRaiz))
            {
                var hitbox = gameObject.AddComponent<UnityEngine.UI.Image>();
                hitbox.color = new Color(1f, 1f, 1f, 0f);
                graficoRaiz = hitbox;
            }

            graficoRaiz.raycastTarget = true;
        }

        void Start()
        {
            if (Mano == null)
                Debug.LogWarning("CardView: inició sin ManoCartas; la instancia no podrá seleccionar cartas.", this);
        }

        public void Bind(DatosCarta datos, TiposDano tipos, Sprite arteCompleto = null, Sprite marcoEspecial = null,
            Material materialMarco = null, bool conCosto = false, bool conDano = false)
        {
            Datos = datos;
            LiberarCartaGenerada();
            Carta = CrearCartaDeUI(datos);
            cartaGenerada = true;
            Sprite ilustracion = arteCompleto != null ? arteCompleto : datos.spriteIlustracion;
            MostrarArte(ilustracion, conCosto, conDano);
            if (ilustracion == null)
                Debug.LogWarning($"CardView: la carta '{datos.nombre}' no tiene ilustración asignada.", this);
            MostrarMarco(arteCompleto != null ? marcoEspecial : null, materialMarco);
            estados.Clear();
            costo.text = datos.costo.ToString();
            dano.text = datos.dano.ToString();
            ColocarCosto(datos.Es(DatosCarta.Hechizo));
            MostrarCosto();

            var tipo = tipos != null ? tipos.Buscar(datos.tipo) : null;
            franja.color = tipo != null ? tipo.color : Color.gray;
            icono.enabled = tipo != null && tipo.icono != null;
            if (icono.enabled)
            {
                icono.sprite = tipo.icono;
                icono.rectTransform.sizeDelta = tipo.icono.rect.size * EscalaIcono;
            }
            if (dorso != null) dorso.gameObject.SetActive(false);
            SetEnZona(false);
            SetJugable(true);
        }

        public void BindCarta(CartaSO carta, TiposDano tipos)
        {
            if (carta == null) throw new System.ArgumentNullException(nameof(carta));
            Bind(new DatosCarta(carta.nombreCarta, 0, carta.valorBase, carta.tipoDano.ToString()),
                tipos, carta.spriteIlustracion, null, null, false, false);
            LiberarCartaGenerada();
            Carta = carta;
        }

        void OnDestroy() => LiberarCartaGenerada();

        void LiberarCartaGenerada()
        {
            if (cartaGenerada && Carta != null)
                Destroy(Carta);
            cartaGenerada = false;
        }

        CartaSO CrearCartaDeUI(DatosCarta datos)
        {
            var carta = ScriptableObject.CreateInstance<CartaSO>();
            carta.id = datos.nombre;
            carta.nombreCarta = datos.nombre;
            carta.valorBase = datos.dano;
            carta.tipoDano = ConvertirTipoDano(datos.tipo);
            carta.spriteIlustracion = datos.spriteIlustracion;
            return carta;
        }

        static TipoDano ConvertirTipoDano(string tipo)
        {
            return System.Enum.TryParse(tipo, true, out TipoDano resultado)
                ? resultado
                : TipoDano.Elemental;
        }

        void MostrarArte(Sprite sprite, bool conCosto, bool conDano)
        {
            bool hay = sprite != null && arte != null;
            foreach (Transform hijo in visual)
            {
                if (hijo == brillo.transform || (arte != null && hijo == arte.transform) || (marco != null && hijo == marco.transform))
                    continue;
                bool numero = (conCosto && costo.transform.IsChildOf(hijo)) || (conDano && dano.transform.IsChildOf(hijo));
                hijo.gameObject.SetActive(!hay || numero);
            }
            if (visual.TryGetComponent<UnityEngine.UI.Image>(out var cara)) cara.enabled = !hay;
            if (arte == null) return;
            arte.transform.SetSiblingIndex(0);
            if (marco != null) marco.transform.SetSiblingIndex(0);
            arte.gameObject.SetActive(hay);
            if (hay)
            {
                arte.sprite = sprite;
                arte.rectTransform.sizeDelta = sprite.rect.size;
            }
        }

        void ColocarCosto(bool enAnillo)
        {
            costoEnAnillo = enAnillo;
            var rt = (RectTransform)costo.transform.parent;
            if (rt.TryGetComponent<UnityEngine.UI.Image>(out var gema)) gema.enabled = !enAnillo;
            rt.anchoredPosition = enAnillo ? new Vector2(-1, 0) : posicionCosto;
            costo.gameObject.SetActive(cifrasCosto == null);
            if (enAnillo) rt.gameObject.SetActive(true);
        }

        void MostrarCosto()
        {
            int actual = Costo;
            var color = actual > Datos.costo ? PaletaUI.Rojo : actual < Datos.costo ? PaletaUI.Menta : Color.white;
            if (cifrasCosto != null) cifrasCosto.Mostrar(actual, color, costoEnAnillo ? cifrasChicas : cifrasGrandes);
            if (costo.transform.parent.TryGetComponent<UnityEngine.UI.Image>(out var gema))
                gema.color = actual != Datos.costo ? PaletaUI.Noche : colorGema;
            costo.text = actual.ToString();
            costo.color = color;
        }

        public IEnumerator RecibirEstado(DatosEstado estado, Sprite[] cuadros)
        {
            UnityEngine.UI.Image efecto = null;
            if (cuadros != null && cuadros.Length > 0)
            {
                var go = new GameObject("Efecto", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image))
                    { layer = gameObject.layer };
                efecto = go.GetComponent<UnityEngine.UI.Image>();
                efecto.rectTransform.SetParent(visual, false);
                efecto.rectTransform.anchorMin = efecto.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                efecto.rectTransform.sizeDelta = cuadros[0].rect.size;
                efecto.raycastTarget = false;
                efecto.color = estado.Color;
            }
            var cara = visual.GetComponent<UnityEngine.UI.Image>();
            int total = cuadros != null ? cuadros.Length : 6;
            for (int i = 0; i < total; i++)
            {
                if (efecto != null)
                {
                    efecto.sprite = cuadros[i];
                    efecto.transform.SetAsLastSibling();
                }
                if (cara != null && cara.enabled) cara.color = i % 4 < 2 ? Color.Lerp(Color.white, estado.Color, 0.5f) : Color.white;
                if (i == total / 2)
                {
                    estados.Add(estado);
                    MostrarCosto();
                    StartCoroutine(SaltarCosto());
                }
                yield return new WaitForSeconds(1f / 20f);
            }
            if (cara != null) cara.color = Color.white;
            if (efecto != null) Destroy(efecto.gameObject);
        }

        public void PonerEstado(DatosEstado estado)
        {
            if (estado == null || estados.Contains(estado)) return;
            estados.Add(estado);
            MostrarCosto();
        }

        public void QuitarEstados()
        {
            if (estados.Count == 0) return;
            estados.Clear();
            MostrarCosto();
        }

        IEnumerator SaltarCosto()
        {
            var rt = (RectTransform)costo.transform.parent;
            var base_ = rt.anchoredPosition;
            foreach (int dy in new[] { 4, 3, 1, 0 })
            {
                rt.anchoredPosition = base_ + new Vector2(0, dy);
                yield return new WaitForSeconds(0.05f);
            }
        }

        void MostrarMarco(Sprite sprite, Material material)
        {
            if (marco == null) return;
            marco.gameObject.SetActive(sprite != null);
            if (sprite == null) return;
            marco.sprite = sprite;
            marco.material = material;
            marco.rectTransform.sizeDelta = sprite.rect.size;
        }

        public void SetEnZona(bool enZona) => brillo.enabled = enZona;
        public void SetJugable(bool jugable) => grupo.alpha = jugable ? 1f : 0.55f;

        public void SetInspeccion(bool activa)
        {
            inspeccionada = activa;
            if (arrastrando || presentando) return;
            desplazamientoObjetivo = activa ? new Vector2(0, SubidaInspeccion) : Vector2.zero;
            escalaObjetivo = activa ? EscalaInspeccion : 1f;
            Capa(activa ? OrdenInspeccion : 0);
        }

        public void EmpezarArrastre(PointerEventData e)
        {
            arrastrando = true;
            presentando = false;
            escalaObjetivo = 1f;
            Capa(OrdenArrastre);
            agarre = PuntoLocal(e) - desplazamiento;
        }

        public void SeguirPuntero(PointerEventData e)
        {
            desplazamiento = desplazamientoObjetivo = PuntoLocal(e) - agarre;
        }

        public void TerminarArrastre()
        {
            arrastrando = false;
            SetInspeccion(false);
        }

        public void VolarDesde(Vector3 posicionMundo)
        {
            desplazamiento = Raiz.InverseTransformPoint(posicionMundo);
            desplazamientoObjetivo = Vector2.zero;
            Capa(OrdenArrastre);
        }

        public void Esconder()
        {
            escala = escalaObjetivo = 0;
            visual.localScale = Vector3.zero;
        }

        public void Repartir(Vector3 desde, float retraso, Sprite reverso)
        {
            StopCoroutine(nameof(Reparto));
            StartCoroutine(Reparto(desde, retraso, reverso));
        }

        IEnumerator Reparto(Vector3 desde, float retraso, Sprite reverso)
        {
            escala = 0;
            escalaObjetivo = 0;
            if (retraso > 0) yield return new WaitForSeconds(retraso);
            desplazamiento = Raiz.InverseTransformPoint(desde);
            desplazamientoObjetivo = Vector2.zero;
            escala = EscalaReparto;
            escalaObjetivo = 1f;
            Capa(OrdenArrastre);
            bool conDorso = dorso != null && reverso != null;
            if (conDorso)
            {
                dorso.sprite = reverso;
                dorso.rectTransform.sizeDelta = reverso.rect.size;
                dorso.transform.SetAsLastSibling();
                dorso.gameObject.SetActive(true);
            }
            yield return new WaitForSeconds(0.12f);
            for (float t = 0; t < DuracionVuelta; t += Time.deltaTime)
            {
                float k = t / DuracionVuelta;
                vuelta = Mathf.Abs(Mathf.Cos(k * Mathf.PI));
                if (conDorso) dorso.gameObject.SetActive(k < 0.5f);
                yield return null;
            }
            vuelta = 1f;
            if (conDorso) dorso.gameObject.SetActive(false);
        }

        public void Presentar(Vector3 centro)
        {
            StopCoroutine(nameof(Presentacion));
            StartCoroutine(Presentacion(centro));
        }

        IEnumerator Presentacion(Vector3 centro)
        {
            presentando = true;
            Capa(OrdenPresentar);
            desplazamientoObjetivo = Raiz.InverseTransformPoint(centro);
            escalaObjetivo = EscalaPresentar;
            yield return new WaitForSeconds(0.32f);
            if (!presentando) yield break;
            presentando = false;
            desplazamientoObjetivo = inspeccionada ? new Vector2(0, SubidaInspeccion) : Vector2.zero;
            escalaObjetivo = inspeccionada ? EscalaInspeccion : 1f;
        }

        void Update()
        {
            float k = 1f - Mathf.Exp(-Suavizado * Time.deltaTime);
            if (!arrastrando) desplazamiento = Vector2.Lerp(desplazamiento, desplazamientoObjetivo, k);
            escala = Mathf.Lerp(escala, escalaObjetivo, k);
            if ((desplazamiento - desplazamientoObjetivo).sqrMagnitude < 0.25f) desplazamiento = desplazamientoObjetivo;
            if (Mathf.Abs(escala - escalaObjetivo) < 0.01f) escala = escalaObjetivo;

            visual.anchoredPosition = new Vector2(Mathf.Round(desplazamiento.x), Mathf.Round(desplazamiento.y));
            visual.localScale = new Vector3(escala * vuelta, escala, 1f);

            if (!arrastrando && !inspeccionada && !presentando && desplazamiento == Vector2.zero && escala == 1f && capa.overrideSorting) Capa(0);
        }

        void Capa(int orden)
        {
            capa.overrideSorting = orden > 0;
            capa.sortingOrder = orden;
        }

        Vector2 PuntoLocal(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Raiz, e.position, e.pressEventCamera, out var p);
            return p;
        }

        public void OnPointerEnter(PointerEventData e) => Mano?.Entrar(this, e);
        public void OnPointerExit(PointerEventData e) => Mano?.Salir(this, e);
        public void OnPointerDown(PointerEventData e) => Mano?.Presionar(this, e);
        public void OnPointerUp(PointerEventData e) => Mano?.Soltar(this, e);
        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;
        public void OnBeginDrag(PointerEventData e) { }
        public void OnDrag(PointerEventData e) => Mano?.Arrastrar(this, e);
        public void OnEndDrag(PointerEventData e) { }

        // Se limpia OnPointerClick para que no duplique las llamadas que ya procesa ManoCartas.Soltar
        public void OnPointerClick(PointerEventData e) { }
    }
}