using System.Collections.Generic;
using System.Linq;

namespace RiftDeck.UI
{
    /// <summary>
    /// Reconoce la mejor mano de póker entre 1 y 5 cartas y dice cuánto vale, al estilo de
    /// Balatro: (fichas de la mano + fichas de las cartas que puntúan) × multiplicador.
    /// El comodín vale por la carta que deje la mano más alta.
    /// </summary>
    public static class ManoPoker
    {
        public struct Resultado
        {
            public string nombre;
            public int fichasBase, mult;
            /// <summary>Índices (en la lista recibida) de las cartas que puntúan.</summary>
            public List<int> puntuan;
            /// <summary>Las cartas como quedaron: el comodín ya convertido en la que le sirve.</summary>
            public IReadOnlyList<DatosCarta> como;

            public int Valor
            {
                get
                {
                    int fichas = fichasBase;
                    foreach (int i in puntuan) fichas += CartasEspeciales.Fichas(como[i].indice);
                    return fichas * mult;
                }
            }
        }

        public static Resultado Evaluar(IReadOnlyList<DatosCarta> cartas)
        {
            int k = -1;
            for (int i = 0; i < cartas.Count && k < 0; i++)
                if (cartas[i].EsComodin) k = i;
            if (k < 0) return EvaluarFijas(cartas);
            // el comodín prueba cada carta que no esté ya en la mano y se queda con la mejor
            Resultado mejor = default;
            bool hay = false;
            for (int palo = 0; palo < 4; palo++)
                for (int rango = 2; rango <= 14; rango++)
                {
                    if (cartas.Any(c => !c.EsComodin && c.indice == rango && c.palo == palo)) continue;
                    var prueba = cartas.ToList();
                    prueba[k] = DatosCarta.Especial(DatosCarta.Poker, rango, cartas[k].nombre, cartas[k].costo,
                        CartasEspeciales.Fichas(rango), CartasEspeciales.TipoDePalo[palo], palo);
                    var r = Evaluar(prueba);
                    if (!hay || r.Valor > mejor.Valor) { mejor = r; hay = true; }
                }
            return mejor;
        }

        static Resultado EvaluarFijas(IReadOnlyList<DatosCarta> cartas)
        {
            var porRango = Enumerable.Range(0, cartas.Count).GroupBy(i => cartas[i].indice)
                .OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();
            bool cinco = cartas.Count == 5;
            bool color = cinco && cartas.All(c => c.palo == cartas[0].palo);
            bool escalera = cinco && EsEscalera(cartas.Select(c => c.indice).ToList());
            var todas = Enumerable.Range(0, cartas.Count).ToList();
            int mayor = porRango[0].Count();

            Resultado Mano(string nombre, int fichas, int mult, List<int> puntuan) =>
                new Resultado { nombre = nombre, fichasBase = fichas, mult = mult, puntuan = puntuan, como = cartas };

            if (escalera && color)
                return Mano(cartas.Min(c => c.indice) == 10 ? "Escalera real" : "Escalera de color", 100, 8, todas);
            if (mayor == 4) return Mano("Póker", 60, 7, porRango[0].ToList());
            if (mayor == 3 && porRango.Count > 1 && porRango[1].Count() == 2) return Mano("Full", 40, 4, todas);
            if (color) return Mano("Color", 35, 4, todas);
            if (escalera) return Mano("Escalera", 30, 4, todas);
            if (mayor == 3) return Mano("Trío", 30, 3, porRango[0].ToList());
            if (mayor == 2 && porRango.Count > 1 && porRango[1].Count() == 2)
                return Mano("Doble pareja", 20, 2, porRango[0].Concat(porRango[1]).ToList());
            if (mayor == 2) return Mano("Pareja", 10, 2, porRango[0].ToList());
            return Mano("Carta alta", 5, 1, new List<int> { porRango[0].First() });
        }

        static bool EsEscalera(List<int> rangos)
        {
            var r = rangos.Distinct().OrderBy(x => x).ToList();
            if (r.Count != 5) return false;
            if (r[4] - r[0] == 4) return true;
            // el as también vale 1: A-2-3-4-5
            return r.SequenceEqual(new[] { 2, 3, 4, 5, 14 });
        }
    }
}
