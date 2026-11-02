using UnityEngine;
using UnityEngine.SceneManagement;

namespace RiftDeck.Partida
{
    /// <summary>
    /// Cuando el <see cref="GestorPartida"/> marca la partida como terminada, traspasa las
    /// puntuaciones por ronda a <see cref="RiftDeck.UI.ResultadoPartida"/> (del Integrante 2)
    /// y carga la escena de resultados. No decide cuándo termina una ronda ni llama a
    /// <see cref="GestorPartida.AvanzarRonda"/>: eso es responsabilidad de quien gestione el
    /// combate. Este componente solo reacciona cuando <see cref="GestorPartida.PartidaTerminada"/>
    /// ya es true.
    /// </summary>
    public class ControladorFinPartida : MonoBehaviour
    {
        [SerializeField] string escenaResultados = "Resultados";

        GestorPartida partida;
        bool finalizada;

        /// <summary>
        /// Asigna el <see cref="GestorPartida"/> de la partida en curso. Igual que en
        /// <see cref="ControladorTurnos"/>, se hace por código porque GestorPartida no es
        /// un UnityEngine.Object y no se puede enganchar desde el Inspector.
        /// </summary>
        public void Inicializar(GestorPartida partida) => this.partida = partida;

        /// <summary>
        /// Guarda las puntuaciones en <see cref="RiftDeck.UI.ResultadoPartida"/> y carga la
        /// escena de resultados. No hace nada si la partida no está realmente terminada o si
        /// ya se finalizó antes para esta misma instancia.
        /// </summary>
        public void FinalizarPartida()
        {
            if (!PuedeFinalizar()) return;
            finalizada = true;

            var puntosJ1 = partida.ObtenerPuntosPorRonda(1);
            var puntosJ2 = partida.ObtenerPuntosPorRonda(2);
            // ResultadoPartida.Guardar ya clona los arrays y resetea OroCobrado; PantallaResultados
            // se encarga sola de cobrar el oro al arrancar la escena, aquí no se toca.
            RiftDeck.UI.ResultadoPartida.Guardar(puntosJ1, puntosJ2);

            SceneManager.LoadScene(escenaResultados);
        }

        bool PuedeFinalizar()
        {
            if (finalizada)
            {
                UnityEngine.Debug.LogWarning("ControladorFinPartida: la partida ya se finalizó; se ignora la llamada.");
                return false;
            }
            if (partida == null)
            {
                UnityEngine.Debug.LogError("ControladorFinPartida: falta asignar el GestorPartida (llama Inicializar).");
                return false;
            }
            if (!partida.PartidaIniciada)
            {
                UnityEngine.Debug.LogWarning("ControladorFinPartida: la partida no se ha iniciado.");
                return false;
            }
            if (!partida.PartidaTerminada)
            {
                UnityEngine.Debug.LogWarning("ControladorFinPartida: la partida todavía no ha terminado.");
                return false;
            }
            return true;
        }
    }
}
