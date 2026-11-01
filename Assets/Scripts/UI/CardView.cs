using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        const int EscalaIcono = 2;
        const float Suavizado = 22f;
        // 4/3 da 4 px de pantalla por píxel de arte a ×3: la carta grande sigue nítida
        const float EscalaInspeccion = 4f / 3f;
        const float SubidaInspeccion = 30f;
        const int OrdenInspeccion = 5;
        const int OrdenArrastre = 10;

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

        public DatosCarta Datos { get; private set; }
        public ManoCartas Mano { get; set; }
        public RectTransform Visual => visual;
        public RectTransform Raiz => (RectTransform)transform;
        /// <summary>Posición en la mano, para volver al mismo lugar desde la zona de jugada.</summary>
        public int IndiceMano { get; set; }

        Vector2 desplazamiento, desplazamientoObjetivo, agarre;
        float escala = 1f, escalaObjetivo = 1f;
        bool inspeccionada, arrastrando;

        public void Bind(DatosCarta datos, TiposDano tipos, Sprite arteCompleto = null)
        {
            Datos = datos;
            MostrarArte(arteCompleto);
            costo.text = datos.costo.ToString();
            dano.text = datos.dano.ToString();

            var tipo = tipos != null ? tipos.Buscar(datos.tipo) : null;
            franja.color = tipo != null ? tipo.color : Color.gray;
            icono.enabled = tipo != null && tipo.icono != null;
            if (icono.enabled)
            {
                icono.sprite = tipo.icono;
                icono.rectTransform.sizeDelta = tipo.icono.rect.size * EscalaIcono;
            }
            // entra desde abajo, como si se repartiera
            desplazamiento = new Vector2(0, -40);
            SetEnZona(false);
            SetJugable(true);
        }

        /// <summary>
        /// Con arte (un arcano), la carta es solo esa imagen: se apagan costo, daño,
        /// ícono y franja.
        /// </summary>
        void MostrarArte(Sprite sprite)
        {
            bool hay = sprite != null && arte != null;
            foreach (Transform hijo in visual)
                if (hijo != brillo.transform && (arte == null || hijo != arte.transform))
                    hijo.gameObject.SetActive(!hay);
            if (visual.TryGetComponent<UnityEngine.UI.Image>(out var cara)) cara.enabled = !hay;
            if (arte == null) return;
            arte.gameObject.SetActive(hay);
            if (hay)
            {
                arte.sprite = sprite;
                // no SetNativeSize: el Canvas propio de la carta tiene otro Reference PPU
                arte.rectTransform.sizeDelta = sprite.rect.size;
            }
        }

        /// <summary>En la zona de jugada lleva el borde Oro.</summary>
        public void SetEnZona(bool enZona) => brillo.enabled = enZona;

        /// <summary>Apaga la carta cuando no alcanza el maná.</summary>
        public void SetJugable(bool jugable) => grupo.alpha = jugable ? 1f : 0.55f;

        /// <summary>La carta sube, crece y se dibuja encima para leerla.</summary>
        public void SetInspeccion(bool activa)
        {
            inspeccionada = activa;
            if (arrastrando) return;
            desplazamientoObjetivo = activa ? new Vector2(0, SubidaInspeccion) : Vector2.zero;
            escalaObjetivo = activa ? EscalaInspeccion : 1f;
            Capa(activa ? OrdenInspeccion : 0);
        }

        public void EmpezarArrastre(PointerEventData e)
        {
            arrastrando = true;
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

        void Update()
        {
            float k = 1f - Mathf.Exp(-Suavizado * Time.deltaTime);
            if (!arrastrando) desplazamiento = Vector2.Lerp(desplazamiento, desplazamientoObjetivo, k);
            escala = Mathf.Lerp(escala, escalaObjetivo, k);
            if ((desplazamiento - desplazamientoObjetivo).sqrMagnitude < 0.25f) desplazamiento = desplazamientoObjetivo;
            if (Mathf.Abs(escala - escalaObjetivo) < 0.01f) escala = escalaObjetivo;

            // se dibuja en píxel entero para que no se vea borroso
            visual.anchoredPosition = new Vector2(Mathf.Round(desplazamiento.x), Mathf.Round(desplazamiento.y));
            visual.localScale = new Vector3(escala, escala, 1f);

            // terminó de volar: deja de dibujarse encima
            if (!arrastrando && !inspeccionada && desplazamiento == Vector2.zero && capa.overrideSorting) Capa(0);
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
