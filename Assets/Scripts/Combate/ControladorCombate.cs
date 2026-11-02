using System;
using UnityEngine;
using UnityEngine.Events;
using RiftDeck.Partida;
using RiftDeck.UI;

namespace RiftDeck.Combate
{
    /// <summary>
    /// Adaptador de escena: traduce la selección de la UI al servicio de combate y
    /// devuelve el resultado al flujo local de dos jugadores.
    /// </summary>
    public class ControladorCombate : MonoBehaviour
    {
        [SerializeField] ManoCartas manoCartas;
        [SerializeField] ControladorTurnos controladorTurnos;
        [SerializeField] ResistenciasData resistenciasObjetivo;
        [SerializeField] UnityEvent<int> puntuacionActualizada;
        [SerializeField] UnityEvent uiRefrescada;

        readonly JugadorModel[] jugadores = { new JugadorModel(1), new JugadorModel(2) };
        readonly CombatEngine motor = new CombatEngine();

        void OnEnable()
        {
            if (manoCartas != null) manoCartas.PidioElegir += AlElegirCarta;
        }

        void OnDisable()
        {
            if (manoCartas != null) manoCartas.PidioElegir -= AlElegirCarta;
        }

        void AlElegirCarta(CardView vista)
        {
            if (vista == null || vista.Carta == null)
            {
                Debug.LogError("ControladorCombate: la carta seleccionada no tiene un CartaSO asignado.");
                return;
            }

            GestorPartida partida = controladorTurnos != null ? controladorTurnos.Partida : null;
            if (partida == null || !partida.PartidaIniciada || partida.PartidaTerminada)
            {
                Debug.LogWarning("ControladorCombate: no hay una partida activa.");
                return;
            }

            try
            {
                int jugador = partida.JugadorActivo;
                int puntos = motor.CalcularPuntos(vista.Carta, resistenciasObjetivo);
                jugadores[jugador - 1].AgregarCarta(vista.Carta);
                jugadores[jugador - 1].UsarCarta(vista.Carta);
                jugadores[jugador - 1].AgregarPuntos(puntos);
                partida.SumarPuntos(jugador, puntos);
                puntuacionActualizada?.Invoke(puntos);
                manoCartas.MoverAZona(vista);
                StartCoroutine(controladorTurnos.CambiarTurno(
                    $"Jugador {jugador} jugó una carta",
                    () => uiRefrescada?.Invoke()));
            }
            catch (ArgumentException excepcion)
            {
                Debug.LogError($"ControladorCombate: no se pudo resolver la jugada. {excepcion.Message}");
            }
            catch (InvalidOperationException excepcion)
            {
                Debug.LogError($"ControladorCombate: configuración de combate inválida. {excepcion.Message}");
            }
        }
    }
}
