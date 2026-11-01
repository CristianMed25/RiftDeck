using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Idle de "respiración" para sprites sin animación propia: estira la escala Y
    /// un poco. Conviene que el pivote esté en los pies.
    /// </summary>
    public class Respirar : MonoBehaviour
    {
        [SerializeField] float amplitud = 0.03f;
        [SerializeField] float periodo = 1.6f;

        void Update()
        {
            float s = Mathf.Sin(Time.time * Mathf.PI * 2f / periodo);
            transform.localScale = new Vector3(1f - s * amplitud * 0.5f, 1f + s * amplitud, 1f);
        }
    }
}
