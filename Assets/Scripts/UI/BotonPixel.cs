using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RiftDeck.UI
{
    /// <summary>
    /// Respuesta de los botones: al presionar se hunde sobre su sombra y al soltar
    /// rebota. Usa la escala, que los Layout Groups no tocan.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class BotonPixel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float Hundido = 0.94f;
        const float Rebote = 1.08f;
        const float DuracionRebote = 0.18f;

        UnityEngine.UI.Button boton;
        UnityEngine.UI.Shadow sombra;
        Vector2 distanciaSombra;
        bool presionado;

        void Awake()
        {
            boton = GetComponent<UnityEngine.UI.Button>();
            sombra = GetComponent<UnityEngine.UI.Shadow>();
            if (sombra != null) distanciaSombra = sombra.effectDistance;
        }

        void OnDisable()
        {
            presionado = false;
            transform.localScale = Vector3.one;
            if (sombra != null) sombra.effectDistance = distanciaSombra;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!boton.IsInteractable()) return;
            presionado = true;
            StopAllCoroutines();
            transform.localScale = Vector3.one * Hundido;
            if (sombra != null) sombra.effectDistance = distanciaSombra * 0.5f;
        }

        public void OnPointerUp(PointerEventData e) => Soltar(true);
        public void OnPointerExit(PointerEventData e) => Soltar(false);

        void Soltar(bool rebotar)
        {
            if (!presionado) return;
            presionado = false;
            if (sombra != null) sombra.effectDistance = distanciaSombra;
            StopAllCoroutines();
            if (rebotar && isActiveAndEnabled) StartCoroutine(Rebotar());
            else transform.localScale = Vector3.one;
        }

        IEnumerator Rebotar()
        {
            for (float t = 0; t < DuracionRebote; t += Time.unscaledDeltaTime)
            {
                float k = t / DuracionRebote;
                // sube rápido hasta Rebote y vuelve a 1 con una curva suave
                float escala = k < 0.4f ? Mathf.Lerp(Hundido, Rebote, k / 0.4f) : Mathf.Lerp(Rebote, 1f, (k - 0.4f) / 0.6f);
                transform.localScale = Vector3.one * escala;
                yield return null;
            }
            transform.localScale = Vector3.one;
        }
    }
}
