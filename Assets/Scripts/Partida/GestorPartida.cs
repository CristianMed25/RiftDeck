using System;

namespace RiftDeck.Partida
{
    /// <summary>
    /// Administra el estado general de una partida local de 2 jugadores: jugador activo,
    /// ronda actual y puntuación por ronda de cada uno. No conoce la interfaz ni la lógica
    /// de combate (eso lo decide quien use esta clase); solo el estado y las reglas de
    /// alternancia de turno y fin de partida tras las 4 rondas.
    /// Pensada para que la use un controlador local (pasando el teléfono) y, más adelante,
    /// una implementación en red: no depende de GameObjects, escenas ni componentes de Unity.
    /// </summary>
    public class GestorPartida
    {
        public const int RondasPorDefecto = 4;

        /// <summary>Puntuación de un jugador, una casilla por ronda. Nunca se expone el array.</summary>
        class EstadoJugador
        {
            public readonly int[] puntosPorRonda;
            public EstadoJugador(int rondasTotales) => puntosPorRonda = new int[rondasTotales];
        }

        readonly EstadoJugador[] jugadores;
        readonly bool[] turnosCompletados = new bool[2];

        public int RondasTotales { get; }
        public int JugadorActivo { get; private set; } = 1;
        public int RondaActual { get; private set; } = 1;
        public bool PartidaIniciada { get; private set; }
        public bool PartidaTerminada { get; private set; }

        /// <summary>Cuántas rondas tiene esta partida (por defecto 4; la decide quien la crea, p. ej. según el modo elegido).</summary>
        public GestorPartida(int rondasTotales = RondasPorDefecto)
        {
            if (rondasTotales < 1)
                throw new ArgumentOutOfRangeException(nameof(rondasTotales), "Debe haber al menos 1 ronda.");
            RondasTotales = rondasTotales;
            jugadores = new[] { new EstadoJugador(rondasTotales), new EstadoJugador(rondasTotales) };
        }

        /// <summary>Pone el estado en cero y empieza en el jugador 1, ronda 1.</summary>
        public void IniciarPartida()
        {
            foreach (var j in jugadores) Array.Clear(j.puntosPorRonda, 0, j.puntosPorRonda.Length);
            JugadorActivo = 1;
            RondaActual = 1;
            Array.Clear(turnosCompletados, 0, turnosCompletados.Length);
            PartidaTerminada = false;
            PartidaIniciada = true;
        }

        /// <summary>Para la revancha: es una partida nueva, no continúa la anterior.</summary>
        public void ReiniciarPartida() => IniciarPartida();

        /// <summary>El otro jugador (1↔2), sin cambiar el estado.</summary>
        public int JugadorContrario(int jugador)
        {
            ValidarJugador(jugador);
            return jugador == 1 ? 2 : 1;
        }

        /// <summary>Pasa el turno al otro jugador.</summary>
        public void SiguienteTurno()
        {
            RequerirPartidaActiva();
            JugadorActivo = JugadorContrario(JugadorActivo);
        }

        /// <summary>
        /// Registra que el jugador activo terminó su turno. La ronda solo avanza cuando
        /// ambos jugadores han terminado la ronda actual.
        /// </summary>
        public void RegistrarTurnoCompletado(int jugador)
        {
            ValidarJugador(jugador);
            RequerirPartidaActiva();
            turnosCompletados[jugador - 1] = true;

            if (!turnosCompletados[0] || !turnosCompletados[1])
                return;

            Array.Clear(turnosCompletados, 0, turnosCompletados.Length);
            AvanzarRonda();
        }

        /// <summary>Suma puntos al jugador indicado, en la ronda actual.</summary>
        public void SumarPuntos(int jugador, int puntos)
        {
            ValidarJugador(jugador);
            RequerirPartidaActiva();
            if (puntos < 0) throw new ArgumentOutOfRangeException(nameof(puntos), "Los puntos no pueden ser negativos.");
            jugadores[jugador - 1].puntosPorRonda[RondaActual - 1] += puntos;
        }

        /// <summary>Puntos de un jugador en una ronda concreta (1 a RondasTotales).</summary>
        public int ObtenerPuntosRonda(int jugador, int ronda)
        {
            ValidarJugador(jugador);
            ValidarRonda(ronda);
            return jugadores[jugador - 1].puntosPorRonda[ronda - 1];
        }

        /// <summary>Copia de los puntos por ronda del jugador (no el array interno).</summary>
        public int[] ObtenerPuntosPorRonda(int jugador)
        {
            ValidarJugador(jugador);
            return (int[])jugadores[jugador - 1].puntosPorRonda.Clone();
        }

        /// <summary>Suma de los puntos de todas las rondas jugadas hasta ahora.</summary>
        public int ObtenerPuntosTotales(int jugador)
        {
            ValidarJugador(jugador);
            int total = 0;
            foreach (var puntos in jugadores[jugador - 1].puntosPorRonda) total += puntos;
            return total;
        }

        /// <summary>
        /// Cierra la ronda actual. Si ya era la última (RondasTotales), termina la partida
        /// en vez de avanzar.
        /// </summary>
        public void AvanzarRonda()
        {
            RequerirPartidaActiva();
            if (RondaActual == RondasTotales)
            {
                PartidaTerminada = true;
                return;
            }

            RondaActual++;
        }

        void RequerirPartidaActiva()
        {
            if (!PartidaIniciada) throw new InvalidOperationException("La partida no se ha iniciado. Llama IniciarPartida() primero.");
            if (PartidaTerminada) throw new InvalidOperationException("La partida ya terminó.");
        }

        static void ValidarJugador(int jugador)
        {
            if (jugador != 1 && jugador != 2)
                throw new ArgumentOutOfRangeException(nameof(jugador), "El jugador debe ser 1 o 2.");
        }

        void ValidarRonda(int ronda)
        {
            if (ronda < 1 || ronda > RondasTotales)
                throw new ArgumentOutOfRangeException(nameof(ronda), $"La ronda debe estar entre 1 y {RondasTotales}.");
        }
    }
}
