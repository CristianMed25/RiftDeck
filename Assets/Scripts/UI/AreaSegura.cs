using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Ajusta un RectTransform al área segura de la pantalla para que el notch o
    /// la cámara del teléfono no tapen la interfaz. Va en un panel hijo directo del
    /// Canvas que contenga todo lo interactivo; el fondo queda fuera para cubrir
    /// toda la pantalla.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class AreaSegura : MonoBehaviour
    {
        RectTransform rect;
        Rect ultimaArea;
        Vector2Int ultimaResolucion;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            Aplicar();
        }

        void Update()
        {
            if (Screen.safeArea != ultimaArea ||
                Screen.width != ultimaResolucion.x || Screen.height != ultimaResolucion.y)
                Aplicar();
        }

        void Aplicar()
        {
            ultimaArea = Screen.safeArea;
            ultimaResolucion = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width == 0 || Screen.height == 0) return;

            var min = ultimaArea.position;
            var max = ultimaArea.position + ultimaArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
