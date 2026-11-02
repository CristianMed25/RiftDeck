using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Arte y reglas de las cartas que dan los aumentos prismáticos: hechizos prohibidos,
    /// armas y Arsenal de RAAWRMERICA y la baraja de póker. El arte lo arma
    /// tools/cartas_especiales.py con marcos de Deckbuilder Kit Free y Pixel Cards Lite.
    /// </summary>
    [CreateAssetMenu(menuName = "RiftDeck/Cartas especiales", fileName = "CartasEspeciales")]
    public class CartasEspeciales : ScriptableObject
    {
        public struct Hechizo
        {
            public string nombre;
            public int costo, dano;
            public string[] elementos;
        }

        public struct Arma
        {
            public string nombre, tipo;
            public int costo, dano;
            /// <summary>En cuántos golpes se reparte el daño (la cadencia del arma).</summary>
            public int disparos;
            public EnemyPanel.Efecto efecto;
        }

        /// <summary>Todos cuentan como daño elemental; los elementos solo sirven para los combos.</summary>
        public static readonly Hechizo[] Hechizos =
        {
            new Hechizo { nombre = "Ascua", costo = 2, dano = 9, elementos = new[] { "fuego" } },
            new Hechizo { nombre = "Escarcha", costo = 2, dano = 9, elementos = new[] { "hielo" } },
            new Hechizo { nombre = "Chispazo", costo = 2, dano = 9, elementos = new[] { "rayo" } },
            new Hechizo { nombre = "Tormenta", costo = 3, dano = 14, elementos = new[] { "hielo", "rayo" } },
            new Hechizo { nombre = "Supernova", costo = 4, dano = 20, elementos = new[] { "fuego", "rayo" } },
            new Hechizo { nombre = "Cataclismo", costo = 5, dano = 30, elementos = new[] { "fuego", "hielo", "rayo" } },
        };

        public static readonly Arma[] Armas =
        {
            new Arma { nombre = "Pistola", tipo = "perforante", costo = 1, dano = 5, disparos = 4, efecto = EnemyPanel.Efecto.Bala },
            new Arma { nombre = "Metralleta", tipo = "perforante", costo = 2, dano = 8, disparos = 8, efecto = EnemyPanel.Efecto.Bala },
            new Arma { nombre = "Rifle", tipo = "perforante", costo = 3, dano = 11, disparos = 3, efecto = EnemyPanel.Efecto.Bala },
            new Arma { nombre = "Francotirador", tipo = "perforante", costo = 4, dano = 15, disparos = 1, efecto = EnemyPanel.Efecto.Bala },
            new Arma { nombre = "Bazuca", tipo = "impacto", costo = 5, dano = 18, disparos = 1, efecto = EnemyPanel.Efecto.Explosion },
            new Arma { nombre = "Misil", tipo = "impacto", costo = 6, dano = 22, disparos = 1, efecto = EnemyPanel.Efecto.ExplosionGrande },
        };

        /// <summary>Picas, corazones, diamantes y tréboles: cada palo es un tipo de daño.</summary>
        public static readonly string[] Palos = { "spades", "hearts", "diamonds", "clubs" };
        public static readonly string[] NombresPalo = { "picas", "corazones", "diamantes", "tréboles" };
        public static readonly string[] TipoDePalo = { "cortante", "elemental", "perforante", "impacto" };

        [Header("Hechizos prohibidos (el marco va con el material prismático)")]
        public Sprite marcoHechizo;
        public Sprite[] hechizos = new Sprite[6];
        [Header("RAAWRMERICA")]
        public Sprite[] armas = new Sprite[6];
        public Sprite marcoArsenal;
        public Sprite contenidoArsenal;
        [Header("Póker: palo × 13 + (rango − 2), en el orden de Palos")]
        public Sprite[] poker = new Sprite[52];
        [Tooltip("El comodín: el papel va con el material prismático y el dibujo encima")]
        public Sprite marcoComodin;
        public Sprite contenidoComodin;
        [Header("Golpes")]
        [Tooltip("Cruz de acierto, como en los FPS")]
        public Sprite hitmarker;
        [Header("Lluvia del Arsenal")]
        public Sprite bala;
        public Sprite misil;
        public Sprite bomba;
        public Sprite[] impacto;
        public Sprite[] explosion;
        public Sprite[] nuclear;
        [Tooltip("Hongo nuclear propio (tools/hongo_nuclear.py), con la base abajo al centro")]
        public Sprite[] hongo;
        [Header("Golpes de hechizo, por elemento")]
        public Sprite[] fuego;
        public Sprite[] hielo;
        public Sprite[] rayo;

        public Sprite Poker(int rango, int palo) => poker[palo * 13 + rango - 2];

        public static string NombreRango(int rango) => rango switch
        {
            11 => "J",
            12 => "Q",
            13 => "K",
            14 => "A",
            _ => rango.ToString(),
        };

        /// <summary>Fichas que suma la carta en una mano de póker (como en Balatro).</summary>
        public static int Fichas(int rango) => rango == 14 ? 11 : Mathf.Min(rango, 10);
    }
}
