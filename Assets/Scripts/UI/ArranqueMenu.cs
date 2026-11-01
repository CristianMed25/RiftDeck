using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>Enciende el Splash al abrir el juego (en la escena está apagado).</summary>
    public class ArranqueMenu : MonoBehaviour
    {
        [SerializeField] PantallaSplash splash;

        void Awake()
        {
            if (splash != null) splash.MostrarSiToca();
        }
    }
}
