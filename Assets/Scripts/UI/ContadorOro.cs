using TMPro;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>Muestra el oro del <see cref="Inventario"/> y salta un poco cuando cambia.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class ContadorOro : MonoBehaviour
    {
        TMP_Text texto;
        int mostrado = -1;
        float salto, baseY;

        void Awake()
        {
            texto = GetComponent<TMP_Text>();
            baseY = texto.rectTransform.anchoredPosition.y;
        }

        void OnEnable()
        {
            Inventario.Cambio += Refrescar;
            mostrado = Inventario.Oro;
            texto.text = mostrado.ToString();
        }

        void OnDisable() => Inventario.Cambio -= Refrescar;

        void Refrescar()
        {
            if (Inventario.Oro != mostrado) salto = 0.25f;
            mostrado = Inventario.Oro;
            texto.text = mostrado.ToString();
        }

        void Update()
        {
            if (salto <= 0) return;
            salto -= Time.unscaledDeltaTime;
            // sube 1 px y vuelve, sin perder la rejilla de píxeles
            texto.rectTransform.anchoredPosition = new Vector2(texto.rectTransform.anchoredPosition.x, baseY + (salto > 0.12f ? 1 : 0));
        }
    }
}
