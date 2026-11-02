using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Los aumentos que traen cartas propias y Todo o nada:
    /// - Hechizos prohibidos: cada turno puede salir un hechizo (12.5 % y +2.5 % por cada
    ///   turno sin suerte, hasta 25 %). En el 25 % sale el 5 o el 6, y si tampoco sale, el
    ///   turno siguiente llega seguro el 6. Cada elemento distinto en la jugada dobla el daño.
    /// - RAAWRMERICA: 3 armas escondidas en el mazo. Cada arma jugada se suma al Arsenal, que
    ///   con 5 o más suelta balas, misiles o una bomba nuclear.
    /// - Póker: la mano son cartas de póker y el daño sale de la mano que se arme.
    /// - Todo o nada (oro): jugar toda la mano o pasar sin jugar sube un multiplicador.
    /// </summary>
    public partial class CombateFalso
    {
        const int ArmasParaArsenal = 5;
        const int PasosHechizo = 5;
        const float ProbabilidadHechizo = 0.125f;
        const float SubidaHechizo = 0.025f;
        const float ProbabilidadCataclismo = 0.15f;
        const float PremioArma = 0.2f;
        const float PasoTodoNada = 0.2f;
        const int FactorPoker = 3;
        /// <summary>Como en una baraja de 54: 2 comodines entre 54 cartas.</summary>
        const float ProbabilidadComodin = 2f / 54f;
        const float RafagaPorArma = 0.5f;

        int armasDisparadas;

        // ---------- Cartas que se quedan en la mano ----------

        void AgregarPersistentes(Jugador j)
        {
            foreach (int indice in j.arcanos) AgregarArcano(indice);
            foreach (int indice in j.hechizos) AgregarHechizo(indice);
            foreach (int indice in j.armas) AgregarArma(indice);
            if (j.tieneArsenal) manoCartas.Agregar(NuevaArsenal(j));
        }

        void AgregarHechizo(int indice)
        {
            var h = CartasEspeciales.Hechizos[indice - 1];
            var carta = Instantiate(prefabCarta);
            carta.Bind(DatosCarta.Especial(DatosCarta.Hechizo, indice, h.nombre, h.costo, h.dano, "elemental"), tipos,
                especiales.hechizos[indice - 1], especiales.marcoHechizo, prismatico, true);
            manoCartas.Agregar(carta);
        }

        void AgregarArma(int indice)
        {
            var a = CartasEspeciales.Armas[indice - 1];
            var carta = Instantiate(prefabCarta);
            carta.Bind(DatosCarta.Especial(DatosCarta.Arma, indice, a.nombre, a.costo, a.dano, a.tipo), tipos,
                especiales.armas[indice - 1], null, null, true);
            manoCartas.Agregar(carta);
        }

        CardView NuevaArsenal(Jugador j)
        {
            var carta = Instantiate(prefabCarta);
            carta.Bind(DatosCarta.Especial(DatosCarta.Arsenal, 0, "Arsenal", 2, j.arsenal.Count, "impacto"), tipos,
                especiales.contenidoArsenal, especiales.marcoArsenal, prismatico, true);
            return carta;
        }

        /// <summary>Saca la carta del Arsenal que haya en la mano y pone una nueva con la cuenta al día.</summary>
        void AgregarArsenal(Jugador j)
        {
            foreach (var c in manoCartas.CartasEnMano().Concat(manoCartas.CartasEnZona()))
                if (c.Datos.Es(DatosCarta.Arsenal)) { c.transform.SetParent(null); Destroy(c.gameObject); }
            manoCartas.Agregar(NuevaArsenal(j));
        }

        CardView NuevaPoker()
        {
            // nunca dos cartas iguales en la mano (ni dos comodines)
            var usadas = new HashSet<int>();
            foreach (var c in manoCartas.CartasEnMano().Concat(manoCartas.CartasEnZona()))
                if (c.Datos.Es(DatosCarta.Poker)) usadas.Add(c.Datos.EsComodin ? -1 : c.Datos.palo * 13 + c.Datos.indice);
            if (!usadas.Contains(-1) && Random.value < ProbabilidadComodin) return NuevoComodin();
            int rango, palo;
            do { rango = Random.Range(2, 15); palo = Random.Range(0, 4); } while (usadas.Contains(palo * 13 + rango));

            string nombre = $"{CartasEspeciales.NombreRango(rango)} de {CartasEspeciales.NombresPalo[palo]}";
            var datos = DatosCarta.Especial(DatosCarta.Poker, rango, nombre, 1, CartasEspeciales.Fichas(rango),
                CartasEspeciales.TipoDePalo[palo], palo);
            var carta = Instantiate(prefabCarta);
            carta.Bind(datos, tipos, especiales.Poker(rango, palo), null, null, true);
            return carta;
        }

        CardView NuevoComodin()
        {
            var carta = Instantiate(prefabCarta);
            carta.Bind(DatosCarta.Especial(DatosCarta.Poker, 0, "Comodín", 1, 0, ""), tipos,
                especiales.contenidoComodin, especiales.marcoComodin, prismatico, true);
            return carta;
        }

        /// <summary>Al elegir Póker, las cartas normales de la mano pasan a ser de póker.</summary>
        void PasarAPoker()
        {
            foreach (var c in manoCartas.CartasEnMano().Concat(manoCartas.CartasEnZona()).ToList())
            {
                if (c.Datos.EsArcano || !string.IsNullOrEmpty(c.Datos.especial)) continue;
                c.transform.SetParent(null);
                Destroy(c.gameObject);
                manoCartas.Agregar(NuevaPoker());
            }
        }

        List<CardView> EnZona(string especial) => manoCartas.CartasEnZona().FindAll(c => c.Datos.Es(especial));

        bool TodaLaManoJugada() => manoCartas.CartasEnMano().TrueForAll(c => c.Datos.Persistente);

        float MultiplicadorTodoNada(Jugador j) => 1f + PasoTodoNada * j.todoNada;

        // ---------- Daño ----------

        /// <summary>
        /// Un pedazo de la jugada y cómo se ve: en cuántos golpes se reparte, con qué efecto,
        /// si cada golpe sale de un lado distinto del enemigo y cuánto se espera entre golpes.
        /// </summary>
        struct Parte
        {
            public float valor;
            public int golpes;
            public EnemyPanel.Efecto efecto;
            public Color[] colores;
            /// <summary>Si cada golpe lleva su propio efecto (los elementos de un hechizo).</summary>
            public EnemyPanel.Efecto[] efectos;
            public bool lados;
            public float espera;
        }

        readonly List<Parte> partes = new List<Parte>();

        static readonly Dictionary<string, Color> ColorElemento = new Dictionary<string, Color>
        {
            { "fuego", new Color32(0xF5, 0x89, 0x2E, 255) },
            { "hielo", new Color32(0x7F, 0xD6, 0xFF, 255) },
            { "rayo", PaletaUI.VioletaReserva },
        };

        static readonly Dictionary<string, EnemyPanel.Efecto> EfectoElemento = new Dictionary<string, EnemyPanel.Efecto>
        {
            { "fuego", EnemyPanel.Efecto.Fuego },
            { "hielo", EnemyPanel.Efecto.Hielo },
            { "rayo", EnemyPanel.Efecto.Rayo },
        };

        // de qué lado del enemigo sale cada golpe de un hechizo (x, y entre -1 y 1)
        static readonly Vector2[] Lados = { new Vector2(-0.9f, 0.2f), new Vector2(0.9f, -0.1f), new Vector2(0.1f, 0.8f), new Vector2(-0.7f, -0.6f), new Vector2(0.7f, 0.6f) };

        /// <summary>Con varias armas en la jugada disparan más y pegan más: x1.5 con 2, x2 con 3...</summary>
        static float Rafaga(int armas) => 1f + RafagaPorArma * Mathf.Max(0, armas - 1);

        /// <summary>
        /// Daño de las cartas que no son arcanos. Las de póker se cuentan como una mano, los
        /// hechizos se multiplican por sus elementos y el Arsenal suma todas sus armas. De paso
        /// llena <see cref="partes"/> con cómo se reparte en golpes.
        /// </summary>
        float DanoJugada(Jugador j, List<DatosCarta> cartas, string soloTipo, out LluviaArsenal.Nivel? arsenal, out string mano)
        {
            arsenal = null;
            mano = null;
            partes.Clear();
            float total = 0;

            void Sumar(float valor, int golpes = 1, EnemyPanel.Efecto efecto = EnemyPanel.Efecto.Ninguno,
                Color[] colores = null, bool lados = false, float espera = 0)
            {
                total += valor;
                partes.Add(new Parte { valor = valor, golpes = Mathf.Max(1, golpes), efecto = efecto, colores = colores, lados = lados, espera = espera });
            }

            var poker = cartas.FindAll(c => c.Es(DatosCarta.Poker));
            if (poker.Count > 0)
            {
                var r = ManoPoker.Evaluar(poker);
                int fichas = r.fichasBase;
                float resistencia = 0;
                // el comodín cuenta como la carta en la que se convirtió (rango y palo)
                foreach (int i in r.puntuan)
                {
                    var c = r.como[i];
                    fichas += CartasEspeciales.Fichas(c.indice);
                    resistencia += actual.Multiplicador(c.tipo) * (c.tipo == soloTipo ? 2 : 1);
                }
                resistencia /= r.puntuan.Count;
                Sumar(fichas * r.mult * resistencia * FactorPoker);
                mano = $"{r.nombre} · {fichas} x{r.mult}";
            }

            var hechizos = cartas.FindAll(c => c.Es(DatosCarta.Hechizo));
            if (hechizos.Count > 0)
            {
                int elementos = hechizos.SelectMany(h => CartasEspeciales.Hechizos[h.indice - 1].elementos).Distinct().Count();
                // cada hechizo pega una vez por elemento, cada vez desde otro lado y en su color
                foreach (var h in hechizos)
                {
                    var suyos = CartasEspeciales.Hechizos[h.indice - 1].elementos;
                    Sumar(Puntos(h) * (h.tipo == soloTipo ? 2 : 1) * (1 << (elementos - 1)), suyos.Length, EnemyPanel.Efecto.Ninguno,
                        suyos.Select(e => ColorElemento[e]).ToArray(), true, 0.14f);
                    var ultima = partes[partes.Count - 1];
                    ultima.efectos = suyos.Select(e => EfectoElemento[e]).ToArray();
                    partes[partes.Count - 1] = ultima;
                }
                if (elementos > 1) mano = $"{elementos} elementos · x{1 << (elementos - 1)}";
            }

            if (cartas.Exists(c => c.Es(DatosCarta.Arsenal)))
            {
                armasDisparadas = j.arsenal.Count;
                var nivel = LluviaArsenal.NivelDe(j.arsenal);
                float suma = j.arsenal.Sum(i => CartasEspeciales.Armas[i - 1].dano) * 10;
                float mult = nivel == LluviaArsenal.Nivel.Nuclear ? 3f : nivel == LluviaArsenal.Nivel.Misiles ? 2f : 1.5f;
                Sumar(suma * mult * actual.Multiplicador("impacto"));
                arsenal = nivel;
                mano = LluviaArsenal.NombreDe(nivel);
            }

            var armas = cartas.FindAll(c => c.Es(DatosCarta.Arma));
            float rafaga = Rafaga(armas.Count);
            foreach (var a in armas)
            {
                var arma = CartasEspeciales.Armas[a.indice - 1];
                bool bala = arma.efecto == EnemyPanel.Efecto.Bala;
                // la metralleta escupe más rápido que el resto
                float espera = bala ? (arma.disparos >= 8 ? 0.05f : 0.09f) : 0.22f;
                Sumar(Puntos(a) * (a.tipo == soloTipo ? 2 : 1) * rafaga, Mathf.RoundToInt(arma.disparos * rafaga), arma.efecto, null, false, espera);
            }
            if (armas.Count > 1) mano = $"Ráfaga x{rafaga.ToString("0.#", CultureInfo.InvariantCulture)}";

            float normales = 0;
            foreach (var c in cartas)
                if (string.IsNullOrEmpty(c.especial)) normales += Puntos(c) * (c.tipo == soloTipo ? 2 : 1);
            if (normales > 0) Sumar(normales);
            return total;
        }

        /// <summary>
        /// Reparte el daño ya calculado (con críticos y demás) en los golpes de cada parte, en
        /// proporción a lo que aportó cada una.
        /// </summary>
        IEnumerator Golpear(int dano, Color color)
        {
            float suma = partes.Sum(p => p.valor);
            if (partes.Count == 0 || suma <= 0)
            {
                enemigo.RecibirGolpe(dano, color, false);
                yield break;
            }
            int restante = dano;
            for (int i = 0; i < partes.Count; i++)
            {
                var p = partes[i];
                int danoParte = i == partes.Count - 1 ? restante : Mathf.RoundToInt(dano * p.valor / suma);
                restante -= danoParte;
                for (int g = 0; g < p.golpes; g++)
                {
                    int pedazo = danoParte / p.golpes + (g < danoParte % p.golpes ? 1 : 0);
                    Vector2? donde = null;
                    if (p.lados) donde = Lados[g % Lados.Length] + new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
                    else if (p.efecto != EnemyPanel.Efecto.Ninguno) donde = new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(-0.6f, 0.6f));
                    var c = p.colores != null ? p.colores[g % p.colores.Length] : color;
                    var efecto = p.efectos != null ? p.efectos[g % p.efectos.Length] : p.efecto;
                    enemigo.Impacto(pedazo, c, donde, efecto);
                    if (p.espera > 0) yield return new WaitForSeconds(p.espera);
                }
            }
        }

        /// <summary>Saca de las listas del jugador lo que jugó. Devuelve si el Arsenal cambió.</summary>
        bool SacarJugadas(Jugador j, List<DatosCarta> cartas)
        {
            bool cambio = false;
            // primero se vacía el Arsenal, así un arma jugada a la vez ya cuenta para el próximo
            if (cartas.Exists(c => c.Es(DatosCarta.Arsenal)))
            {
                j.arsenal.Clear();
                j.tieneArsenal = false;
            }
            foreach (var c in cartas)
            {
                if (c.Es(DatosCarta.Hechizo)) j.hechizos.Remove(c.indice);
                if (!c.Es(DatosCarta.Arma)) continue;
                j.armas.Remove(c.indice);
                j.arsenal.Add(c.indice);
                j.tieneArsenal = true;
                cambio = true;
            }
            return cambio;
        }

        // ---------- Hechizos prohibidos ----------

        float ProbabilidadDe(Jugador j) =>
            j.pasoHechizo > PasosHechizo ? 1f : ProbabilidadHechizo + SubidaHechizo * j.pasoHechizo;

        /// <summary>Tira el dado del turno. Devuelve el hechizo robado (1 a 6) o 0.</summary>
        int TirarHechizo(Jugador j)
        {
            int robado = 0;
            if (j.pasoHechizo > PasosHechizo) robado = 6;
            else if (Random.value < ProbabilidadDe(j))
                robado = j.pasoHechizo < PasosHechizo ? Random.Range(1, 5)
                    : Random.value < ProbabilidadCataclismo ? 6 : 5;

            if (robado == 0) { j.pasoHechizo++; return 0; }
            j.pasoHechizo = 0;
            j.hechizos.Add(robado);
            return robado;
        }

        // ---------- RAAWRMERICA ----------

        List<int> ArmasAlAzar(int cuantas)
        {
            var lista = new List<int>();
            for (int i = 0; i < cuantas; i++) lista.Add(Random.Range(1, CartasEspeciales.Armas.Length + 1));
            return lista;
        }

        IEnumerator RaawrMerica(Jugador j)
        {
            var nuevas = ArmasAlAzar(3);
            string nombres = string.Join(", ", nuevas.Select(i => CartasEspeciales.Armas[i - 1].nombre.ToLowerInvariant()));
            if (j.mazo >= 3)
            {
                j.armasEnMazo.AddRange(nuevas);
                yield return Aviso($"Tu mazo esconde: {nombres}", "¡RAAWRMERICA!", PaletaUI.Rojo, null, 16);
            }
            else
            {
                yield return Aviso($"A la mano: {nombres}", "¡RAAWRMERICA!", PaletaUI.Rojo, () =>
                {
                    j.armas.AddRange(nuevas);
                    foreach (int i in nuevas) AgregarArma(i);
                }, 16);
            }
        }

        /// <summary>Al morir el enemigo, un arma a quien le haya hecho al menos el 20 % de la vida.</summary>
        IEnumerator PremiarArmas()
        {
            for (int n = 0; n < jugadores.Length; n++)
            {
                var j = jugadores[n];
                if (!j.Tiene("raawr") || j.danoEnemigo < actual.vida * PremioArma) continue;
                int arma = ArmasAlAzar(1)[0];
                yield return Aviso($"J{n + 1} hizo el {PremioArma * 100:0}% · arma de premio",
                    CartasEspeciales.Armas[arma - 1].nombre.ToUpperInvariant(), PaletaUI.Rojo, () =>
                    {
                        j.armas.Add(arma);
                        if (j == Actual) AgregarArma(arma);
                    }, 16);
            }
        }

        // ---------- Textos ----------

        /// <summary>Líneas bajo el maná con lo que llevan los aumentos que cuentan algo.</summary>
        IEnumerable<string> Estado(Jugador j)
        {
            if (j.Tiene("arcanos"))
            {
                yield return $"Tríos {j.trios}/{MaxTrios}";
                foreach (var b in Bonos(j)) yield return b;
            }
            if (j.Tiene("prohibidos")) yield return $"Hechizo {ProbabilidadDe(j) * 100:0.#}%";
            if (j.Tiene("raawr")) yield return $"Arsenal {j.arsenal.Count}/{ArmasParaArsenal} · {j.arsenal.Distinct().Count()}/6";
            if (j.Tiene("todo")) yield return $"Todo o nada x{MultiplicadorTodoNada(j).ToString("0.0", CultureInfo.InvariantCulture)}";
        }

        /// <summary>Con cartas de póker en la zona, qué mano se está armando.</summary>
        string VistaPoker()
        {
            var poker = EnZona(DatosCarta.Poker).ConvertAll(c => c.Datos);
            if (poker.Count == 0) return null;
            var r = ManoPoker.Evaluar(poker);
            int k = poker.FindIndex(c => c.EsComodin);
            if (k < 0) return $"{r.nombre} x{r.mult}";
            var vale = r.como[k];
            return $"{r.nombre} x{r.mult} · comodín = {CartasEspeciales.NombreRango(vale.indice)} de {CartasEspeciales.NombresPalo[vale.palo]}";
        }

        string DescribirEspecial(DatosCarta d)
        {
            var j = Actual;
            if (d.Es(DatosCarta.Hechizo))
            {
                var h = CartasEspeciales.Hechizos[d.indice - 1];
                return $"Cuesta {d.costo}. {d.dano} de daño ({string.Join(", ", h.elementos)}).\n" +
                       $"Cada elemento distinto en la jugada x2.\n{actual.nombre}: {Puntos(d)} pts";
            }
            if (d.Es(DatosCarta.Arma))
                return $"Cuesta {d.costo}. {d.dano} de daño {NombreFamilia(d.tipo)} en {CartasEspeciales.Armas[d.indice - 1].disparos} disparos.\n" +
                       $"Al jugarla se suma al Arsenal ({j.arsenal.Count}/{ArmasParaArsenal}).\n{actual.nombre}: {Puntos(d)} pts";
            if (d.Es(DatosCarta.Arsenal))
            {
                var nombres = j.arsenal.Select(i => CartasEspeciales.Armas[i - 1].nombre.ToLowerInvariant());
                return $"{string.Join(", ", nombres)}.\n{j.arsenal.Count}/{ArmasParaArsenal}: 5 balas, 7 misiles.\nLas 6 armas distintas: bomba nuclear ({j.arsenal.Distinct().Count()}/6).";
            }
            if (d.EsComodin)
                return "Cuesta 1. Vale por la carta que mejor\ncomplete tu mano de póker (rango y palo).";
            if (d.Es(DatosCarta.Poker))
                return $"Cuesta 1 · {d.dano} fichas.\nDaño {NombreFamilia(d.tipo)} ({CartasEspeciales.NombresPalo[d.palo]}).\n" +
                       "Arma la mejor mano de póker (hasta 5).";
            return "";
        }
    }
}
