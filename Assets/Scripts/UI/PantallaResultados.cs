using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Lo que deja la partida para la pantalla de resultados: los puntos de cada jugador
    /// en cada ronda. El integrante 3 lo llenará desde su controlador de partida.
    /// </summary>
    public static class ResultadoPartida
    {
        public const int OroPorPartida = 60;

        public static int[] PuntosJ1 { get; private set; }
        public static int[] PuntosJ2 { get; private set; }
        /// <summary>Si ya se sumó el oro de esta partida (para no darlo dos veces).</summary>
        public static bool OroCobrado { get; private set; }

        public static bool HayPartida => PuntosJ1 != null;

        public static void Guardar(int[] puntosJ1, int[] puntosJ2)
        {
            PuntosJ1 = (int[])puntosJ1.Clone();
            PuntosJ2 = (int[])puntosJ2.Clone();
            OroCobrado = false;
        }

        public static void CobrarOro()
        {
            if (OroCobrado) return;
            OroCobrado = true;
            Inventario.GanarOro(OroPorPartida);
        }
    }

    /// <summary>
    /// Escena Resultados: título, quién gana, oro ganado, el Bufón celebrando y la tabla
    /// de rondas (J1 contra J2). REVANCHA vuelve a Combate y MENÚ a la escena Menu.
    /// Sin partida (al abrir la escena sola en el editor) muestra datos de ejemplo.
    /// </summary>
    public class PantallaResultados : MonoBehaviour
    {
        static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

        [SerializeField] TMP_Text[] titulo;
        [SerializeField] TMP_Text ganador;
        [SerializeField] TMP_Text oro;
        [SerializeField] TMP_Text nombreJ1;
        [SerializeField] TMP_Text nombreJ2;
        [SerializeField] TMP_Text marcador;
        [Tooltip("Una fila por ronda y la última para el total")]
        [SerializeField] FilaRonda[] filas;
        [SerializeField] GameObject[] chispas;

        [System.Serializable]
        public class FilaRonda
        {
            public TMP_Text etiqueta, puntosJ1, quien, puntosJ2;
        }

        void Start()
        {
            var j1 = ResultadoPartida.HayPartida ? ResultadoPartida.PuntosJ1 : new[] { 2410, 2520, 3100, 3480 };
            var j2 = ResultadoPartida.HayPartida ? ResultadoPartida.PuntosJ2 : new[] { 2180, 2600, 2870, 3050 };
            if (ResultadoPartida.HayPartida) ResultadoPartida.CobrarOro();

            int rondasJ1 = 0, rondasJ2 = 0, totalJ1 = 0, totalJ2 = 0;
            for (int i = 0; i < filas.Length; i++)
            {
                bool esTotal = i == filas.Length - 1;
                int a, b;
                if (esTotal) { a = totalJ1; b = totalJ2; }
                else if (i < j1.Length) { a = j1[i]; b = j2[i]; totalJ1 += a; totalJ2 += b; }
                else { filas[i].etiqueta.transform.parent.gameObject.SetActive(false); continue; }
                if (!esTotal && a != b) { if (a > b) rondasJ1++; else rondasJ2++; }
                MostrarFila(filas[i], esTotal ? "TOTAL" : $"RONDA {i + 1}", a, b);
            }

            nombreJ1.text = Ajustes.NombreCorto(1).ToUpperInvariant();
            nombreJ2.text = Ajustes.NombreCorto(2).ToUpperInvariant();
            marcador.text = $"{rondasJ1}–{rondasJ2}";

            // gana el que hizo más puntos en total; las rondas son el desglose
            int quien = totalJ1 == totalJ2 ? 0 : totalJ1 > totalJ2 ? 1 : 2;
            foreach (var t in titulo) t.text = quien == 0 ? "¡EMPATE!" : "¡VICTORIA!";
            ganador.text = quien == 0 ? "Nadie se rinde" : "Gana " + Ajustes.Nombre(quien);
            ganador.color = quien == 0 ? PaletaUI.Oro : PaletaUI.DeJugador(quien);
            oro.text = "+" + ResultadoPartida.OroPorPartida;

            StartCoroutine(Parpadear());
        }

        static void MostrarFila(FilaRonda fila, string etiqueta, int a, int b)
        {
            fila.etiqueta.text = etiqueta;
            fila.puntosJ1.text = a.ToString("N0", Colombia);
            fila.puntosJ2.text = b.ToString("N0", Colombia);
            // el que gana la fila va en Oro; el marcador del medio dice quién (no solo el color)
            fila.puntosJ1.color = a > b ? PaletaUI.Oro : PaletaUI.Blanco;
            fila.puntosJ2.color = b > a ? PaletaUI.Oro : PaletaUI.Blanco;
            int quien = a == b ? 0 : a > b ? 1 : 2;
            fila.quien.text = quien == 0 ? "=" : Ajustes.NombreCorto(quien).ToUpperInvariant();
            fila.quien.color = quien == 0 ? PaletaUI.Suave : PaletaUI.DeJugador(quien);
        }

        IEnumerator Parpadear()
        {
            // las chispas alrededor del Bufón se prenden de a una, en 3 pasos
            for (int paso = 0; ; paso++)
            {
                for (int i = 0; i < chispas.Length; i++) chispas[i].SetActive((i + paso) % 3 != 0);
                yield return new WaitForSecondsRealtime(0.4f);
            }
        }
    }
}
