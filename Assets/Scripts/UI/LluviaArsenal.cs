using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RiftDeck.UI
{
    /// <summary>
    /// Lo que pasa al disparar el Arsenal de RAAWRMERICA: caen balas, misiles o una bomba
    /// nuclear sobre el enemigo, cada uno con su explosión de Super Pixel Effects. Crea las
    /// imágenes al vuelo dentro de su propio RectTransform (que tapa toda la pantalla).
    /// </summary>
    public class LluviaArsenal : MonoBehaviour
    {
        public enum Nivel { Balas, Misiles, Nuclear }

        const float FpsEfecto = 24f;
        const float FpsHongo = 12f;

        [SerializeField] CartasEspeciales cartas;
        [Tooltip("Hacia dónde cae todo (el sprite del enemigo)")]
        [SerializeField] RectTransform objetivo;
        [Tooltip("Se sacude con la explosión nuclear")]
        [SerializeField] RectTransform sacudir;
        [SerializeField] Image destello;

        RectTransform Zona => (RectTransform)transform;

        void Awake()
        {
            if (destello != null) destello.enabled = false;
        }

        /// <summary>
        /// La bomba nuclear es el premio por juntar las 6 armas distintas; si no, con 7 o más
        /// caen misiles y con menos, balas.
        /// </summary>
        public static Nivel NivelDe(IReadOnlyCollection<int> armas) =>
            armas.Distinct().Count() >= CartasEspeciales.Armas.Length ? Nivel.Nuclear
            : armas.Count >= 7 ? Nivel.Misiles : Nivel.Balas;

        public static string NombreDe(Nivel n) => n switch
        {
            Nivel.Nuclear => "¡BOMBA NUCLEAR!",
            Nivel.Misiles => "¡LLUVIA DE MISILES!",
            _ => "¡LLUVIA DE BALAS!",
        };

        public IEnumerator Lanzar(Nivel nivel, int armas)
        {
            var centro = (Vector2)Zona.InverseTransformPoint(objetivo.position);
            if (nivel == Nivel.Nuclear)
            {
                yield return Caer(cartas.bomba, centro, 1.1f, null);
                yield return Nuclear(centro);
                yield break;
            }
            bool balas = nivel == Nivel.Balas;
            int cuantas = balas ? armas * 4 : armas * 2;
            float espera = balas ? 0.05f : 0.12f;
            float duracion = balas ? 0.35f : 0.6f;
            int vivas = 0;
            for (int i = 0; i < cuantas; i++)
            {
                var destino = centro + new Vector2(Random.Range(-40f, 40f), Random.Range(-18f, 24f));
                vivas++;
                StartCoroutine(Contar(Caer(balas ? cartas.bala : cartas.misil, destino, duracion,
                    balas ? cartas.impacto : cartas.explosion), () => vivas--));
                yield return new WaitForSeconds(espera);
            }
            while (vivas > 0) yield return null;
        }

        static IEnumerator Contar(IEnumerator rutina, System.Action alTerminar)
        {
            yield return rutina;
            alTerminar();
        }

        /// <summary>El proyectil cae en línea recta desde arriba y explota al llegar.</summary>
        IEnumerator Caer(Sprite sprite, Vector2 destino, float duracion, Sprite[] efecto)
        {
            var img = Nueva(sprite);
            float alto = Zona.rect.height;
            var desde = new Vector2(destino.x + Random.Range(-30f, 30f), alto / 2 + 20);
            for (float t = 0; t < duracion; t += Time.deltaTime)
            {
                float k = t / duracion;
                img.rectTransform.anchoredPosition = Redondear(Vector2.Lerp(desde, destino, k * k));
                yield return null;
            }
            Destroy(img.gameObject);
            if (cartas.hitmarker != null) StartCoroutine(Marca(destino));
            if (efecto != null && efecto.Length > 0) yield return Efecto(efecto, destino, 1);
        }

        IEnumerator Nuclear(Vector2 centro)
        {
            // el hongo sube desde los pies del enemigo, a x2 y más lento que las explosiones
            if (cartas.hongo != null && cartas.hongo.Length > 0)
            {
                var r = objetivo.rect;
                var pies = (Vector2)Zona.InverseTransformPoint(objetivo.TransformPoint(new Vector2(r.center.x, r.yMin)));
                // si el sombrero no cabe arriba, la base baja lo que haga falta
                float alto = cartas.hongo[0].rect.height * 2 * 0.95f;
                pies.y = Mathf.Min(pies.y, Zona.rect.yMax - 4 - alto);
                StartCoroutine(Efecto(cartas.hongo, pies, 2, new Vector2(0.5f, 0.05f), FpsHongo));
            }
            if (destello != null)
            {
                destello.enabled = true;
                destello.color = Color.white;
            }
            var efecto = StartCoroutine(Efecto(cartas.nuclear, centro, 2));
            var origen = sacudir != null ? sacudir.anchoredPosition : Vector2.zero;
            for (float t = 0; t < 1.2f; t += Time.deltaTime)
            {
                // el blanco se apaga en 4 pasos y la pantalla tiembla de a 2 px
                if (destello != null) destello.color = new Color(1, 1, 1, 1f - Mathf.Floor(t / 0.9f * 4) / 4f);
                if (sacudir != null)
                    sacudir.anchoredPosition = origen + new Vector2(Random.Range(-1, 2), Random.Range(-1, 2)) * 2 * (1 - t / 1.2f);
                yield return null;
            }
            if (sacudir != null) sacudir.anchoredPosition = origen;
            if (destello != null) destello.enabled = false;
            yield return efecto;
        }

        /// <summary>La cruz de acierto en cada impacto: sale al doble y se encoge.</summary>
        IEnumerator Marca(Vector2 donde)
        {
            var img = Nueva(cartas.hitmarker);
            img.rectTransform.anchoredPosition = Redondear(donde);
            img.rectTransform.localScale = Vector3.one * 2;
            yield return new WaitForSeconds(0.04f);
            img.rectTransform.localScale = Vector3.one;
            yield return new WaitForSeconds(0.1f);
            Destroy(img.gameObject);
        }

        IEnumerator Efecto(Sprite[] cuadros, Vector2 donde, int escala) =>
            Efecto(cuadros, donde, escala, new Vector2(0.5f, 0.5f), FpsEfecto);

        IEnumerator Efecto(Sprite[] cuadros, Vector2 donde, int escala, Vector2 pivote, float fps)
        {
            var img = Nueva(cuadros[0]);
            img.rectTransform.pivot = pivote;
            img.rectTransform.anchoredPosition = Redondear(donde);
            img.rectTransform.sizeDelta = cuadros[0].rect.size * escala;
            foreach (var c in cuadros)
            {
                img.sprite = c;
                yield return new WaitForSeconds(1f / fps);
            }
            Destroy(img.gameObject);
        }

        Image Nueva(Sprite sprite)
        {
            var go = new GameObject("Proyectil", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)) { layer = gameObject.layer };
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = sprite.rect.size;
            return img;
        }

        static Vector2 Redondear(Vector2 v) => new Vector2(Mathf.Round(v.x), Mathf.Round(v.y));
    }
}
