using TMPro;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Tarjeta de un cosmético en la Tienda: vista previa, nombre y un botón que cambia según
    /// el estado (precio → ¿COMPRAR? → EQUIPAR → EQUIPADO). Comprar pide un segundo toque
    /// para que no se gaste oro sin querer.
    /// </summary>
    public class ItemTienda : MonoBehaviour
    {
        const float TiempoConfirmar = 2.5f;
        const float FpsEfecto = 12f;

        [SerializeField] UnityEngine.UI.Image vista;
        [SerializeField] UnityEngine.UI.Image muestra;
        [SerializeField] TMP_Text nombre;
        [SerializeField] UnityEngine.UI.Button boton;
        [SerializeField] UnityEngine.UI.Image fondoBoton;
        [SerializeField] UnityEngine.UI.Image moneda;
        [SerializeField] TMP_Text textoBoton;
        [SerializeField] UnityEngine.UI.Image marco;

        Tienda tienda;
        float confirmar;
        float tiempo;
        int escalaMuestra = 1;
        float sacudida;
        Vector2 posicionBoton;

        public DatosCosmetico Datos { get; private set; }

        void Awake()
        {
            boton.onClick.AddListener(Tocar);
            posicionBoton = boton.GetComponent<RectTransform>().anchoredPosition;
        }

        public void Bind(DatosCosmetico datos, Tienda dueña, Sprite dorsoMuestra)
        {
            Datos = datos;
            tienda = dueña;
            name = "Item_" + datos.id;
            nombre.text = datos.nombre;

            // la vista previa depende de la categoría
            if (datos.categoria == CategoriaCosmetico.Tapete)
            {
                vista.sprite = datos.vista;
                vista.type = UnityEngine.UI.Image.Type.Tiled;
                vista.color = Color.white;
                Muestra(dorsoMuestra, 1); // una carta sobre el tapete
            }
            else
            {
                vista.sprite = null;
                vista.color = PaletaUI.Tinta;
                var s = datos.vista;
                escalaMuestra = datos.categoria == CategoriaCosmetico.Efecto
                    ? Mathf.Max(1, Mathf.FloorToInt(96f / Mathf.Max(s.rect.width, s.rect.height)))
                    : 1;
                Muestra(s, escalaMuestra);
            }
            Refrescar();
        }

        void Muestra(Sprite sprite, int escala)
        {
            muestra.sprite = sprite;
            muestra.enabled = sprite != null;
            if (sprite != null) muestra.rectTransform.sizeDelta = sprite.rect.size * escala;
        }

        public void Refrescar()
        {
            if (Datos == null) return;
            bool equipado = Inventario.EstaEquipado(Datos);
            bool tiene = Inventario.Tiene(Datos);
            bool alcanza = Inventario.Oro >= Datos.precio;
            marco.enabled = equipado;
            marco.color = PaletaUI.Menta;

            bool conPrecio = false;
            if (equipado) Estado("Equipado", PaletaUI.Menta, PaletaUI.Tinta);
            else if (tiene) Estado("Equipar", PaletaUI.Azul, PaletaUI.Blanco);
            else if (confirmar > 0) Estado("¿Comprar?", PaletaUI.Rosa, PaletaUI.Tinta);
            else
            {
                conPrecio = true;
                Estado(Datos.precio.ToString(), alcanza ? PaletaUI.Oro : PaletaUI.Hueco, alcanza ? PaletaUI.Tinta : PaletaUI.Suave);
            }

            // el precio va en la fuente del marcador y con la moneda a la izquierda
            textoBoton.font = conPrecio ? tienda.FuenteNumeros : tienda.FuenteBotones;
            moneda.enabled = conPrecio;
            textoBoton.rectTransform.anchoredPosition = new Vector2(conPrecio ? 6 : 0, textoBoton.rectTransform.anchoredPosition.y);
            if (conPrecio)
            {
                float ancho = Mathf.Ceil(textoBoton.GetPreferredValues(textoBoton.text).x);
                moneda.rectTransform.anchoredPosition = new Vector2(Mathf.Round(-ancho / 2f - 1), 0);
            }
        }

        void Estado(string texto, Color fondo, Color letra)
        {
            textoBoton.text = texto.ToUpperInvariant();
            textoBoton.color = letra;
            fondoBoton.color = fondo;
        }

        void Tocar()
        {
            if (Inventario.EstaEquipado(Datos)) return;
            if (Inventario.Tiene(Datos))
            {
                Inventario.Equipar(Datos);
                tienda.Avisar($"{Datos.nombre} equipado", PaletaUI.Menta);
                return;
            }
            if (Inventario.Oro < Datos.precio)
            {
                sacudida = 0.3f;
                tienda.Avisar($"Te faltan {Datos.precio - Inventario.Oro} de oro", PaletaUI.Rosa);
                return;
            }
            if (confirmar <= 0)
            {
                confirmar = TiempoConfirmar;
                Refrescar();
                return;
            }
            confirmar = 0;
            Inventario.Comprar(Datos);
            Inventario.Equipar(Datos);
            tienda.Comprado(this);
        }

        void Update()
        {
            if (confirmar > 0)
            {
                confirmar -= Time.unscaledDeltaTime;
                if (confirmar <= 0) Refrescar();
            }

            if (sacudida > 0)
            {
                sacudida -= Time.unscaledDeltaTime;
                // va y viene de a 2 px
                float x = sacudida > 0 ? (Mathf.FloorToInt(sacudida * 30) % 2 == 0 ? 2 : -2) : 0;
                boton.GetComponent<RectTransform>().anchoredPosition = posicionBoton + new Vector2(x, 0);
            }

            var cuadros = Datos != null ? Datos.cuadros : null;
            if (cuadros != null && cuadros.Length > 1)
            {
                // la animación del efecto; las explosiones largas llevan una pausa al final para que
                // no mareen, los ciclos cortos (chispas) se repiten sin parar
                tiempo += Time.unscaledDeltaTime;
                int total = cuadros.Length + (cuadros.Length > 6 ? Mathf.RoundToInt(FpsEfecto * 0.6f) : 0);
                int i = Mathf.FloorToInt(tiempo * FpsEfecto) % total;
                muestra.enabled = i < cuadros.Length;
                if (i < cuadros.Length) muestra.sprite = cuadros[i];
            }
        }
    }
}
