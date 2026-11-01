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
        const float UmbralMovimiento = 6f;   // unidades del canvas (640×360)
        const float UmbralArrastre = 14f;
        const float TiempoMantener = 0.25f;

        enum Modo { Nada, Pendiente, Barrido, Arrastre }

        [SerializeField] RectTransform mano;
        [SerializeField] RectTransform zona;
        [SerializeField] UnityEngine.UI.Image marcoZona;
        [SerializeField] DetalleCarta detalle;

        public event Action<CardView> PidioElegir;
        public event Action<CardView> PidioDevolver;
        /// <summary>Texto del detalle de una carta (lo arma quien conoce al enemigo).</summary>
        public Func<CardView, string> Descripcion { get; set; }
        public TiposDano Tipos { get; set; }
        /// <summary>Mientras es falso no se aceptan toques (animaciones, cambio de turno).</summary>
        public bool Activa { get; set; } = true;

        Modo modo;
        CardView presionada, inspeccionada, arrastrada;
        Vector2 inicio;
        float tiempoInicio;
        bool vioDetalle;
        Color colorMarco;
        Canvas canvas;
        UnityEngine.UI.HorizontalLayoutGroup layoutMano, layoutZona;
        float espaciadoMano, espaciadoZona;

        void Awake()
        {
            canvas = GetComponentInParent<Canvas>().rootCanvas;
            if (marcoZona != null) colorMarco = marcoZona.color;
            layoutMano = mano.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layoutZona = zona.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (layoutMano != null) espaciadoMano = layoutMano.spacing;
            if (layoutZona != null) espaciadoZona = layoutZona.spacing;
        }

        /// <summary>
        /// Si las cartas no caben (mano grande, arcanos, pantalla 16:9), se enciman: el
        /// espacio entre ellas baja, incluso a negativo, en unidades enteras.
        /// </summary>
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

        static List<CardView> Cartas(RectTransform contenedor)
        {
            var lista = new List<CardView>();
            foreach (Transform hijo in contenedor)
                if (hijo.TryGetComponent<CardView>(out var c)) lista.Add(c);
            return lista;
        }

        public void Agregar(CardView carta)
        {
            carta.Mano = this;
            carta.transform.SetParent(mano, false);
        }

        public void MoverAZona(CardView carta)
        {
            carta.IndiceMano = carta.transform.GetSiblingIndex();
            Mover(carta, zona, zona.childCount);
            carta.SetEnZona(true);
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
            // solo el mouse "pasa por encima"; en táctil el detalle sale al mantener
            if (Activa && modo == Modo.Nada && EsMouse(e)) Inspeccionar(carta);
        }

        public void Salir(CardView carta, PointerEventData e)
        {
            if (modo == Modo.Nada && inspeccionada == carta) Inspeccionar(null);
        }

        public void Presionar(CardView carta, PointerEventData e)
        {
            if (!Activa || modo != Modo.Nada) return;
            presionada = carta;
            modo = Modo.Pendiente;
            inicio = e.position;
            tiempoInicio = Time.unscaledTime;
            vioDetalle = false;
        }

        void Update()
        {
            Acomodar(mano, layoutMano, espaciadoMano);
            Acomodar(zona, layoutZona, espaciadoZona);
            if (modo == Modo.Pendiente && !vioDetalle && Time.unscaledTime - tiempoInicio >= TiempoMantener)
            {
                Inspeccionar(presionada);
                vioDetalle = true;
            }
        }

        public void Arrastrar(CardView carta, PointerEventData e)
        {
            if (modo == Modo.Nada) return;
            Vector2 delta = (e.position - inicio) / canvas.scaleFactor;

            if (modo == Modo.Pendiente)
            {
                if (EnZona(presionada))
                {
                    if (delta.magnitude > UmbralMovimiento) EmpezarArrastre(presionada, e);
                }
                else if (delta.y > UmbralArrastre && delta.y > Mathf.Abs(delta.x)) EmpezarArrastre(presionada, e);
                else if (delta.magnitude > UmbralMovimiento) modo = Modo.Barrido;
            }

            if (modo == Modo.Barrido)
            {
                var debajo = CartaBajo(e.position.x);
                if (debajo != null) Inspeccionar(debajo);
                // del barrido se pasa a arrastrar la carta que está levantada
                if (ArribaDeLaMano(e) && inspeccionada != null) EmpezarArrastre(inspeccionada, e);
            }

            if (modo == Modo.Arrastre)
            {
                arrastrada.SeguirPuntero(e);
                if (marcoZona != null)
                    marcoZona.color = SeQuedaEnZona(arrastrada, e) ? PaletaUI.Oro : colorMarco;
            }
        }

        public void Soltar(CardView carta, PointerEventData e)
        {
            switch (modo)
            {
                case Modo.Pendiente:
                    Inspeccionar(null);
                    if (!vioDetalle)
                    {
                        if (EnZona(presionada)) PidioDevolver?.Invoke(presionada);
                        else PidioElegir?.Invoke(presionada);
                    }
                    break;
                case Modo.Barrido:
                    Inspeccionar(null);
                    break;
                case Modo.Arrastre:
                    var c = arrastrada;
                    bool quedaEnZona = SeQuedaEnZona(c, e);
                    c.TerminarArrastre();
                    Inspeccionar(null);
                    if (EnZona(c) && !quedaEnZona) PidioDevolver?.Invoke(c);
                    else if (!EnZona(c) && quedaEnZona) PidioElegir?.Invoke(c);
                    if (marcoZona != null) marcoZona.color = colorMarco;
                    break;
            }
            modo = Modo.Nada;
            presionada = arrastrada = null;
        }

        /// <summary>Cancela lo que se esté haciendo (al pasar turno, por ejemplo).</summary>
        public void Cancelar()
        {
            if (arrastrada != null) arrastrada.TerminarArrastre();
            Inspeccionar(null);
            modo = Modo.Nada;
            presionada = arrastrada = null;
            if (marcoZona != null) marcoZona.color = colorMarco;
        }

        void EmpezarArrastre(CardView carta, PointerEventData e)
        {
            modo = Modo.Arrastre;
            arrastrada = carta;
            Inspeccionar(null);
            carta.EmpezarArrastre(e);
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

        /// <summary>La carta de la mano más cercana en horizontal al dedo.</summary>
        CardView CartaBajo(float x)
        {
            CardView mejor = null;
            float distancia = float.MaxValue;
            foreach (var c in CartasEnMano())
            {
                float d = Mathf.Abs(RectTransformUtility.WorldToScreenPoint(null, c.Raiz.position).x - x);
                if (d < distancia) { distancia = d; mejor = c; }
            }
            return mejor;
        }

        /// <summary>
        /// La línea entre la mano y la zona de jugada es el borde de abajo de la zona (la
        /// caja de la mano es más alta que las cartas y se mete debajo de la zona).
        /// </summary>
        bool ArribaDeLaMano(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zona, e.position, null, out var p);
            return p.y > zona.rect.yMin;
        }

        /// <summary>
        /// Desde la mano basta con subirla por encima de esa línea; desde la zona, hay
        /// que bajarla hasta la mano para devolverla.
        /// </summary>
        bool SeQuedaEnZona(CardView carta, PointerEventData e) => ArribaDeLaMano(e);

        static bool EsMouse(PointerEventData e) =>
            e is ExtendedPointerEventData x ? x.pointerType == UIPointerType.MouseOrPen : e.pointerId < 0;
    }
}
