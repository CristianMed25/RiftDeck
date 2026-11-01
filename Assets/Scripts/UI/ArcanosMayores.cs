using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Las 22 cartas del tarot del aumento prismático "Arcanos mayores". Cada arcano
    /// pertenece a una familia: un tipo de daño o "mana".
    /// </summary>
    [CreateAssetMenu(menuName = "RiftDeck/Arcanos mayores", fileName = "Arcanos")]
    public class ArcanosMayores : ScriptableObject
    {
        public const string FamiliaMana = "mana";

        [Serializable]
        public class Arcano
        {
            public string nombre;
            [Tooltip("Id de un tipo de daño, o \"mana\"")]
            public string familia;
            public Sprite arte;
        }

        public Sprite reverso;
        public List<Arcano> cartas = new List<Arcano>();
    }
}
