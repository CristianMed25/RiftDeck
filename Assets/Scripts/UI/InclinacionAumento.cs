using UnityEngine;
using UnityEngine.EventSystems;

namespace RiftDeck.UI
{
    /// <summary>
    /// Lee el dedo (o el mouse) sobre una tarjeta de aumento y dice hacia dónde se inclina,
    /// estilo Balatro. Quien la usa (PantallaAumentos) lo pasa al shader prismático y mueve
    /// las capas del ícono para dar profundidad.
    /// </summary>
    public class InclinacionAumento : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        const float Suavizado = 12f;

        RectTransform rect;
        Vector2 objetivo;
        bool dentro;

        /// <summary>Hacia dónde se inclina, de -1 a 1 en cada eje (x a la derecha, y arriba).</summary>
        public Vector2 Direccion { get; private set; }
        /// <summary>Cuánto manda el dedo: 0 suelta (el brillo se mece solo), 1 encima.</summary>
        public float Peso { get; private set; }

        void Awake() => rect = (RectTransform)transform;

        void OnDisable()
        {
            dentro = false;
            Direccion = Vector2.zero;
            Peso = 0;
        }

        public void OnPointerEnter(PointerEventData e) { dentro = true; Apuntar(e); }
        public void OnPointerExit(PointerEventData e) => dentro = false;
        public void OnPointerMove(PointerEventData e) { if (dentro) Apuntar(e); }

        void Apuntar(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out var local))
                return;
            var r = rect.rect;
            objetivo = new Vector2(
                Mathf.Clamp((local.x - r.center.x) / (r.width * 0.5f), -1, 1),
                Mathf.Clamp((local.y - r.center.y) / (r.height * 0.5f), -1, 1));
        }

        void Update()
        {
            float k = 1 - Mathf.Exp(-Suavizado * Time.unscaledDeltaTime);
            Direccion = Vector2.Lerp(Direccion, dentro ? objetivo : Vector2.zero, k);
            Peso = Mathf.Lerp(Peso, dentro ? 1 : 0, k);
        }
    }
}
