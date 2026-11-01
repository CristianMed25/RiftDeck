using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Enemigo al centro: nombre, nivel, botón "?" con las resistencias, sprite animado
    /// y barra de vida. También hace las reacciones: sacudida y número al recibir daño,
    /// bloqueo, ataque, muerte y entrada del siguiente enemigo.
    /// </summary>
    public class EnemyPanel : MonoBehaviour
    {
        static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");
        static readonly Color ColorGolpe = new Color(1f, 0.55f, 0.6f);

        [SerializeField] TMP_Text nombre;
        [SerializeField] TMP_Text nivel;
        [SerializeField] TMP_Text estado;
        [SerializeField] UnityEngine.UI.Image sprite;
        [SerializeField] RectTransform sombra;
        [SerializeField] AnimacionSprites animacion;
        [SerializeField] UnityEngine.UI.Button botonResistencias;
        [SerializeField] GameObject panelResistencias;
        [Tooltip("Celdas con hijos Icono, Valor y Texto, en el orden de TiposDano")]
        [SerializeField] Transform rejillaResistencias;
        [SerializeField] UnityEngine.UI.Image barraVida;
        [SerializeField] TMP_Text textoVida;
        [Tooltip("Texto apagado que se clona para cada número de daño")]
        [SerializeField] TMP_Text numeroFlotante;

        Vector2 posicionSprite;

        void Awake()
        {
            panelResistencias.SetActive(false);
            botonResistencias.onClick.AddListener(() => panelResistencias.SetActive(!panelResistencias.activeSelf));
            posicionSprite = sprite.rectTransform.anchoredPosition;
            if (numeroFlotante != null) numeroFlotante.gameObject.SetActive(false);
            MostrarEscudo(false);
        }

        public void Mostrar(DatosEnemigo datos, TiposDano tipos)
        {
            nombre.text = datos.nombre;
            (nivel.text, nivel.color) = datos.nivel switch
            {
                NivelEnemigo.Normal => ("Normal", PaletaUI.Menta),
                NivelEnemigo.Medio => ("Medio", PaletaUI.Oro),
                _ => ("Élite", PaletaUI.Rosa),
            };

            animacion.Cargar(datos.clips);
            var primero = datos.clips[0].cuadros[0];
            var tam = primero.rect.size * datos.escala;
            sprite.rectTransform.sizeDelta = tam;
            sprite.rectTransform.anchoredPosition = posicionSprite;
            sprite.color = Color.white;
            if (sombra != null) sombra.sizeDelta = new Vector2(Mathf.Round(tam.x * 0.6f), sombra.sizeDelta.y);

            if (rejillaResistencias != null && tipos != null)
            {
                for (int i = 0; i < rejillaResistencias.childCount; i++)
                {
                    var celda = rejillaResistencias.GetChild(i);
                    bool hay = i < tipos.tipos.Count;
                    celda.gameObject.SetActive(hay);
                    if (hay) Celda(celda, tipos.tipos[i], datos.Multiplicador(tipos.tipos[i].id));
                }
            }
            MostrarEscudo(false);
        }

        static void Celda(Transform celda, TiposDano.Tipo tipo, float m)
        {
            var color = m > 1f ? PaletaUI.Menta : m < 1f ? PaletaUI.Rosa : PaletaUI.Suave;
            celda.Find("Icono").GetComponent<UnityEngine.UI.Image>().sprite = tipo.icono;
            var valor = celda.Find("Valor").GetComponent<TMP_Text>();
            valor.text = "x" + m.ToString("0.##", CultureInfo.InvariantCulture);
            valor.color = m == 1f ? Color.white : color;
            var texto = celda.Find("Texto").GetComponent<TMP_Text>();
            texto.text = m > 1f ? "Débil" : m < 1f ? "Resiste" : "Normal";
            texto.color = color;
        }

        public void MostrarVida(int actual, int maxima)
        {
            barraVida.fillAmount = Mathf.Clamp01((float)actual / maxima);
            textoVida.text = Mathf.Max(0, actual).ToString("N0", Colombia) + "/" + maxima.ToString("N0", Colombia);
        }

        /// <summary>Aviso de que el enemigo anulará el daño del turno.</summary>
        public void MostrarEscudo(bool activo)
        {
            if (estado != null) estado.gameObject.SetActive(activo);
        }

        /// <summary>El enemigo recibe una jugada: se sacude, parpadea y sale el número.</summary>
        public void RecibirGolpe(int dano, Color color, bool bloqueado)
        {
            if (bloqueado)
            {
                animacion.Reproducir("bloqueo");
                if (numeroFlotante != null) StartCoroutine(Flotar("Bloqueado", PaletaUI.Suave));
                return;
            }
            animacion.Reproducir("golpe");
            StopCoroutine(nameof(Sacudir));
            StartCoroutine(nameof(Sacudir));
            if (numeroFlotante != null) StartCoroutine(Flotar("-" + dano.ToString("N0", Colombia), color));
        }

        /// <summary>Turno del enemigo: reproduce la animación de la acción.</summary>
        public void Actuar(string accion) => animacion.Reproducir(accion);

        public IEnumerator Morir()
        {
            animacion.Reproducir("muerte");
            yield return new WaitForSeconds(animacion.Duracion("muerte") + 0.3f);
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                sprite.color = new Color(1, 1, 1, 1f - t / 0.3f);
                yield return null;
            }
            sprite.color = new Color(1, 1, 1, 0);
        }

        /// <summary>El enemigo nuevo cae un poco desde arriba mientras aparece.</summary>
        public IEnumerator Entrar()
        {
            const float duracion = 0.35f;
            for (float t = 0; t < duracion; t += Time.deltaTime)
            {
                float k = t / duracion;
                sprite.color = new Color(1, 1, 1, k);
                sprite.rectTransform.anchoredPosition = posicionSprite + new Vector2(0, Mathf.Round(16 * (1 - k) * (1 - k)));
                yield return null;
            }
            sprite.color = Color.white;
            sprite.rectTransform.anchoredPosition = posicionSprite;
        }

        IEnumerator Sacudir()
        {
            var rt = sprite.rectTransform;
            int[] pasos = { 4, -4, 3, -3, 2, -1, 0 };
            foreach (int dx in pasos)
            {
                rt.anchoredPosition = posicionSprite + new Vector2(dx, 0);
                sprite.color = dx != 0 && dx % 2 == 0 ? ColorGolpe : Color.white;
                yield return new WaitForSeconds(0.04f);
            }
            rt.anchoredPosition = posicionSprite;
            sprite.color = Color.white;
        }

        IEnumerator Flotar(string texto, Color color)
        {
            var numero = Instantiate(numeroFlotante, numeroFlotante.transform.parent);
            numero.gameObject.SetActive(true);
            numero.text = texto;
            numero.color = color;
            var rt = numero.rectTransform;
            var inicio = rt.anchoredPosition;
            const float duracion = 0.9f;
            for (float t = 0; t < duracion; t += Time.deltaTime)
            {
                float k = t / duracion;
                rt.anchoredPosition = inicio + new Vector2(0, Mathf.Round(28 * (1 - (1 - k) * (1 - k))));
                numero.alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                yield return null;
            }
            Destroy(numero.gameObject);
        }
    }
}
