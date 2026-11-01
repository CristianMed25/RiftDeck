using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Colores de identidad de la interfaz (los de los mockups). La paleta extendida
    /// para sprites vive fuera del repo, en tools/paleta.
    /// </summary>
    public static class PaletaUI
    {
        public static readonly Color CieloIndigo = Hex("#4526E8"); // fondos
        public static readonly Color Noche = Hex("#2B1C78");       // paneles
        public static readonly Color Rosa = Hex("#FF6FA8");        // sombras y alertas
        public static readonly Color Oro = Hex("#FFC83D");         // acción principal
        public static readonly Color Menta = Hex("#4FE3C1");       // éxito y energía
        public static readonly Color Tinta = Hex("#1B1033");       // contornos y texto oscuro
        public static readonly Color Blanco = Hex("#FFF8F0");

        // Apoyo de la UI (de la paleta extendida)
        public static readonly Color Azul = Hex("#4C86F2");        // botones secundarios, maná
        public static readonly Color Rojo = Hex("#E8413E");
        public static readonly Color Violeta = Hex("#8B6CFF");     // volver, menú
        public static readonly Color VioletaReserva = Hex("#B05CFF"); // maná de reserva
        public static readonly Color Crema = Hex("#FFF1D0");       // cara de las cartas
        public static readonly Color Borde = Hex("#4A37B0");       // línea interior de paneles
        public static readonly Color Hueco = Hex("#3A2A80");       // gemas y barras vacías
        public static readonly Color Suave = Hex("#C9C1FF");       // etiquetas sobre Noche
        public static readonly Color Plata = Hex("#D8DCF0");       // aumentos de plata

        // Identidad de cada jugador en toda la app
        public static readonly Color J1 = Menta;
        public static readonly Color J2 = Rosa;
        public static Color DeJugador(int n) => n == 1 ? J1 : J2;

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}
