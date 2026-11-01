using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Ficha de un jugador en la barra superior: retrato, nombre, vida en corazones y
    /// puntos. Los dos jugadores se ven siempre; el que juega lleva borde Oro y el otro
    /// queda un poco apagado.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

        [SerializeField] TMP_Text nombre;
        [SerializeField] TMP_Text puntos;
        [SerializeField] UnityEngine.UI.Image[] corazones;
        [SerializeField] Sprite corazonLleno;
        [SerializeField] Sprite corazonVacio;
        [SerializeField] UnityEngine.UI.Image marcoActivo;
        [SerializeField] CanvasGroup grupo;
        [Tooltip("Una gema por aumento elegido, del color de su calidad")]
        [SerializeField] UnityEngine.UI.Image[] aumentos;
        [SerializeField] Material prismatico;

        public void Mostrar(int jugador, int vida, int puntosActuales, bool activo)
        {
            nombre.text = $"J{jugador} · {(activo ? "juega" : "espera")}";
            nombre.color = PaletaUI.DeJugador(jugador);
            puntos.text = puntosActuales.ToString("N0", Colombia);
            for (int i = 0; i < corazones.Length; i++)
                corazones[i].sprite = i < vida ? corazonLleno : corazonVacio;
            marcoActivo.enabled = activo;
            grupo.alpha = activo ? 1f : 0.72f;
        }

        public void MostrarAumentos(IReadOnlyList<DatosAumento> elegidos)
        {
            for (int i = 0; i < aumentos.Length; i++)
            {
                bool hay = i < elegidos.Count;
                aumentos[i].color = hay ? DatosAumento.ColorDe(elegidos[i].calidad) : PaletaUI.Hueco;
                aumentos[i].material = hay && elegidos[i].calidad == CalidadAumento.Prismatico ? prismatico : null;
            }
        }
    }
}
