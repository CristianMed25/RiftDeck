using UnityEngine;
using UnityEngine.SceneManagement;

namespace RiftDeck.UI
{
    /// <summary>
    /// Navegación provisional: el botón carga la escena indicada. Cuando el integrante 3
    /// tenga su controlador de partida, este componente se cambia por el suyo.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class IrAEscena : MonoBehaviour
    {
        [SerializeField] string escena;

        void Awake()
        {
            GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => SceneManager.LoadScene(escena));
        }
    }
}
