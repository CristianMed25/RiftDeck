using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using RiftDeck.Combate;

namespace RiftDeck.UI
{
    /// <summary>
    /// Carta: costo arriba a la izquierda, daño arriba a la derecha, ícono del tipo al
    /// centro y franja de color abajo. La raíz es un hueco fijo para el Layout Group; lo
    /// que se mueve (inspeccionar, arrastrar, volar a la zona de jugada) es el hijo
    /// "Visual", que lleva su propio Canvas para dibujarse encima de las demás.
    /// Los toques se los pasa a <see cref="ManoCartas"/>, que decide qué hacer.
    /// El costo se dibuja con sprites de cifras (<see cref="NumeroPixel"/>), no pintado en el
    /// arte: si un estado lo cambia (Penitencia: +1), se ve al momento en rojo.
    /// Animaciones tomadas del Deckbuilder Kit: al repartir sale boca abajo del mazo, vuela a
    /// su hueco y se voltea; al ponerla en la zona viene hacia la cámara y baja a su lugar.
    /// </summary>
    public class CardView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        const int EscalaIcono = 2;
        const float Suavizado = 22f;
        // 4/3 da 4 px de pantalla por píxel de arte a ×3: la carta grande sigue nítida
        const float EscalaInspeccion = 4f / 3f;
        const float SubidaInspeccion = 30f;
        const int OrdenInspeccion = 5;
        const int OrdenArrastre = 10;
        const int OrdenPresentar = 12;
        const float EscalaReparto = 0.5f;
        const float DuracionVuelta = 0.25f;
        // 2 = 6 px de pantalla por píxel de arte: grande y nítida frente a la cámara
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
        /// <summary>Posición en la mano, para volver al mismo lugar desde la zona de jugada.</summary>
        public int IndiceMano { get; set; }

        readonly List<DatosEstado> estados = new List<DatosEstado>();
        /// <summary>Buffs y debuffs puestos en esta carta (no en las demás de la mano).</summary>
        public IReadOnlyList<DatosEstado> Estados => estados;

        /// <summary>El costo con los estados de la carta: es el que se paga.</summary>
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
        // ancho relativo mientras se voltea (1 = de frente)
        float vuelta = 1f;
        bool inspeccionada, arrastrando, presentando;
        bool cartaGenerada;
        Vector2 posicionCosto;
        Color colorGema;
        bool costoEnAnillo;

        void Awake()
        {
            var rt = (RectTransform)costo.transform.parent;
            posicionCosto = rt.anchoredPosition;
            if (rt.TryGetComponent<UnityEngine.UI.Image>(out var gema)) colorGema = gema.color;
        }

        /// <param name="arteCompleto">Arte que tapa la cara (arcanos y cartas especiales).</param>
        /// <param name="marcoEspecial">Marco debajo del arte, opcional, con su material.</param>
        /// <param name="conCosto">Deja a la vista el costo sobre el arte.</param>
        /// <param name="conDano">Deja a la vista el daño sobre el arte.</param>
        public void Bind(DatosCarta datos, TiposDano tipos, Sprite arteCompleto = null, Sprite marcoEspecial = null,
            Material materialMarco = null, bool conCosto = false, bool conDano = false)
        {
            Datos = datos;
            LiberarCartaGenerada();
            Carta = CrearCartaDeUI(datos);
            cartaGenerada = true;
            MostrarArte(arteCompleto, conCosto, conDano);
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

        /// <summary>Asocia el dato de combate conservando el formato visual existente.</summary>
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
            return carta;
        }

        static TipoDano ConvertirTipoDano(string tipo)
        {
            return System.Enum.TryParse(tipo, true, out TipoDano resultado)
                ? resultado
                : TipoDano.Elemental;
        }

        /// <summary>
        /// Con arte (un arcano o una carta especial), la carta es esa imagen: se apagan el
        /// ícono, la franja y, si no se piden, el costo y el daño.
        /// </summary>
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
            // marco y arte van al fondo: el costo, el daño y el borde de la zona quedan encima
            arte.transform.SetSiblingIndex(0);
            if (marco != null) marco.transform.SetSiblingIndex(0);
            arte.gameObject.SetActive(hay);
            if (hay)
            {
                arte.sprite = sprite;
                // no SetNativeSize: el Canvas propio de la carta tiene otro Reference PPU
                arte.rectTransform.sizeDelta = sprite.rect.size;
            }
        }

        /// <summary>
        /// Normal: la gema azul arriba a la izquierda con cifras grandes. Hechizos: sin gema,
        /// cifras chicas dentro del anillo que trae el marco de Spire (centro en el píxel 7,7).
        /// </summary>
        void ColocarCosto(bool enAnillo)
        {
            costoEnAnillo = enAnillo;
            var rt = (RectTransform)costo.transform.parent;
            if (rt.TryGetComponent<UnityEngine.UI.Image>(out var gema)) gema.enabled = !enAnillo;
            rt.anchoredPosition = enAnillo ? new Vector2(-1, 0) : posicionCosto;
            // las cifras reemplazan al texto
            costo.gameObject.SetActive(cifrasCosto == null);
            if (enAnillo) rt.gameObject.SetActive(true);
        }

        /// <summary>Blanco si es el de la carta, rojo si subió y menta si bajó.</summary>
        void MostrarCosto()
        {
            int actual = Costo;
            var color = actual > Datos.costo ? PaletaUI.Rojo : actual < Datos.costo ? PaletaUI.Menta : Color.white;
            if (cifrasCosto != null) cifrasCosto.Mostrar(actual, color, costoEnAnillo ? cifrasChicas : cifrasGrandes);
            // el rojo y el menta no se leen sobre el azul: la gema se oscurece si el costo cambió
            if (costo.transform.parent.TryGetComponent<UnityEngine.UI.Image>(out var gema))
                gema.color = actual != Datos.costo ? PaletaUI.Noche : colorGema;
            costo.text = actual.ToString();
            costo.color = color;
        }

        /// <summary>
        /// Un estado le cae a esta carta: el efecto se reproduce encima, a medio camino cambia
        /// el costo y la cifra salta.
        /// </summary>
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
                // destello del color del estado sobre la cara
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

        /// <summary>Pone el estado sin animación (la carta vuelve a la mano ya marcada).</summary>
        public void PonerEstado(DatosEstado estado)
        {
            if (estado == null || estados.Contains(estado)) return;
            estados.Add(estado);
            MostrarCosto();
        }

        /// <summary>Se le quitan los estados (el enemigo que los puso murió).</summary>
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

        /// <summary>En la zona de jugada lleva el borde Oro.</summary>
        public void SetEnZona(bool enZona) => brillo.enabled = enZona;

        /// <summary>Apaga la carta cuando no alcanza el maná.</summary>
        public void SetJugable(bool jugable) => grupo.alpha = jugable ? 1f : 0.55f;

        /// <summary>La carta sube, crece y se dibuja encima para leerla.</summary>
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

        /// <summary>Al soltar vuelve sola a su hueco (o al nuevo, si cambió de lugar).</summary>
        public void TerminarArrastre()
        {
            arrastrando = false;
            SetInspeccion(false);
        }

        /// <summary>
        /// Después de cambiar de contenedor: arranca desde donde se veía antes y vuela a
        /// su hueco nuevo.
        /// </summary>
        public void VolarDesde(Vector3 posicionMundo)
        {
            desplazamiento = Raiz.InverseTransformPoint(posicionMundo);
            desplazamientoObjetivo = Vector2.zero;
            Capa(OrdenArrastre);
        }

        /// <summary>Escondida hasta que la mano la reparta (si no, se ve un cuadro en su hueco).</summary>
        public void Esconder()
        {
            escala = escalaObjetivo = 0;
            visual.localScale = Vector3.zero;
        }

        /// <summary>
        /// Sale boca abajo y chica desde <paramref name="desde"/> (el mazo), vuela a su hueco y
        /// se voltea al llegar. Las de una misma mano salen una tras otra.
        /// </summary>
        public void Repartir(Vector3 desde, float retraso, Sprite reverso)
        {
            StopCoroutine(nameof(Reparto));
            StartCoroutine(Reparto(desde, retraso, reverso));
        }

        IEnumerator Reparto(Vector3 desde, float retraso, Sprite reverso)
        {
            // escondida hasta su turno
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
            // se voltea: se angosta mostrando el dorso y se abre mostrando la cara
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

        /// <summary>
        /// Al ponerla en la zona: viene hacia la cámara (al centro de la pantalla y al doble),
        /// se queda un instante y baja a su lugar en la zona.
        /// </summary>
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
            if (!presentando) yield break; // la agarraron mientras tanto
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

            // se dibuja en píxel entero para que no se vea borroso
            visual.anchoredPosition = new Vector2(Mathf.Round(desplazamiento.x), Mathf.Round(desplazamiento.y));
            visual.localScale = new Vector3(escala * vuelta, escala, 1f);

            // terminó de volar: deja de dibujarse encima
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
    }
}
