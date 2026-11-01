using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RiftDeck.UI
{
    /// <summary>
    /// Escena Tienda: cosméticos con el oro del juego (sin dinero real ni anuncios).
    /// Arma una tarjeta por cosmético a partir de la plantilla, filtra por pestaña y avisa
    /// lo que pasa. El botón atrás de Android (Escape) vuelve al menú.
    /// </summary>
    public class Tienda : MonoBehaviour
    {
        [SerializeField] DatosCosmetico[] catalogo;
        [SerializeField] ItemTienda plantilla;
        [SerializeField] RectTransform contenido;
        [SerializeField] UnityEngine.UI.ScrollRect desplazamiento;
        [Tooltip("0 Todo, 1 Dorsos, 2 Tapetes, 3 Efectos")]
        [SerializeField] UnityEngine.UI.Button[] pestanas;
        [SerializeField] Sprite dorsoMuestra;
        [SerializeField] TMP_FontAsset fuenteNumeros;
        [SerializeField] TMP_FontAsset fuenteBotones;
        [SerializeField] CanvasGroup grupoAviso;
        [SerializeField] TMP_Text aviso;
        [SerializeField] UnityEngine.UI.Image destello;
        [SerializeField] Sprite[] cuadrosDestello;
        [SerializeField] string escenaMenu = "Menu";

        readonly List<ItemTienda> items = new();
        Coroutine rutinaAviso;

        public TMP_FontAsset FuenteNumeros => fuenteNumeros;
        public TMP_FontAsset FuenteBotones => fuenteBotones;

        void Awake()
        {
            plantilla.gameObject.SetActive(false);
            grupoAviso.alpha = 0;
            if (destello != null) destello.enabled = false;
            for (int i = 0; i < pestanas.Length; i++)
            {
                int indice = i;
                pestanas[i].onClick.AddListener(() => Filtrar(indice));
            }
        }

        void Start()
        {
            foreach (var c in catalogo)
            {
                var item = Instantiate(plantilla, contenido);
                item.gameObject.SetActive(true);
                item.Bind(c, this, dorsoMuestra);
                items.Add(item);
            }
            Filtrar(0);
        }

        void OnEnable() => Inventario.Cambio += RefrescarTodo;
        void OnDisable() => Inventario.Cambio -= RefrescarTodo;

        void RefrescarTodo()
        {
            foreach (var i in items) i.Refrescar();
        }

        void Filtrar(int pestana)
        {
            for (int i = 0; i < pestanas.Length; i++)
            {
                bool activa = i == pestana;
                pestanas[i].GetComponent<UnityEngine.UI.Image>().color = activa ? PaletaUI.Oro : PaletaUI.Tinta;
                pestanas[i].GetComponentInChildren<TMP_Text>().color = activa ? PaletaUI.Tinta : PaletaUI.Blanco;
            }
            foreach (var item in items)
                item.gameObject.SetActive(pestana == 0 || (int)item.Datos.categoria == pestana - 1);
            desplazamiento.horizontalNormalizedPosition = 0;
        }

        void Update()
        {
            var teclado = UnityEngine.InputSystem.Keyboard.current;
            if (teclado != null && teclado.escapeKey.wasPressedThisFrame) SceneManager.LoadScene(escenaMenu);
        }

        public void Comprado(ItemTienda item)
        {
            Avisar($"¡{item.Datos.nombre} es tuyo!", PaletaUI.Oro);
            if (destello != null && cuadrosDestello.Length > 0) StartCoroutine(Destello(item.transform));
        }

        public void Avisar(string texto, Color color)
        {
            aviso.text = texto;
            aviso.color = color;
            if (rutinaAviso != null) StopCoroutine(rutinaAviso);
            rutinaAviso = StartCoroutine(MostrarAviso());
        }

        IEnumerator MostrarAviso()
        {
            grupoAviso.alpha = 1;
            yield return new WaitForSecondsRealtime(1.4f);
            for (float t = 0; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                // se apaga en 3 pasos, como el resto de la UI
                grupoAviso.alpha = 1 - Mathf.Floor(t / 0.25f * 3) / 3f;
                yield return null;
            }
            grupoAviso.alpha = 0;
        }

        IEnumerator Destello(Transform sobre)
        {
            destello.rectTransform.position = sobre.position;
            destello.enabled = true;
            foreach (var cuadro in cuadrosDestello)
            {
                destello.sprite = cuadro;
                yield return new WaitForSecondsRealtime(1f / 24f);
            }
            destello.enabled = false;
        }
    }
}
