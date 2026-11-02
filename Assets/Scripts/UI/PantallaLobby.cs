using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RiftDeck.UI
{
    /// <summary>
    /// Escena Lobby (pantalla 5 del mockup), en versión local: "Preparación del duelo".
    /// Muestra a J1 contra J2, la semilla compartida y el primer enemigo con sus
    /// debilidades, y cuenta 3, 2, 1 antes de abrir el combate.
    /// </summary>
    public class PantallaLobby : MonoBehaviour
    {
        const int Cuenta = 3;

        [SerializeField] TMP_Text modo;
        [SerializeField] TMP_Text nombreJ1;
        [SerializeField] TMP_Text nombreJ2;
        [SerializeField] GameObject[] listos;
        [SerializeField] TMP_Text semilla;
        [SerializeField] DatosEnemigo[] primeros;
        [SerializeField] TiposDano tipos;
        [SerializeField] UnityEngine.UI.Image enemigo;
        [SerializeField] TMP_Text nombreEnemigo;
        [Tooltip("Hasta 2 celdas: la debilidad y la resistencia del primer enemigo")]
        [SerializeField] UnityEngine.UI.Image[] iconos;
        [SerializeField] TMP_Text[] etiquetas;
        [SerializeField] TMP_Text numero;
        [SerializeField] TMP_Text consejo;
        [SerializeField] string escenaCombate = "Combate";

        static readonly string[] Consejos =
        {
            "Consejo: guarda reserva de maná para el élite.",
            "Consejo: toca ? para ver qué resiste el enemigo.",
            "Consejo: el Caballero se levanta una vez. Guarda daño.",
            "Consejo: 3 arcanos de la misma familia forman un trío.",
        };

        void Start()
        {
            modo.text = ModoPartida.Rapida ? "PARTIDA RÁPIDA · 1 RONDA" : $"DUELO LOCAL · {ModoPartida.RondasDuelo} RONDAS";
            nombreJ1.text = Ajustes.NombreCorto(1).ToUpperInvariant();
            nombreJ2.text = Ajustes.NombreCorto(2).ToUpperInvariant();

            ModoPartida.Semilla = Random.Range(1, 0xFFFFFF);
            semilla.text = ModoPartida.Codigo(ModoPartida.Semilla);
            var rng = new System.Random(ModoPartida.Semilla);
            var primero = primeros[rng.Next(primeros.Length)];
            ModoPartida.PrimerEnemigo = primero;
            MostrarEnemigo(primero);
            consejo.text = Consejos[rng.Next(Consejos.Length)];

            StartCoroutine(Contar());
        }

        void MostrarEnemigo(DatosEnemigo e)
        {
            var sprite = e.clips[0].cuadros[0];
            enemigo.sprite = sprite;
            // entero y que quepa en 40 px de alto
            int escala = Mathf.Max(1, Mathf.FloorToInt(40f / sprite.rect.height));
            enemigo.rectTransform.sizeDelta = sprite.rect.size * escala;
            nombreEnemigo.text = e.nombre;

            int k = 0;
            foreach (var r in e.resistencias)
            {
                if (k >= iconos.Length || r.multiplicador == 1f) continue;
                var t = tipos.Buscar(r.tipo);
                iconos[k].sprite = t != null ? t.icono : null;
                etiquetas[k].text = r.multiplicador > 1f ? "DÉBIL" : "RES";
                etiquetas[k].color = r.multiplicador > 1f ? PaletaUI.Menta : PaletaUI.Rosa;
                k++;
            }
            for (int i = k; i < iconos.Length; i++) iconos[i].transform.parent.gameObject.SetActive(false);
        }

        IEnumerator Contar()
        {
            // los LISTO aparecen uno tras otro
            foreach (var l in listos) l.SetActive(false);
            foreach (var l in listos)
            {
                yield return new WaitForSeconds(0.35f);
                l.SetActive(true);
            }
            for (int n = Cuenta; n > 0; n--)
            {
                numero.text = n.ToString();
                // salta: 40 px y baja a 32 (múltiplos de 8, nítido)
                numero.fontSize = 40;
                yield return new WaitForSeconds(0.08f);
                numero.fontSize = 32;
                yield return new WaitForSeconds(0.92f);
            }
            SceneManager.LoadScene(escenaCombate);
        }
    }
}
