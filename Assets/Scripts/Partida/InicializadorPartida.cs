using UnityEngine;

namespace RiftDeck.Partida
{
    /// <summary>
    /// Arranca una partida nueva al cargar la escena Combate: crea el
    /// <see cref="GestorPartida"/>, lo inicia y lo reparte a <see cref="ControladorTurnos"/>
    /// y <see cref="ControladorFinPartida"/>. No conoce combate ni interfaz: solo conecta
    /// las piezas de gestión de partida entre sí.
    /// </summary>
    public class InicializadorPartida : MonoBehaviour
    {
        [SerializeField] ControladorTurnos controladorTurnos;
        [SerializeField] ControladorFinPartida controladorFinPartida;

        GestorPartida partida;

        /// <summary>El GestorPartida de esta partida, para que el combate lo use durante la escena.</summary>
        public GestorPartida Partida => partida;

        void Awake()
        {
            if (!ReferenciasValidas()) return;

            partida = new GestorPartida();
            partida.IniciarPartida();

            controladorTurnos.Inicializar(partida);
            controladorFinPartida.Inicializar(partida);
        }

        bool ReferenciasValidas()
        {
            bool ok = true;
            if (controladorTurnos == null)
            {
                UnityEngine.Debug.LogError("InicializadorPartida: falta asignar ControladorTurnos en el Inspector.");
                ok = false;
            }
            if (controladorFinPartida == null)
            {
                UnityEngine.Debug.LogError("InicializadorPartida: falta asignar ControladorFinPartida en el Inspector.");
                ok = false;
            }
            return ok;
        }
    }
}
