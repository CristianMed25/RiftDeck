using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace RiftDeck.UI
{
    /// <summary>
    /// Cómo se maneja la mano, al estilo de los roguelikes de cartas:
    /// - Pasar el dedo (o el mouse) por la mano levanta la carta de debajo y muestra
    ///   su detalle. Mantener una carta presionada también la muestra.
    /// - Arrastrar una carta hacia arriba (fuera de la mano) la pone en la zona de jugada.
    /// - Un toque rápido también la pone en la zona; tocarla allí la devuelve a la mano.
    /// La regla de si alcanza el maná no vive aquí: se pide con <see cref="PidioElegir"/>
    /// y quien la acepte llama a <see cref="MoverAZona"/>.
    /// </summary>
    public class ManoCartas : MonoBehaviour
    {
        [SerializeField] RectTransform mano;
        [SerializeField] RectTransform zona;
        [SerializeField] UnityEngine.UI.Image marcoZona;
        [SerializeField] DetalleCarta detalle;
        [Header("Reparto (animación del Deckbuilder Kit)")]
        [Tooltip("De dónde salen las cartas al repartir (el mazo)")]
        [SerializeField] RectTransform origenReparto;
        [Tooltip("Dorsos de la Tienda; sale el equipado o, si no hay, el primero")]
        [SerializeField] DatosCosmetico[] dorsos;
        [SerializeField] float pausaReparto = 0.07f;

        public event Action<CardView> PidioElegir;
        public event Action<CardView> PidioDevolver;
        public Func<CardView, string> Descripcion { get; set; }
        public TiposDano Tipos { get; set; }
        public bool Activa { get; set; } = true;

        CardView presionada, inspeccionada, arrastrada;
        Vector2 inicio;
        bool arrastrando;
        Color colorMarco;
        Canvas canvas;
        UnityEngine.UI.HorizontalLayoutGroup layoutMano, layoutZona;
        float espaciadoMano, espaciadoZona;

        void Awake()
        {
            var canvasLocal = GetComponentInParent<Canvas>();
            if (canvasLocal == null)
            {
                Debug.LogError("ManoCartas: no existe un Canvas padre; no se pueden recibir eventos UI.", this);
                return;
            }

            canvas = canvasLocal.rootCanvas;
            if (canvas.transform.localScale == Vector3.zero)
            {
                Debug.LogWarning(
                    "ManoCartas: el Canvas raíz inició con escala cero; se restablece a escala uno.",
                    canvas);
                canvas.transform.localScale = Vector3.one;
            }

            var fondoMano = GetComponent<UnityEngine.UI.Graphic>();
            if (fondoMano != null)
                fondoMano.raycastTarget = false;

            if (marcoZona != null) colorMarco = marcoZona.color;
            layoutMano = mano.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layoutZona = zona.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (layoutMano != null) espaciadoMano = layoutMano.spacing;
            if (layoutZona != null) espaciadoZona = layoutZona.spacing;
        }

        static void Acomodar(RectTransform contenedor, UnityEngine.UI.HorizontalLayoutGroup layout, float espaciado)
        {
            if (layout == null) return;
            int n = 0;
            float anchoCartas = 0;
            foreach (Transform hijo in contenedor)
            {
                if (!hijo.TryGetComponent<CardView>(out var c)) continue;
                n++;
                anchoCartas += c.Raiz.rect.width;
            }
            float cabe = n < 2 ? espaciado
                : Mathf.Floor((contenedor.rect.width - layout.padding.horizontal - anchoCartas) / (n - 1));
            float nuevo = Mathf.Min(espaciado, cabe);
            if (layout.spacing != nuevo) layout.spacing = nuevo;
        }

        public bool EnZona(CardView carta) => carta.transform.parent == zona;

        public List<CardView> CartasEnZona() => Cartas(zona);
        public List<CardView> CartasEnMano() => Cartas(mano);

        public void SolicitarElegir(CardView carta)
        {
            if (Activa && carta != null && !EnZona(carta))
                PidioElegir?.Invoke(carta);
        }

        static List<CardView> Cartas(RectTransform contenedor)
        {
            var lista = new List<CardView>();
            foreach (Transform hijo in contenedor)
                if (hijo.TryGetComponent<CardView>(out var c)) lista.Add(c);
            return lista;
        }

        readonly List<CardView> porRepartir = new List<CardView>();

        public void Agregar(CardView carta)
        {
            carta.Mano = this;
            carta.transform.SetParent(mano, false);
            carta.Esconder();
            porRepartir.Add(carta);
        }

        void LateUpdate()
        {
            if (porRepartir.Count == 0) return;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(mano);
            var desde = origenReparto != null ? origenReparto.position : mano.position;
            var reverso = Dorso();
            int n = 0;
            foreach (var c in porRepartir)
                if (c != null && c.transform.parent == mano) c.Repartir(desde, n++ * pausaReparto, reverso);
            porRepartir.Clear();
        }

        Sprite Dorso()
        {
            if (dorsos == null || dorsos.Length == 0) return null;
            var id = Inventario.Equipado(CategoriaCosmetico.Dorso);
            foreach (var d in dorsos) if (d != null && d.id == id) return d.vista;
            return dorsos[0] != null ? dorsos[0].vista : null;
        }

        public void MoverAZona(CardView carta)
        {
            carta.IndiceMano = carta.transform.GetSiblingIndex();
            Mover(carta, zona, zona.childCount);
            carta.SetEnZona(true);
            var lienzo = (RectTransform)canvas.transform;
            carta.Presentar(lienzo.TransformPoint(lienzo.rect.center));
        }

        public void MoverAMano(CardView carta)
        {
            Mover(carta, mano, Mathf.Min(carta.IndiceMano, mano.childCount));
            carta.SetEnZona(false);
        }

        void Mover(CardView carta, RectTransform destino, int indice)
        {
            var antes = carta.Visual.position;
            var origen = (RectTransform)carta.transform.parent;
            carta.transform.SetParent(destino, false);
            carta.transform.SetSiblingIndex(indice);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(origen);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(destino);
            carta.VolarDesde(antes);
        }

        // ---------- Toques (los manda cada CardView) ----------

        public void Entrar(CardView carta, PointerEventData e)
        {
            if (Activa && !arrastrando && EsMouse(e)) Inspeccionar(carta);
        }

        public void Salir(CardView carta, PointerEventData e)
        {
            if (!arrastrando && inspeccionada == carta) Inspeccionar(null);
        }

        public void Presionar(CardView carta, PointerEventData e)
        {
            if (!Activa || carta == null) return;
            presionada = carta;
            inicio = e.position;
            arrastrando = false;
        }

        void Update()
        {
            Acomodar(mano, layoutMano, espaciadoMano);
            Acomodar(zona, layoutZona, espaciadoZona);
        }

        public void Arrastrar(CardView carta, PointerEventData e)
        {
            if (!Activa || presionada != carta) return;
            if (!arrastrando && (e.position - inicio).magnitude >= 8f)
            {
                arrastrando = true;
                arrastrada = carta;
                carta.EmpezarArrastre(e);
            }

            if (!arrastrando) return;
            carta.SeguirPuntero(e);
            if (marcoZona != null)
                marcoZona.color = EnZona(e.position) ? PaletaUI.Oro : colorMarco;
        }

        public void Soltar(CardView carta, PointerEventData e)
        {
            if (!Activa || presionada != carta)
            {
                Cancelar();
                return;
            }

            var seleccionada = presionada;
            bool estabaEnZona = EnZona(seleccionada);
            bool quedaEnZona = EnZona(e.position);
            if (arrastrando) seleccionada.TerminarArrastre();
            if (arrastrando && marcoZona != null) marcoZona.color = colorMarco;

            if (estabaEnZona != quedaEnZona)
            {
                if (quedaEnZona) PidioElegir?.Invoke(seleccionada);
                else PidioDevolver?.Invoke(seleccionada);
            }
            else if (!arrastrando)
            {
                if (estabaEnZona) PidioDevolver?.Invoke(seleccionada);
                else PidioElegir?.Invoke(seleccionada);
            }

            Inspeccionar(null);
            presionada = arrastrada = null;
            arrastrando = false;
        }

        public void Cancelar()
        {
            if (arrastrada != null) arrastrada.TerminarArrastre();
            Inspeccionar(null);
            presionada = arrastrada = null;
            arrastrando = false;
            if (marcoZona != null) marcoZona.color = colorMarco;
        }

        void Inspeccionar(CardView carta)
        {
            if (inspeccionada == carta) return;
            if (inspeccionada != null) inspeccionada.SetInspeccion(false);
            inspeccionada = carta;
            if (carta != null)
            {
                carta.SetInspeccion(true);
                if (detalle != null) detalle.Mostrar(carta, Tipos, Descripcion != null ? Descripcion(carta) : "");
            }
            else if (detalle != null) detalle.Ocultar();
        }


        bool EnZona(Vector2 pantalla)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(zona, pantalla, null);
        }

        static bool EsMouse(PointerEventData e) =>
            e is ExtendedPointerEventData x && x.pointerType == UIPointerType.MouseOrPen ||
            !(e is ExtendedPointerEventData) && e.pointerId < 0;
    }
}