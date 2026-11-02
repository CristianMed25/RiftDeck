using System;
using System.Collections;
using UnityEngine;

namespace RiftDeck.Partida
{
    /// <summary>
    /// Conecta el <see cref="GestorPartida"/> con la pantalla "pasa el dispositivo" del
    /// Integrante 2 (<see cref="RiftDeck.UI.PasaDispositivo"/>). No decide maná, cartas ni
    /// nada de combate: solo alterna el turno y deja que quien lo llame reaccione al cambio
    /// mediante un callback opcional, una vez la pantalla ya tapa al jugador anterior.
    /// </summary>
    public class ControladorTurnos : MonoBehaviour
    {
        [SerializeField] RiftDeck.UI.PasaDispositivo pasaDispositivo;

        GestorPartida partida;
        bool cambiandoTurno;

        /// <summary>True mientras la corrutina de cambio de turno está en curso.</summary>
        public bool EnCambioDeTurno => cambiandoTurno;
        public GestorPartida Partida => partida;

        /// <summary>
        /// Asigna el <see cref="GestorPartida"/> de la partida en curso. Lo llama quien
        /// arranca la partida (hoy la propia escena de Combate; a futuro, el bootstrapper
        /// local u online), ya que GestorPartida es una clase C# normal y no se puede
        /// enganchar desde el Inspector.
        /// </summary>
        public void Inicializar(GestorPartida partida) => this.partida = partida;

        /// <summary>
        /// Tapa la pantalla con <see cref="RiftDeck.UI.PasaDispositivo.Mostrar"/>, cambia el
        /// turno en <see cref="GestorPartida"/> ya con la pantalla cubierta, y por último
        /// corre <paramref name="alCambiar"/> para que el combate actualice mano/maná del
        /// jugador que acaba de recibir el dispositivo.
        /// </summary>
        /// <param name="texto">Motivo que se muestra arriba (p. ej. "El enemigo actúa · turno de").</param>
        /// <param name="alCambiar">Opcional: lo ejecuta el combate una vez cambiado el turno.</param>
        public IEnumerator CambiarTurno(string texto, Action alCambiar = null)
        {
            if (!PuedeCambiarTurno()) yield break;
            cambiandoTurno = true;

            int recibe = partida.JugadorContrario(partida.JugadorActivo);
            string ronda = $"RONDA {partida.RondaActual}/{GestorPartida.RondasTotales}";

            yield return pasaDispositivo.Mostrar(texto, recibe,
                partida.ObtenerPuntosTotales(1), partida.ObtenerPuntosTotales(2), ronda, () =>
                {
                    // ya con la pantalla tapada: así el jugador que entrega no ve el cambio
                    partida.SiguienteTurno();
                    alCambiar?.Invoke();
                });

            cambiandoTurno = false;
        }

        bool PuedeCambiarTurno()
        {
            if (partida == null)
            {
                UnityEngine.Debug.LogError("ControladorTurnos: falta asignar el GestorPartida (llama Inicializar).");
                return false;
            }
            if (pasaDispositivo == null)
            {
                UnityEngine.Debug.LogError("ControladorTurnos: falta asignar PasaDispositivo en el Inspector.");
                return false;
            }
            if (!partida.PartidaIniciada || partida.PartidaTerminada)
            {
                UnityEngine.Debug.LogWarning("ControladorTurnos: no se puede cambiar de turno; la partida no está activa.");
                return false;
            }
            if (cambiandoTurno)
            {
                UnityEngine.Debug.LogWarning("ControladorTurnos: ya hay un cambio de turno en curso.");
                return false;
            }
            return true;
        }
    }
}
