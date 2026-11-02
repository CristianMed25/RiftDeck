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
        [SerializeField] DatosEnemigo enemigoObjetivo;
        [SerializeField] PlayerHUD hudJugador1;
        [SerializeField] PlayerHUD hudJugador2;
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
                ResistenciasData resistencias = resistenciasObjetivo;
                if (enemigoObjetivo != null)
                    resistencias = ResistenciasData.DesdeEnemigo(enemigoObjetivo);

                if (resistencias == null)
                    throw new InvalidOperationException("Falta asignar las resistencias del objetivo.");

                int puntos = motor.CalcularPuntos(vista.Carta, resistencias);
                if (enemigoObjetivo != null)
                    Destroy(resistencias);
                jugadores[jugador - 1].AgregarPuntos(puntos);
                partida.SumarPuntos(jugador, puntos);
                ActualizarHud(partida, jugador);
                puntuacionActualizada?.Invoke(partida.ObtenerPuntosTotales(jugador));
                if (!manoCartas.EnZona(vista))
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

        void ActualizarHud(GestorPartida partida, int jugadorActivo)
        {
            if (hudJugador1 != null)
                hudJugador1.Mostrar(1, 0, partida.ObtenerPuntosTotales(1), jugadorActivo == 1);
            if (hudJugador2 != null)
                hudJugador2.Mostrar(2, 0, partida.ObtenerPuntosTotales(2), jugadorActivo == 2);
        }
    }
}
