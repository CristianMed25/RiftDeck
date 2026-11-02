using UnityEngine;
using UnityEngine.SceneManagement;

namespace RiftDeck.UI
{
    /// <summary>
    /// Escena Modo (pantalla 4 del mockup, local): Duelo de 4 rondas o Partida rápida de 1.
    /// La elegida lleva marco Oro y la etiqueta ELEGIDO; el botón va a la preparación.
    /// </summary>
    public class PantallaModo : MonoBehaviour
    {
        [System.Serializable]
        public class Opcion
        {
            public UnityEngine.UI.Button boton;
            public GameObject marco;
            public GameObject elegido;
            public int rondas;
        }

        [SerializeField] Opcion[] opciones;
        [SerializeField] UnityEngine.UI.Button continuar;
        [SerializeField] TMPro.TMP_Text textoContinuar;
        [SerializeField] string escenaPreparacion = "Lobby";

        void Start()
        {
            foreach (var o in opciones)
            {
                var esta = o;
                o.boton.onClick.AddListener(() => Elegir(esta.rondas));
            }
            continuar.onClick.AddListener(() => SceneManager.LoadScene(escenaPreparacion));
            Elegir(ModoPartida.Rondas);
        }

        void Elegir(int rondas)
        {
            ModoPartida.Rondas = rondas;
            foreach (var o in opciones)
            {
                bool si = o.rondas == rondas;
                o.marco.SetActive(si);
                o.elegido.SetActive(si);
            }
            textoContinuar.text = ModoPartida.Rapida ? "Jugar ya" : "Preparar duelo";
        }
    }
}
