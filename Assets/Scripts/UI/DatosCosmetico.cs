using UnityEngine;

namespace RiftDeck.UI
{
    public enum CategoriaCosmetico { Dorso, Tapete, Efecto }

    /// <summary>
    /// Un objeto de la Tienda. Solo cambia cómo se ve el juego: nada de esto da ventaja.
    /// Los de precio 0 son los de serie (se tienen desde el principio).
    /// </summary>
    [CreateAssetMenu(menuName = "RiftDeck/Cosmético", fileName = "Cosmetico")]
    public class DatosCosmetico : ScriptableObject
    {
        public string id;
        public string nombre;
        public CategoriaCosmetico categoria;
        public int precio;
        [Tooltip("Dorso: la carta. Tapete: la baldosa que se repite. Efecto: el primer cuadro")]
        public Sprite vista;
        [Tooltip("Solo efectos: los cuadros de la animación")]
        public Sprite[] cuadros;

        public bool DeSerie => precio <= 0;

        public static string NombreDe(CategoriaCosmetico categoria) => categoria switch
        {
            CategoriaCosmetico.Dorso => "Dorsos",
            CategoriaCosmetico.Tapete => "Tapetes",
            _ => "Efectos",
        };
    }
}
