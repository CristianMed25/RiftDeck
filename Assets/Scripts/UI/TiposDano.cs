using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Cómo se ve cada tipo de daño: color de la franja e ícono. Es una lista y no un
    /// enum porque los tipos todavía no están cerrados (los decide el integrante 1).
    /// Nunca se depende solo del color: siempre va ícono + color.
    /// </summary>
    [CreateAssetMenu(menuName = "RiftDeck/Tipos de daño", fileName = "TiposDano")]
    public class TiposDano : ScriptableObject
    {
        [Serializable]
        public class Tipo
        {
            public string id;
            public string nombre;
            public Color color = Color.white;
            public Sprite icono;
        }

        public List<Tipo> tipos = new List<Tipo>();

        public Tipo Buscar(string id) => tipos.Find(t => t.id == id);
    }
}
