using System.Collections.Generic;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Un número armado con sprites de cifras (Art/UI/Cifras, de tools/cifras.py), centrado
    /// en su RectTransform y en píxel entero. Las cifras son blancas con contorno de Tinta,
    /// así que el color tiñe solo el relleno: el mismo sprite sirve para un costo normal,
    /// uno que subió (rojo) o uno que bajó (menta).
    /// </summary>
    public class NumeroPixel : MonoBehaviour
    {
        // los contornos de dos cifras seguidas se comparten (1 px)
        const int Solape = 1;

        readonly List<UnityEngine.UI.Image> imagenes = new List<UnityEngine.UI.Image>();

        /// <param name="cifras">Los 10 sprites, del 0 al 9.</param>
        public void Mostrar(int numero, Color color, Sprite[] cifras)
        {
            if (cifras == null || cifras.Length < 10) return;
            string texto = Mathf.Max(0, numero).ToString();
            while (imagenes.Count < texto.Length) imagenes.Add(Nueva());

            int ancho = -Solape * (texto.Length - 1);
            foreach (char c in texto) ancho += (int)cifras[c - '0'].rect.width;
            // borde izquierdo en píxel entero aunque el ancho sea impar
            int x = -Mathf.FloorToInt(ancho / 2f);
            for (int i = 0; i < imagenes.Count; i++)
            {
                var img = imagenes[i];
                bool hay = i < texto.Length;
                img.gameObject.SetActive(hay);
                if (!hay) continue;
                var s = cifras[texto[i] - '0'];
                img.sprite = s;
                img.color = color;
                img.rectTransform.sizeDelta = s.rect.size;
                img.rectTransform.anchoredPosition = new Vector2(x, -Mathf.FloorToInt(s.rect.height / 2f));
                x += (int)s.rect.width - Solape;
            }
        }

        UnityEngine.UI.Image Nueva()
        {
            var go = new GameObject("Cifra", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image))
                { layer = gameObject.layer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = Vector2.zero;
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.raycastTarget = false;
            return img;
        }
    }
}
