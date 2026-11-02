using System.Collections.Generic;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Lo que muestra el Perfil, guardado en el teléfono (PlayerPrefs): partidas, victorias y
    /// récord de cada jugador, cuánto daño hizo de cada tipo y los últimos duelos.
    /// "Borrar progreso" en Configuración lo borra junto con el oro.
    /// </summary>
    public static class Estadisticas
    {
        public const int DuelosGuardados = 3;

        public struct Duelo
        {
            public int puntosJ1, puntosJ2;
            /// <summary>0 = empate.</summary>
            public int Ganador => puntosJ1 == puntosJ2 ? 0 : puntosJ1 > puntosJ2 ? 1 : 2;
        }

        public static int Partidas => PlayerPrefs.GetInt("est_partidas", 0);
        public static int Victorias(int jugador) => PlayerPrefs.GetInt("est_victorias_j" + jugador, 0);
        public static int Record(int jugador) => PlayerPrefs.GetInt("est_record_j" + jugador, 0);
        public static int Dano(int jugador, string tipo) => PlayerPrefs.GetInt($"est_dano_j{jugador}_{tipo}", 0);

        /// <summary>Lo llama el combate en cada jugada (se guarda en disco al terminar la partida).</summary>
        public static void SumarDano(int jugador, string tipo, int dano)
        {
            if (dano <= 0 || string.IsNullOrEmpty(tipo)) return;
            PlayerPrefs.SetInt($"est_dano_j{jugador}_{tipo}", Dano(jugador, tipo) + dano);
        }

        /// <summary>Una vez por partida, desde la pantalla de resultados.</summary>
        public static void RegistrarPartida(int totalJ1, int totalJ2)
        {
            PlayerPrefs.SetInt("est_partidas", Partidas + 1);
            var duelo = new Duelo { puntosJ1 = totalJ1, puntosJ2 = totalJ2 };
            if (duelo.Ganador != 0) PlayerPrefs.SetInt("est_victorias_j" + duelo.Ganador, Victorias(duelo.Ganador) + 1);
            PlayerPrefs.SetInt("est_record_j1", Mathf.Max(Record(1), totalJ1));
            PlayerPrefs.SetInt("est_record_j2", Mathf.Max(Record(2), totalJ2));

            // el más nuevo primero
            for (int i = DuelosGuardados - 1; i > 0; i--)
                PlayerPrefs.SetString("est_duelo_" + i, PlayerPrefs.GetString("est_duelo_" + (i - 1), ""));
            PlayerPrefs.SetString("est_duelo_0", totalJ1 + "|" + totalJ2);
            PlayerPrefs.Save();
        }

        public static List<Duelo> UltimosDuelos()
        {
            var lista = new List<Duelo>();
            for (int i = 0; i < DuelosGuardados; i++)
            {
                var partes = PlayerPrefs.GetString("est_duelo_" + i, "").Split('|');
                if (partes.Length == 2 && int.TryParse(partes[0], out int a) && int.TryParse(partes[1], out int b))
                    lista.Add(new Duelo { puntosJ1 = a, puntosJ2 = b });
            }
            return lista;
        }
    }

    /// <summary>
    /// Lo que se elige antes del combate (pantallas Modo y Preparación): cuántas rondas,
    /// la semilla compartida y el primer enemigo que se mostró de adelanto.
    /// </summary>
    public static class ModoPartida
    {
        public const int RondasDuelo = 4;
        public const int RondasRapida = 1;

        public static int Rondas { get; set; } = RondasDuelo;
        public static bool Rapida => Rondas == RondasRapida;
        /// <summary>0 = sin semilla (al azar).</summary>
        public static int Semilla { get; set; }
        /// <summary>El enemigo que se vio en la preparación; el combate lo usa una vez y lo borra.</summary>
        public static DatosEnemigo PrimerEnemigo { get; set; }

        public static string Codigo(int semilla) => $"#{(semilla >> 12) & 0xFFF:X3}-{semilla & 0xFFF:X3}";
    }
}
