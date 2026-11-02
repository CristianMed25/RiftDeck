using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// "Pasa el dispositivo": tapa la pantalla entera entre turnos para que el que entrega
    /// no vea la mano del que recibe. El nombre va en el color del jugador que recibe y la
    /// pantalla espera a que toque ESTOY LISTO. La lógica de turnos es del integrante 3:
    /// esto es solo la pantalla.
    /// </summary>
    public class PasaDispositivo : MonoBehaviour
    {
        static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");
        const float PasoTelefono = 0.35f;

        [SerializeField] CanvasGroup grupo;
        [Tooltip("Qué pasó antes de cambiar (el enemigo actuó, se protegió…)")]
        [SerializeField] TMP_Text motivo;
        [SerializeField] TMP_Text nombre;
        [SerializeField] TMP_Text sombraNombre;
        [SerializeField] TMP_Text instrucciones;
        [SerializeField] TMP_Text marcador;
        [SerializeField] TMP_Text flechaDesde;
        [SerializeField] TMP_Text flechaHacia;
        [SerializeField] RectTransform telefono;
        [SerializeField] UnityEngine.UI.Button listo;

        bool listoPulsado;

        void Awake()
        {
            listo.onClick.AddListener(() => listoPulsado = true);
            Ocultar();
        }

        void Ocultar()
        {
            grupo.alpha = 0;
            grupo.blocksRaycasts = false;
            grupo.interactable = false;
        }

        /// <summary>
        /// Tapa la pantalla, corre "mientras" ya tapada (cambiar la mano) y espera a que
        /// el jugador que recibe toque ESTOY LISTO.
        /// </summary>
        public IEnumerator Mostrar(string texto, int recibe, int puntosJ1, int puntosJ2, string ronda, Action mientras)
        {
            int entrega = recibe == 1 ? 2 : 1;
            var colorRecibe = "#" + ColorUtility.ToHtmlStringRGB(PaletaUI.DeJugador(recibe));
            motivo.text = texto;
            nombre.text = sombraNombre.text = Ajustes.Nombre(recibe).ToUpperInvariant();
            nombre.color = PaletaUI.DeJugador(recibe);
            instrucciones.text = $"Pásale el teléfono a <color={colorRecibe}>{Ajustes.Nombre(recibe)}</color>.\n" +
                                 $"<color=#B9B0F0>{Ajustes.Nombre(entrega)}, no mires sus cartas.</color>";
            marcador.text = $"<color=#{ColorUtility.ToHtmlStringRGB(PaletaUI.J1)}>{Ajustes.NombreCorto(1)} {puntosJ1.ToString("N0", Colombia)}</color>" +
                            $"   <color=#{ColorUtility.ToHtmlStringRGB(PaletaUI.Suave)}>{ronda}</color>   " +
                            $"<color=#{ColorUtility.ToHtmlStringRGB(PaletaUI.J2)}>{Ajustes.NombreCorto(2)} {puntosJ2.ToString("N0", Colombia)}</color>";
            flechaDesde.color = PaletaUI.DeJugador(entrega);
            flechaHacia.color = PaletaUI.DeJugador(recibe);

            grupo.blocksRaycasts = true;
            for (float t = 0; t < 0.15f; t += Time.unscaledDeltaTime) { grupo.alpha = t / 0.15f; yield return null; }
            grupo.alpha = 1;
            mientras?.Invoke();
            Ajustes.Vibrar();

            // el teléfono va y viene de a 2 px hacia el que recibe, hasta que toque listo
            listoPulsado = false;
            grupo.interactable = true;
            float x = telefono.anchoredPosition.x, reloj = 0;
            int paso = 0;
            while (!listoPulsado)
            {
                reloj += Time.unscaledDeltaTime;
                if (reloj >= PasoTelefono)
                {
                    reloj = 0;
                    paso = (paso + 1) % 3;
                    telefono.anchoredPosition = new Vector2(x + paso * 2 * (recibe == 2 ? 1 : -1), telefono.anchoredPosition.y);
                }
                yield return null;
            }
            telefono.anchoredPosition = new Vector2(x, telefono.anchoredPosition.y);
            grupo.interactable = false;

            for (float t = 0; t < 0.2f; t += Time.unscaledDeltaTime) { grupo.alpha = 1f - t / 0.2f; yield return null; }
            Ocultar();
        }
    }
}
