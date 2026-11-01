using System.Collections;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Splash dentro de la escena del menú: llena la barra de carga y se oculta. Solo
    /// sale al abrir el juego, no al volver del combate. En la escena se guarda apagado
    /// para que no tape el menú mientras se edita; lo enciende el Canvas al arrancar.
    /// </summary>
    public class PantallaSplash : MonoBehaviour
    {
        static bool yaMostrado;

        [SerializeField] UnityEngine.UI.Image relleno;
        [SerializeField] CanvasGroup grupo;
        [SerializeField] float duracion = 1.6f;
        [SerializeField] float desvanecer = 0.25f;

        /// <summary>Lo llama <see cref="ArranqueMenu"/> en Awake.</summary>
        public void MostrarSiToca()
        {
            gameObject.SetActive(!yaMostrado);
            yaMostrado = true;
        }

        IEnumerator Start()
        {
            grupo.alpha = 1;
            grupo.blocksRaycasts = true;
            for (float t = 0; t < duracion; t += Time.deltaTime)
            {
                // la barra avanza a saltos de 1/12 para que se vea pixelada
                relleno.fillAmount = Mathf.Floor(t / duracion * 12f) / 12f;
                yield return null;
            }
            relleno.fillAmount = 1;
            yield return new WaitForSeconds(0.2f);
            for (float t = 0; t < desvanecer; t += Time.deltaTime)
            {
                grupo.alpha = 1f - t / desvanecer;
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
