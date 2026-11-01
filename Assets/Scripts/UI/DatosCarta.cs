using System;

namespace RiftDeck.UI
{
    /// <summary>
    /// Lo que la UI necesita para dibujar una carta. Cuando exista el CardData del
    /// integrante 1, se convierte a esto en un solo lugar y las vistas no cambian.
    /// </summary>
    [Serializable]
    public struct DatosCarta
    {
        public string nombre;
        public int costo;
        public int dano;
        public string tipo;
        /// <summary>0 si es una carta normal; 1 a 22 si es un arcano mayor (índice + 1).</summary>
        public int arcano;

        public bool EsArcano => arcano > 0;

        public DatosCarta(string nombre, int costo, int dano, string tipo)
        {
            this.nombre = nombre;
            this.costo = costo;
            this.dano = dano;
            this.tipo = tipo;
            arcano = 0;
        }

        /// <summary>Carta de tarot: no cuesta maná y su "tipo" es la familia del arcano.</summary>
        public static DatosCarta Arcano(int indice, string nombre, string familia) =>
            new DatosCarta(nombre, 0, 0, familia) { arcano = indice + 1 };
    }
}
