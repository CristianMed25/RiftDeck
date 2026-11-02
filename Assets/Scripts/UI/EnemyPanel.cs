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
        // desde cuánto daño el número llega más grande y sacude más (provisional)
        const int GolpeMedio = 300;
        const int GolpeFuerte = 800;

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

        public enum Efecto { Ninguno, Bala, Explosion, ExplosionGrande, Fuego, Hielo, Rayo }

        /// <summary>Sprites de los efectos de golpe (chispa, explosión y la cruz de acierto).</summary>
        public CartasEspeciales Efectos { get; set; }

        /// <summary>El enemigo recibe una jugada de un solo golpe.</summary>
        public void RecibirGolpe(int dano, Color color, bool bloqueado)
        {
            if (bloqueado)
            {
                animacion.Reproducir("bloqueo");
                if (numeroFlotante != null) StartCoroutine(Flotar("Bloqueado", PaletaUI.Suave, 0, NumeroCentro(), DireccionAlAzar()));
                return;
            }
            Impacto(dano, color, null, Efecto.Ninguno);
        }

        /// <summary>
        /// Un golpe, solo o de una ráfaga: se sacude, parpadea y sale el número. <paramref name="donde"/>
        /// es un punto del sprite entre (-1,-1) y (1,1); null = debajo del enemigo. El número
        /// sale en diagonal hacia afuera de ese lado.
        /// </summary>
        public void Impacto(int dano, Color color, Vector2? donde, Efecto efecto)
        {
            animacion.Reproducir("golpe");
            StopCoroutine(nameof(Sacudir));
            StartCoroutine(nameof(Sacudir));
            var punto = donde.HasValue ? PuntoDelSprite(donde.Value) : NumeroCentro();
            var direccion = donde.HasValue && Mathf.Abs(donde.Value.x) > 0.1f ? new Vector2(Mathf.Sign(donde.Value.x), 0) : DireccionAlAzar();
            if (efecto != Efecto.Ninguno && Efectos != null) StartCoroutine(MostrarEfecto(punto, efecto));
            if (numeroFlotante != null) StartCoroutine(Flotar("-" + dano.ToString("N0", Colombia), color, dano, punto, direccion));
        }

        /// <summary>Un punto del cuerpo del enemigo, en coordenadas de la capa de números.</summary>
        Vector2 PuntoDelSprite(Vector2 relativo)
        {
            var capa = Capa(out _);
            // el centro del rectángulo, no el pivote (que va a los pies)
            var rt = sprite.rectTransform;
            var centro = (Vector2)capa.InverseTransformPoint(rt.TransformPoint(rt.rect.center));
            var mitad = sprite.rectTransform.rect.size / 2;
            return centro + Vector2.Scale(relativo, mitad);
        }

        Vector2 NumeroCentro() => Capa(out _).InverseTransformPoint(numeroFlotante.rectTransform.position);

        static Vector2 DireccionAlAzar() => new Vector2(Random.value < 0.5f ? -1 : 1, 0);

        /// <summary>Los números y efectos van en el Canvas, encima de la escena: así no se sacuden con ella.</summary>
        Transform Capa(out RectTransform fondo)
        {
            var areaSegura = GetComponentInParent<AreaSegura>();
            fondo = areaSegura != null ? (RectTransform)areaSegura.transform : (RectTransform)transform;
            return fondo.parent != null ? fondo.parent : fondo;
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

        /// <summary>
        /// El número sale chico del enemigo y viene hacia el jugador en diagonal, creciendo y
        /// girando un poco. Choca con la pantalla: se pasa de tamaño, destella en blanco y
        /// sacude todo lo de atrás. Después resbala y se apaga. Crece de a 8 px para que la
        /// fuente siga nítida.
        /// </summary>
        IEnumerator Flotar(string texto, Color color, int dano, Vector2 desde, Vector2 direccion)
        {
            // según el golpe: tamaño final (múltiplo de 8) y fuerza de la sacudida en px
            int final = dano >= GolpeFuerte ? 32 : dano >= GolpeMedio ? 24 : 16;
            int fuerza = dano >= GolpeFuerte ? 3 : dano >= GolpeMedio ? 2 : dano > 0 ? 1 : 0;
            bool chico = final == 16;

            var capa = Capa(out var fondo);
            var sombra = Nuevo(texto, PaletaUI.Tinta, capa);
            var numero = Nuevo(texto, color, capa);
            // hacia afuera y hacia abajo (hacia el jugador), con un poco de azar
            var hasta = desde + new Vector2(direccion.x * Random.Range(14f, 26f), -Random.Range(14f, 24f));
            float giro = -direccion.x * Random.Range(6f, 12f);

            // 1. viene hacia la pantalla, acelerando y girando
            const float llegada = 0.24f;
            for (float t = 0; t < llegada; t += Time.deltaTime)
            {
                float k = t / llegada;
                k *= k;
                int tam = Mathf.Max(8, Mathf.RoundToInt(Mathf.Lerp(8, final, k) / 8) * 8);
                Poner(numero, sombra, tam, Vector2.Lerp(desde, hasta, k), giro * k);
                yield return null;
            }

            // 2. choca: se pasa de tamaño y de giro, destella y sacude la escena
            Poner(numero, sombra, final + 8, hasta, giro * 1.4f);
            numero.color = Color.white;
            if (fuerza > 0) StartCoroutine(SacudirPantalla(fondo, fuerza));
            yield return new WaitForSeconds(0.06f);
            numero.color = color;
            Poner(numero, sombra, final, hasta, giro);
            yield return new WaitForSeconds(chico ? 0.25f : 0.4f);

            // 3. resbala por el vidrio, sigue girando y se apaga en 4 pasos
            const float salida = 0.3f;
            for (float t = 0; t < salida; t += Time.deltaTime)
            {
                float k = t / salida;
                Poner(numero, sombra, final, hasta + new Vector2(direccion.x * 4 * k, -Mathf.Round(14 * k * k)), giro * (1 + 0.5f * k));
                float alfa = 1f - Mathf.Floor(k * 4) / 4f;
                numero.alpha = sombra.alpha = alfa;
                yield return null;
            }
            Destroy(numero.gameObject);
            Destroy(sombra.gameObject);
        }

        /// <summary>
        /// La chispa, la explosión o el golpe del elemento. Las armas además llevan la cruz
        /// de acierto (como en los FPS); los hechizos no.
        /// </summary>
        IEnumerator MostrarEfecto(Vector2 punto, Efecto efecto)
        {
            var capa = Capa(out var fondo);
            var cuadros = efecto switch
            {
                Efecto.Bala => Efectos.impacto,
                Efecto.Fuego => Efectos.fuego,
                Efecto.Hielo => Efectos.hielo,
                Efecto.Rayo => Efectos.rayo,
                _ => Efectos.explosion,
            };
            int escala = efecto == Efecto.ExplosionGrande ? 2 : 1;
            if (cuadros != null && cuadros.Length > 0) StartCoroutine(Cuadros(capa, fondo, cuadros, punto, escala));
            bool arma = efecto == Efecto.Bala || efecto == Efecto.Explosion || efecto == Efecto.ExplosionGrande;
            if (!arma || Efectos.hitmarker == null) yield break;

            var marca = NuevaImagen(capa, fondo, Efectos.hitmarker, punto);
            // aparece al doble y se encoge, como el "tic" del acierto
            marca.rectTransform.localScale = Vector3.one * 2;
            yield return new WaitForSeconds(0.04f);
            marca.rectTransform.localScale = Vector3.one;
            yield return new WaitForSeconds(0.12f);
            Destroy(marca.gameObject);
        }

        IEnumerator Cuadros(Transform capa, RectTransform fondo, Sprite[] cuadros, Vector2 punto, int escala)
        {
            var img = NuevaImagen(capa, fondo, cuadros[0], punto);
            img.rectTransform.sizeDelta = cuadros[0].rect.size * escala;
            foreach (var c in cuadros)
            {
                img.sprite = c;
                yield return new WaitForSeconds(1f / 24f);
            }
            Destroy(img.gameObject);
        }

        static UnityEngine.UI.Image NuevaImagen(Transform capa, RectTransform fondo, Sprite sprite, Vector2 punto)
        {
            var go = new GameObject("Efecto", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image)) { layer = fondo.gameObject.layer };
            go.transform.SetParent(capa, false);
            // encima de todo, también de la lluvia del Arsenal y su hongo
            go.transform.SetAsLastSibling();
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = sprite.rect.size;
            img.rectTransform.localPosition = new Vector2(Mathf.Round(punto.x), Mathf.Round(punto.y));
            return img;
        }

        TMP_Text Nuevo(string texto, Color color, Transform capa)
        {
            var t = Instantiate(numeroFlotante, capa);
            t.transform.SetAsLastSibling();
            t.gameObject.SetActive(true);
            t.text = texto;
            t.color = color;
            t.raycastTarget = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.rectTransform.sizeDelta = new Vector2(400, 64);
            return t;
        }

        /// <summary>La sombra en Tinta va corrida hacia abajo: más tamaño, más relieve.</summary>
        static void Poner(TMP_Text numero, TMP_Text sombra, int tam, Vector2 pos, float giro)
        {
            numero.rectTransform.localRotation = sombra.rectTransform.localRotation = Quaternion.Euler(0, 0, giro);
            pos = new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
            int corrimiento = tam / 8;
            numero.fontSize = sombra.fontSize = tam;
            numero.rectTransform.localPosition = pos;
            sombra.rectTransform.localPosition = pos + new Vector2(corrimiento, -corrimiento);
        }

        bool sacudiendo;

        IEnumerator SacudirPantalla(RectTransform pantalla, int fuerza)
        {
            // si ya se está sacudiendo, el origen guardado sería uno corrido
            if (sacudiendo) yield break;
            sacudiendo = true;
            var origen = pantalla.anchoredPosition;
            for (int f = fuerza; f > 0; f--)
            {
                pantalla.anchoredPosition = origen + new Vector2(Random.Range(0, 2) * 2 - 1, Random.Range(0, 2) * 2 - 1) * f;
                yield return new WaitForSeconds(0.035f);
                pantalla.anchoredPosition = origen + new Vector2(Random.Range(-1, 2), Random.Range(-1, 2)) * (f / 2);
                yield return new WaitForSeconds(0.035f);
            }
            pantalla.anchoredPosition = origen;
            sacudiendo = false;
        }
    }
}
