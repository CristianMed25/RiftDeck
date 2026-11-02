using UnityEngine;
using UnityEngine.SceneManagement;

namespace RiftDeck.UI
{
    /// <summary>El botón atrás de Android (Escape) carga la escena indicada.</summary>
    public class AtrasAndroid : MonoBehaviour
    {
        [SerializeField] string escena = "Menu";

        void Update()
        {
            var teclado = UnityEngine.InputSystem.Keyboard.current;
            if (teclado != null && teclado.escapeKey.wasPressedThisFrame) SceneManager.LoadScene(escena);
        }
    }
}
