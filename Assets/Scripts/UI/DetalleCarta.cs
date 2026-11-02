using TMPro;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Cuadro con el detalle de la carta que se está inspeccionando: nombre, tipo y qué
    /// hace contra el enemigo actual. Se pone al lado de la carta (a la derecha si cabe).
    /// Si la carta tiene buffs o debuffs propios (Penitencia), debajo sale un cuadrito con
    /// cada uno: "Penitencia = +1 de costo". Las cartas sin estados solo muestran el detalle.
    /// </summary>
    public class DetalleCarta : MonoBehaviour
    {
        const float Margen = 4f;

        [SerializeField] CanvasGroup grupo;
        [SerializeField] TMP_Text nombre;
        [SerializeField] UnityEngine.UI.Image icono;
        [SerializeField] TMP_Text tipo;
        [SerializeField] TMP_Text texto;
        [Header("Estados de la carta")]
        [SerializeField] RectTransform cajaEstados;
        [SerializeField] TMP_Text textoEstados;

        RectTransform Rt => (RectTransform)transform;

        void Awake() => Ocultar();

        public void Mostrar(CardView carta, TiposDano tipos, string descripcion)
        {
            var t = tipos != null ? tipos.Buscar(carta.Datos.tipo) : null;
            nombre.text = carta.Datos.nombre;
            tipo.text = t != null ? t.nombre : carta.Datos.tipo;
            tipo.color = t != null ? t.color : Color.white;
            if (carta.Datos.EsArcano)
            {
                tipo.text = "Arcano · " + (t != null ? t.nombre : "Maná");
                tipo.color = t != null ? t.color : PaletaUI.Azul;
            }
            icono.enabled = t != null && t.icono != null;
            if (icono.enabled) icono.sprite = t.icono;
            texto.text = descripcion;
            MostrarEstados(carta);

            Colocar(carta);
            grupo.alpha = 1;
        }

        public void Ocultar() => grupo.alpha = 0;

        void MostrarEstados(CardView carta)
        {
            if (cajaEstados == null) return;
            bool hay = carta.Estados.Count > 0;
            cajaEstados.gameObject.SetActive(hay);
            if (!hay) return;
            var lineas = new System.Text.StringBuilder();
            foreach (var e in carta.Estados)
            {
                if (lineas.Length > 0) lineas.Append('\n');
                string color = ColorUtility.ToHtmlStringRGB(e.Color);
                lineas.Append($"<color=#{color}>{e.nombre}</color> = {e.resumen}");
            }
            bool combate = false;
            foreach (var e in carta.Estados) combate |= e.turnos <= 0;
            lineas.Append(combate ? "\n<color=#C9C1FF>Dura hasta el final del combate</color>" : "\n<color=#C9C1FF>Dura este turno</color>");
            textoEstados.text = lineas.ToString();
            textoEstados.ForceMeshUpdate();
            float alto = Mathf.Ceil(textoEstados.preferredHeight);
            textoEstados.rectTransform.sizeDelta = new Vector2(textoEstados.rectTransform.sizeDelta.x, alto);
            cajaEstados.sizeDelta = new Vector2(cajaEstados.sizeDelta.x, alto + 12);
        }

        /// <summary>Alto total con el cuadrito de estados, para que no se salga de la pantalla.</summary>
        float AltoTotal => Rt.rect.height + (cajaEstados != null && cajaEstados.gameObject.activeSelf ? cajaEstados.rect.height + 2 : 0);

        /// <summary>
        /// Calcula dónde quedará la carta ya levantada y pone el cuadro a su lado,
        /// sin salirse de la pantalla.
        /// </summary>
        void Colocar(CardView carta)
        {
            var padre = (RectTransform)Rt.parent;
            var esquinas = new Vector3[4];
            carta.Raiz.GetWorldCorners(esquinas);
            Vector2 abajoIzq = padre.InverseTransformPoint(esquinas[0]);
            Vector2 arribaDer = padre.InverseTransformPoint(esquinas[2]);
            // la carta inspeccionada crece 4/3 desde su centro y sube 30
            float ancho = (arribaDer.x - abajoIzq.x) * 4f / 3f;
            float centro = (abajoIzq.x + arribaDer.x) / 2f;
            float arriba = (abajoIzq.y + arribaDer.y) / 2f + (arribaDer.y - abajoIzq.y) * 2f / 3f + 30f;

            var tam = Rt.rect.size;
            var limites = padre.rect;
            float x = centro + ancho / 2f + Margen;
            if (x + tam.x > limites.xMax - Margen) x = centro - ancho / 2f - Margen - tam.x;
            float y = Mathf.Clamp(arriba, limites.yMin + Margen + AltoTotal, limites.yMax - Margen);

            Rt.anchorMin = Rt.anchorMax = new Vector2(0.5f, 0.5f);
            Rt.pivot = new Vector2(0, 1);
            Rt.anchoredPosition = new Vector2(Mathf.Round(x), Mathf.Round(y));
        }
    }
}
