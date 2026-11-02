using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiftDeck.UI
{
    /// <summary>
    /// Escena Configuración: sonido, vibración, calidad, velocidad de animación y los
    /// nombres de los dos jugadores. Todo se guarda al momento en <see cref="Ajustes"/>.
    /// Borrar progreso es el único botón destructivo: pide un segundo toque.
    /// </summary>
    public class PantallaConfiguracion : MonoBehaviour
    {
        const float TiempoConfirmar = 2.5f;

        [SerializeField] Slider musica;
        [SerializeField] Slider efectos;
        [SerializeField] Button vibracion;
        [SerializeField] Image fondoVibracion;
        [SerializeField] RectTransform perillaVibracion;
        [Tooltip("Baja, Media, Alta")]
        [SerializeField] Button[] calidad;
        [Tooltip("x1, x2")]
        [SerializeField] Button[] velocidad;
        [SerializeField] TMP_InputField nombreJ1;
        [SerializeField] TMP_InputField nombreJ2;
        [SerializeField] Button comoJugar;
        [SerializeField] GameObject ayuda;
        [SerializeField] Button cerrarAyuda;
        [SerializeField] Button borrar;
        [SerializeField] TMP_Text textoBorrar;
        [SerializeField] TMP_Text version;
        [SerializeField] CanvasGroup grupoAviso;
        [SerializeField] TMP_Text aviso;

        float confirmarHasta;
        Coroutine rutinaAviso;

        void Start()
        {
            musica.SetValueWithoutNotify(Ajustes.Musica);
            efectos.SetValueWithoutNotify(Ajustes.Efectos);
            musica.onValueChanged.AddListener(v => Ajustes.Musica = v);
            efectos.onValueChanged.AddListener(v => Ajustes.Efectos = v);

            vibracion.onClick.AddListener(() =>
            {
                Ajustes.Vibracion = !Ajustes.Vibracion;
                Ajustes.Vibrar();
                MostrarVibracion();
            });
            MostrarVibracion();

            for (int i = 0; i < calidad.Length; i++)
            {
                int nivel = i;
                calidad[i].onClick.AddListener(() => { Ajustes.Calidad = nivel; Marcar(calidad, nivel); });
            }
            Marcar(calidad, Ajustes.Calidad);
            for (int i = 0; i < velocidad.Length; i++)
            {
                int x = i + 1;
                velocidad[i].onClick.AddListener(() => { Ajustes.Velocidad = x; Marcar(velocidad, x - 1); });
            }
            Marcar(velocidad, Ajustes.Velocidad - 1);

            PrepararNombre(nombreJ1, 1);
            PrepararNombre(nombreJ2, 2);

            ayuda.SetActive(false);
            comoJugar.onClick.AddListener(() => ayuda.SetActive(true));
            cerrarAyuda.onClick.AddListener(() => ayuda.SetActive(false));

            borrar.onClick.AddListener(Borrar);
            version.text = $"RiftDeck v{Application.version} · Android 8.0+";
            grupoAviso.alpha = 0;
        }

        void Update()
        {
            if (confirmarHasta > 0 && Time.unscaledTime > confirmarHasta)
            {
                confirmarHasta = 0;
                textoBorrar.text = "Borrar progreso";
            }
        }

        static void PrepararNombre(TMP_InputField campo, int jugador)
        {
            campo.characterLimit = Ajustes.LargoNombre;
            campo.SetTextWithoutNotify(Ajustes.NombrePropio(jugador));
            campo.textComponent.color = PaletaUI.DeJugador(jugador);
            campo.onEndEdit.AddListener(texto =>
            {
                Ajustes.CambiarNombre(jugador, texto);
                campo.SetTextWithoutNotify(Ajustes.NombrePropio(jugador));
            });
        }

        void MostrarVibracion()
        {
            bool encendida = Ajustes.Vibracion;
            fondoVibracion.color = encendida ? PaletaUI.Menta : PaletaUI.Hueco;
            perillaVibracion.anchoredPosition = new Vector2(encendida ? 13 : 2, perillaVibracion.anchoredPosition.y);
        }

        static void Marcar(Button[] grupo, int elegido)
        {
            for (int i = 0; i < grupo.Length; i++)
            {
                bool activo = i == elegido;
                grupo[i].GetComponent<Image>().color = activo ? PaletaUI.Oro : PaletaUI.Tinta;
                grupo[i].GetComponentInChildren<TMP_Text>().color = activo ? PaletaUI.Tinta : PaletaUI.Suave;
            }
        }

        void Borrar()
        {
            if (confirmarHasta == 0)
            {
                confirmarHasta = Time.unscaledTime + TiempoConfirmar;
                textoBorrar.text = "¿Seguro? Otra vez";
                return;
            }
            confirmarHasta = 0;
            textoBorrar.text = "Borrar progreso";
            Ajustes.BorrarProgreso();
            Avisar("Listo: oro y cosméticos reiniciados");
        }

        void Avisar(string texto)
        {
            aviso.text = texto;
            if (rutinaAviso != null) StopCoroutine(rutinaAviso);
            rutinaAviso = StartCoroutine(MostrarAviso());
        }

        IEnumerator MostrarAviso()
        {
            grupoAviso.alpha = 1;
            yield return new WaitForSecondsRealtime(1.8f);
            for (float t = 0; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                // se apaga en 3 pasos, como el resto de la UI
                grupoAviso.alpha = 1 - Mathf.Floor(t / 0.25f * 3) / 3f;
                yield return null;
            }
            grupoAviso.alpha = 0;
        }
    }
}
