using System.Globalization;
using TMPro;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Escena Perfil (pantalla 3 del mockup, en versión local): una ficha por jugador con
    /// victorias, porcentaje ganado, récord y estilo de daño, y los últimos duelos abajo.
    /// Los datos vienen de <see cref="Estadisticas"/>.
    /// </summary>
    public class PantallaPerfil : MonoBehaviour
    {
        static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

        [System.Serializable]
        public class Ficha
        {
            public TMP_Text nombre, partidas, victorias, ganadas, record;
            [Tooltip("Una fila por tipo de daño, en el orden de TiposDano")]
            public FilaDano[] estilo;
        }

        [System.Serializable]
        public class FilaDano
        {
            public UnityEngine.UI.Image icono;
            public TMP_Text nombre;
            public UnityEngine.UI.Image barra;
            public TMP_Text porcentaje;
        }

        [System.Serializable]
        public class FilaDuelo
        {
            public UnityEngine.UI.Image etiqueta;
            public TMP_Text resultado, detalle;
        }

        [SerializeField] TiposDano tipos;
        [SerializeField] Ficha j1;
        [SerializeField] Ficha j2;
        [SerializeField] FilaDuelo[] duelos;
        [SerializeField] TMP_Text sinDuelos;

        void Start()
        {
            Llenar(j1, 1);
            Llenar(j2, 2);

            var lista = Estadisticas.UltimosDuelos();
            sinDuelos.gameObject.SetActive(lista.Count == 0);
            for (int i = 0; i < duelos.Length; i++)
            {
                var fila = duelos[i];
                bool hay = i < lista.Count;
                fila.etiqueta.transform.parent.gameObject.SetActive(hay);
                if (!hay) continue;
                var d = lista[i];
                int g = d.Ganador;
                fila.etiqueta.color = g == 0 ? PaletaUI.Oro : PaletaUI.DeJugador(g);
                fila.resultado.text = g == 0 ? "EMPATE" : "GANA " + Ajustes.NombreCorto(g).ToUpperInvariant();
                fila.detalle.text = $"{Ajustes.NombreCorto(1)} {d.puntosJ1.ToString("N0", Colombia)}  –  " +
                                    $"{d.puntosJ2.ToString("N0", Colombia)} {Ajustes.NombreCorto(2)}";
            }
        }

        void Llenar(Ficha f, int jugador)
        {
            int partidas = Estadisticas.Partidas, victorias = Estadisticas.Victorias(jugador);
            f.nombre.text = Ajustes.Nombre(jugador).ToUpperInvariant();
            f.nombre.color = PaletaUI.DeJugador(jugador);
            f.partidas.text = partidas == 1 ? "1 partida en este teléfono" : $"{partidas} partidas en este teléfono";
            f.victorias.text = victorias.ToString();
            f.ganadas.text = partidas > 0 ? Mathf.RoundToInt(100f * victorias / partidas) + "%" : "–";
            f.record.text = Estadisticas.Record(jugador).ToString("N0", Colombia);

            int total = 0;
            foreach (var t in tipos.tipos) total += Estadisticas.Dano(jugador, t.id);
            for (int i = 0; i < f.estilo.Length; i++)
            {
                var fila = f.estilo[i];
                bool hay = i < tipos.tipos.Count;
                fila.nombre.transform.parent.gameObject.SetActive(hay);
                if (!hay) continue;
                var t = tipos.tipos[i];
                float parte = total > 0 ? (float)Estadisticas.Dano(jugador, t.id) / total : 0;
                fila.icono.sprite = t.icono;
                fila.nombre.text = t.nombre;
                fila.barra.color = t.color;
                fila.barra.fillAmount = parte;
                fila.porcentaje.text = Mathf.RoundToInt(parte * 100) + "%";
            }
        }
    }
}
