using UnityEngine;

namespace RiftDeck.UI
{
    public enum CalidadAumento { Plata, Oro, Prismatico }

    /// <summary>
    /// Aumento al estilo de TFT: una mejora que se elige entre tres y dura toda la
    /// partida. Lo que hace lo decide quien lee <see cref="id"/> (por ahora CombateFalso).
    /// </summary>
    [CreateAssetMenu(menuName = "RiftDeck/Aumento", fileName = "Aumento")]
    public class DatosAumento : ScriptableObject
    {
        public string id;
        public string nombre;
        [TextArea] public string descripcion;
        public CalidadAumento calidad;
        public Sprite icono;
        [Tooltip("Opcional: silueta blanca que se pinta con el iridiscente detrás del ícono (el ícono va hueco)")]
        public Sprite silueta;
        [Tooltip("Opcional: mapa de normales de la silueta; si está, el iridiscente sigue el volumen (se ve 3D)")]
        public Sprite relieve;
        [Tooltip("Opcional: aura que late alrededor del ícono (los prismáticos, como en TFT)")]
        public Sprite aura;

        public static Color ColorDe(CalidadAumento calidad) => calidad switch
        {
            CalidadAumento.Plata => PaletaUI.Plata,
            CalidadAumento.Oro => PaletaUI.Oro,
            _ => Color.white, // el prismático lo pinta el material
        };

        public static string NombreDe(CalidadAumento calidad) => calidad switch
        {
            CalidadAumento.Plata => "Plata",
            CalidadAumento.Oro => "Oro",
            _ => "Prismático",
        };
    }
}
