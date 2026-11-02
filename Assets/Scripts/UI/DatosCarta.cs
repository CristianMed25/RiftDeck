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
        public const string Hechizo = "hechizo";
        public const string Arma = "arma";
        public const string Arsenal = "arsenal";
        public const string Poker = "poker";

        public string nombre;
        public int costo;
        public int dano;
        public string tipo;
        /// <summary>0 si es una carta normal; 1 a 22 si es un arcano mayor (índice + 1).</summary>
        public int arcano;
        /// <summary>"" si es normal; si no, de qué aumento prismático viene (ver las constantes).</summary>
        public string especial;
        /// <summary>Hechizo y arma: 1 a 6. Póker: el rango, de 2 a 14 (el as), o 0 el comodín.</summary>
        public int indice;
        /// <summary>Póker: 0 picas, 1 corazones, 2 diamantes, 3 tréboles.</summary>
        public int palo;

        public bool EsArcano => arcano > 0;
        public bool Es(string tipoEspecial) => especial == tipoEspecial;
        /// <summary>El comodín de póker: vale por la carta que mejor complete la mano.</summary>
        public bool EsComodin => Es(Poker) && indice == 0;
        /// <summary>Se queda en la mano de un turno al otro (no se reparte de nuevo).</summary>
        public bool Persistente => EsArcano || Es(Hechizo) || Es(Arma) || Es(Arsenal);

        public DatosCarta(string nombre, int costo, int dano, string tipo)
        {
            this.nombre = nombre;
            this.costo = costo;
            this.dano = dano;
            this.tipo = tipo;
            arcano = 0;
            especial = "";
            indice = 0;
            palo = 0;
        }

        /// <summary>Carta de tarot: no cuesta maná y su "tipo" es la familia del arcano.</summary>
        public static DatosCarta Arcano(int indice, string nombre, string familia) =>
            new DatosCarta(nombre, 0, 0, familia) { arcano = indice + 1 };

        public static DatosCarta Especial(string especial, int indice, string nombre, int costo, int dano, string tipo, int palo = 0) =>
            new DatosCarta(nombre, costo, dano, tipo) { especial = especial, indice = indice, palo = palo };
    }
}
