using TMPro;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Cuadro con el detalle de la carta que se está inspeccionando: nombre, tipo y qué
    /// hace contra el enemigo actual. Se pone al lado de la carta (a la derecha si cabe).
    /// </summary>
    public class DetalleCarta : MonoBehaviour
    {
        const float Margen = 4f;

        [SerializeField] CanvasGroup grupo;
        [SerializeField] TMP_Text nombre;
        [SerializeField] UnityEngine.UI.Image icono;
        [SerializeField] TMP_Text tipo;
        [SerializeField] TMP_Text texto;

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

            Colocar(carta);
            grupo.alpha = 1;
        }

        public void Ocultar() => grupo.alpha = 0;

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
            float y = Mathf.Clamp(arriba, limites.yMin + Margen + tam.y, limites.yMax - Margen);

            Rt.anchorMin = Rt.anchorMax = new Vector2(0.5f, 0.5f);
            Rt.pivot = new Vector2(0, 1);
            Rt.anchoredPosition = new Vector2(Mathf.Round(x), Mathf.Round(y));
        }
    }
}
