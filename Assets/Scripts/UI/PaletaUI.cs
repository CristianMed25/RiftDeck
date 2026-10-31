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

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}
