using UnityEngine;
using RiftDeck.Combate;

namespace RiftDeck.UI
{
    /// <summary>
    /// Adaptador para prefabs nuevos. Los prefabs existentes usan CardView directamente;
    /// ambos publican la misma CartaSO a través de ManoCartas.PidioElegir.
    /// </summary>
    [RequireComponent(typeof(CardView))]
    public class CartaUI : MonoBehaviour
    {
        [SerializeField] CartaSO carta;
        [SerializeField] TiposDano tipos;

        void Awake()
        {
            if (carta == null)
            {
                Debug.LogError("CartaUI: falta asignar la CartaSO en el Inspector.", this);
                return;
            }
            GetComponent<CardView>().BindCarta(carta, tipos);
        }
    }
}
