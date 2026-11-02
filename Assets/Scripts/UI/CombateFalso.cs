using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Mueve la pantalla de combate con datos de ejemplo mientras no existan los
    /// sistemas de los integrantes 1 y 3. Reglas provisionales:
    /// - En el turno N hay N de maná (tope 10); lo que no se gasta pasa a la reserva (tope 3).
    /// - Cada ronda son 3 enemigos: uno Normal, uno Medio y uno Élite, al azar dentro de su nivel.
    /// - El daño de una jugada es daño × resistencia × 10: baja la vida del enemigo y
    ///   suma esos puntos al jugador.
    /// - El enemigo con la habilidad "bloqueo" puede anular el daño de todo el turno siguiente
    ///   (estado Escudo). Los buffs y debuffs del enemigo cambian el daño que recibe.
    /// - El costo que se paga es el de la carta con sus estados (CardView.Costo). La primera vez
    ///   que el Caballero baja a la mitad de su vida lanza Penitencia: todas las cartas de la mano
    ///   de ese momento cuestan +1 hasta el final del combate (vuelven cada turno marcadas).
    /// - Los hechizos de fuego dejan Quemadura (pierde 5 % de su vida al pasar cada turno).
    /// - El enemigo que se levanta (el Caballero) se cura toda la vida la primera vez que llega a 0 y gana
    ///   su buff hasta el final del combate; la segunda vez sí muere.
    /// - Aumentos (estilo TFT): 3 por partida, uno en cada una de las rondas 1 a 3, al llegar
    ///   un enemigo elegido al azar. Uno es de plata, otro de oro (en orden al azar) y el
    ///   último siempre prismático. Los dos jugadores ven las mismas tres opciones.
    /// - Arcanos mayores (prismático): una carta de tarot por turno. Jugar 3 juntas forma un
    ///   bono permanente (máximo 5); jugar 1 sola da un efecto solo en esa jugada.
    /// - Los otros prismáticos (hechizos prohibidos, RAAWRMERICA y póker) y Todo o nada están
    ///   en CombateFalso.Prismaticos.cs.
    /// Se reemplaza entero cuando llegue la lógica real.
    /// </summary>
    public partial class CombateFalso : MonoBehaviour
    {
        // 4 en el duelo y 1 en la partida rápida (pantalla Modo)
        static int RondasTotales => ModoPartida.Rondas;
        const int CartasPorMano = 6;
        const float ProbabilidadBloqueo = 0.5f;
        const int OfertasTotales = 3;
        const int MaxTrios = 5;
        const int MaxArcanosEnMano = 5;
        const float BonoTrio = 0.15f;

        class Jugador
        {
            public int maxMana, mana, reserva, puntos, vida, mazo;
            /// <summary>Puntos hechos en cada ronda, para la tabla de resultados.</summary>
            public readonly int[] porRonda = new int[RondasTotales];
            public readonly List<DatosAumento> aumentos = new List<DatosAumento>();
            public readonly Queue<Oferta> pendientes = new Queue<Oferta>();
            /// <summary>Arcanos en la mano (índices); se guardan de un turno al otro.</summary>
            public readonly List<int> arcanos = new List<int>();
            /// <summary>Bonos permanentes de los tríos, por tipo de daño.</summary>
            public readonly Dictionary<string, float> bonoTipo = new Dictionary<string, float>();
            public int trios, manaExtra;
            /// <summary>Hechizos prohibidos en la mano (1 a 6) y en qué paso va la probabilidad de robar.</summary>
            public readonly List<int> hechizos = new List<int>();
            public int pasoHechizo;
            /// <summary>RAAWRMERICA: armas en la mano, armas escondidas en el mazo y armas ya juntadas en el Arsenal.</summary>
            public readonly List<int> armas = new List<int>();
            public readonly List<int> armasEnMazo = new List<int>();
            public readonly List<int> arsenal = new List<int>();
            public bool tieneArsenal;
            /// <summary>Daño hecho al enemigo actual (para el arma de premio al 20 %).</summary>
            public int danoEnemigo;
            /// <summary>Todo o nada: cuántas veces jugó toda la mano o pasó sin jugar.</summary>
            public int todoNada;
            public bool jugoEsteTurno;
            /// <summary>
            /// Penitencia: cartas normales marcadas, que vuelven a la mano cada turno, y claves de las
            /// especiales marcadas ("hechizo:4"). Se borran al jugarlas o al cambiar de enemigo.
            /// </summary>
            public readonly List<DatosCarta> marcadas = new List<DatosCarta>();
            public readonly HashSet<string> especialesMarcadas = new HashSet<string>();

            public bool Tiene(string id) => aumentos.Exists(a => a.id == id);
        }

        class Oferta
        {
            public int numero;
            public List<DatosAumento> opciones;
        }

        [SerializeField] TiposDano tipos;
        [SerializeField] CardView prefabCarta;
        [SerializeField] ManoCartas manoCartas;
        [SerializeField] PlayerHUD hudJ1;
        [SerializeField] PlayerHUD hudJ2;
        [SerializeField] ManaBar barraMana;
        [SerializeField] EnemyPanel enemigo;
        [SerializeField] UnityEngine.UI.Button botonJugar;
        [SerializeField] UnityEngine.UI.Button botonPasar;
        [SerializeField] TMP_Text textoTurno;
        [SerializeField] TMP_Text textoRonda;
        [Tooltip("Una casilla por enemigo de la ronda: Normal, Medio y Élite")]
        [SerializeField] UnityEngine.UI.Image[] casillasEnemigo;
        [SerializeField] TMP_Text textoPista;
        [Tooltip("Aviso chico sobre el marco de la zona, para cuando las cartas tapan la pista")]
        [SerializeField] TMP_Text textoAvisoZona;
        [SerializeField] TMP_Text textoMazo;
        [SerializeField] CanvasGroup avisoTurno;
        [SerializeField] TMP_Text avisoTexto;
        [SerializeField] TMP_Text avisoJugador;
        [SerializeField] PasaDispositivo pasaDispositivo;
        [SerializeField] string escenaResultados = "Resultados";
        [Header("En cada ronda sale uno al azar de cada lista, en este orden")]
        [SerializeField] DatosEnemigo[] normales;
        [SerializeField] DatosEnemigo[] medios;
        [SerializeField] DatosEnemigo[] elites;
        [Header("Aumentos")]
        [SerializeField] PantallaAumentos pantallaAumentos;
        [SerializeField] DatosAumento[] aumentosPlata;
        [SerializeField] DatosAumento[] aumentosOro;
        [SerializeField] DatosAumento[] aumentosPrismaticos;
        [SerializeField] ArcanosMayores arcanos;
        [Tooltip("Estado de los aumentos que llevan cuenta (tríos, hechizos, Arsenal, Todo o nada)")]
        [SerializeField] TMP_Text textoArcanos;
        [SerializeField] CartasEspeciales especiales;
        [SerializeField] Material prismatico;
        [SerializeField] LluviaArsenal lluvia;
        [Header("Buffs y debuffs del enemigo")]
        [SerializeField] DatosEstado estadoEscudo;
        [Tooltip("Lo pone un hechizo de hielo")]
        [SerializeField] DatosEstado estadoVulnerable;
        [Tooltip("Lo pone un hechizo de fuego")]
        [SerializeField] DatosEstado estadoQuemadura;

        static readonly Dictionary<string, string[]> Nombres = new Dictionary<string, string[]>
        {
            { "cortante", new[] { "Tajo", "Corte doble", "Guillotina" } },
            { "perforante", new[] { "Dardo", "Estocada", "Lanza" } },
            { "impacto", new[] { "Golpe", "Martillazo", "Terremoto" } },
            { "elemental", new[] { "Chispa", "Llamarada", "Infierno" } },
        };

        readonly Jugador[] jugadores =
        {
            new Jugador { vida = 3, mazo = 30 },
            new Jugador { vida = 3, mazo = 30 },
        };

        int activo = 1;
        int ronda = 1;
        int indiceEnemigo;
        DatosEnemigo actual;
        int vidaEnemigo;
        readonly List<EstadoActivo> estados = new List<EstadoActivo>();
        bool seLevanto;
        // el estado de cartas (Penitencia) sale una sola vez, al bajar por primera vez del umbral
        bool tocoUmbral, estadoCartasLanzado;
        bool ocupado;
        // en qué enemigo de cada ronda (0 a 2) sale la oferta, y de qué calidad es
        readonly int[] enemigoDeOferta = new int[OfertasTotales];
        readonly CalidadAumento[] calidadDeOferta = new CalidadAumento[OfertasTotales];
        System.Random azar;

        Jugador Actual => jugadores[activo - 1];
        bool Escudo => estados.Exists(e => e.datos.anulaDano);

        /// <summary>Cuánto del daño le llega al enemigo según sus buffs y debuffs.</summary>
        float DanoRecibido
        {
            get
            {
                float m = 1f;
                foreach (var e in estados) m *= e.datos.danoRecibido;
                return m;
            }
        }

        void Start()
        {
            Time.timeScale = Ajustes.Velocidad;
            // la semilla de la preparación: las mismas cartas y enemigos con el mismo código
            if (ModoPartida.Semilla != 0) Random.InitState(ModoPartida.Semilla);
            botonJugar.onClick.AddListener(() => StartCoroutine(Lanzar()));
            botonPasar.onClick.AddListener(() => StartCoroutine(CambioDeTurno()));
            avisoTurno.alpha = 0;
            avisoTurno.blocksRaycasts = false;
            manoCartas.Tipos = tipos;
            enemigo.Efectos = especiales;
            manoCartas.Descripcion = Describir;
            manoCartas.PidioElegir += Elegir;
            manoCartas.PidioDevolver += Devolver;

            // para la demo se empieza con 3 de maná y no con 1
            foreach (var j in jugadores) j.maxMana = j.mana = 3;
            PrepararAumentos();
            NuevoEnemigo();
            RepartirMano();
            StartCoroutine(Arrancar());
        }

        void OnDestroy() => Time.timeScale = 1;

        IEnumerator Arrancar()
        {
            ocupado = true;
            Refrescar();
            yield return null;
            yield return OfrecerPendientes();
            ocupado = false;
            Refrescar();
        }

        // ---------- Cartas ----------

        void RepartirMano()
        {
            // Destroy es al final del cuadro: se sacan ya para que no cuenten en la mano
            var viejas = manoCartas.CartasEnMano();
            viejas.AddRange(manoCartas.CartasEnZona());
            foreach (var c in viejas) { c.transform.SetParent(null); Destroy(c.gameObject); }
            var j = Actual;
            // las marcadas por Penitencia vuelven primero y ocupan su lugar en la mano
            foreach (var d in j.marcadas)
            {
                var c = CrearCarta(d);
                c.PonerEstado(actual.estadoCartas);
                manoCartas.Agregar(c);
            }
            int cantidad = Mathf.Max(0, CartasPorMano + (j.Tiene("mano") ? 1 : 0) - j.marcadas.Count);
            for (int i = 0; i < cantidad; i++)
            {
                // las armas de RAAWRMERICA están escondidas en el mazo: salen con su probabilidad
                if (j.armasEnMazo.Count > 0 && Random.value < (float)j.armasEnMazo.Count / Mathf.Max(1, j.mazo))
                {
                    j.armas.Add(j.armasEnMazo[0]);
                    j.armasEnMazo.RemoveAt(0);
                }
                else manoCartas.Agregar(NuevaCarta());
            }
            AgregarPersistentes(j);
            foreach (var c in manoCartas.CartasEnMano())
                if (!EsNormal(c.Datos) && j.especialesMarcadas.Contains(Clave(c.Datos))) c.PonerEstado(actual.estadoCartas);
        }

        static string Clave(DatosCarta d) => d.especial + ":" + d.indice;

        /// <summary>Normales y de póker se reparten de nuevo; las especiales vuelven solas a la mano.</summary>
        static bool EsNormal(DatosCarta d) => string.IsNullOrEmpty(d.especial) || d.Es(DatosCarta.Poker);

        /// <summary>Vuelve a armar una carta normal o de póker a partir de sus datos.</summary>
        CardView CrearCarta(DatosCarta d)
        {
            var carta = Instantiate(prefabCarta);
            if (d.EsComodin) carta.Bind(d, tipos, especiales.contenidoComodin, especiales.marcoComodin, prismatico, true);
            else if (d.Es(DatosCarta.Poker)) carta.Bind(d, tipos, especiales.Poker(d.indice, d.palo), null, null, true);
            else carta.Bind(d, tipos);
            return carta;
        }

        CardView NuevaCarta()
        {
            if (Actual.Tiene("poker")) return NuevaPoker();
            var carta = Instantiate(prefabCarta);
            carta.Bind(CartaAlAzar(), tipos);
            return carta;
        }

        void AgregarArcano(int indice)
        {
            var a = arcanos.cartas[indice];
            var carta = Instantiate(prefabCarta);
            carta.Bind(DatosCarta.Arcano(indice, a.nombre, a.familia), tipos, a.arte);
            manoCartas.Agregar(carta);
        }

        /// <summary>Roba un arcano que no tenga ya en la mano.</summary>
        void DarArcano(Jugador j, bool aLaVista)
        {
            if (j.arcanos.Count >= MaxArcanosEnMano) return;
            var libres = new List<int>();
            for (int i = 0; i < arcanos.cartas.Count; i++) if (!j.arcanos.Contains(i)) libres.Add(i);
            int indice = libres[Random.Range(0, libres.Count)];
            j.arcanos.Add(indice);
            if (aLaVista) AgregarArcano(indice);
        }

        DatosCarta CartaAlAzar()
        {
            var tipo = tipos.tipos[Random.Range(0, tipos.tipos.Count)].id;
            int costo = Random.Range(1, 7);
            int dano = costo * 2 + Random.Range(1, 4);
            var nombres = Nombres.TryGetValue(tipo, out var n) ? n : new[] { tipo };
            var nombre = nombres[Mathf.Min(nombres.Length - 1, (costo - 1) / 2)];
            return new DatosCarta(nombre, costo, dano, tipo);
        }

        /// <summary>Puntos fijos de una carta, con los bonos permanentes (sin los de azar).</summary>
        int Puntos(DatosCarta c)
        {
            if (c.EsArcano) return 0;
            var j = Actual;
            float m = actual.Multiplicador(c.tipo);
            float bono = 1f;
            if (j.Tiene("filo")) bono += 0.10f;
            if (j.Tiene("especialista") && m > 1f) bono += 0.30f;
            if (j.bonoTipo.TryGetValue(c.tipo, out var b)) bono += b;
            return Mathf.RoundToInt(c.dano * m * 10 * bono * DanoRecibido);
        }

        string NombreFamilia(string familia)
        {
            var t = tipos.Buscar(familia);
            return t != null ? t.nombre.ToLower() : "maná";
        }

        string Describir(CardView carta)
        {
            var d = carta.Datos;
            // el texto dice lo que cuesta ahora, con Penitencia y compañía
            d.costo = carta.Costo;
            if (!string.IsNullOrEmpty(d.especial)) return DescribirEspecial(d);
            if (d.EsArcano)
            {
                bool mana = d.tipo == ArcanosMayores.FamiliaMana;
                string fam = NombreFamilia(d.tipo);
                return (mana ? "Sola: +2 de reserva." : $"Sola: x2 al daño {fam}.") +
                       (mana ? "\nTrío: +1 de maná por turno." : $"\nTrío: +15% {fam} siempre.") +
                       $"\nTríos {Actual.trios}/{MaxTrios} · cuesta 0";
            }
            var t = tipos.Buscar(d.tipo);
            float m = actual.Multiplicador(d.tipo);
            string mult = "x" + m.ToString("0.##", CultureInfo.InvariantCulture);
            string efecto = m > 1f ? " (débil)" : m < 1f ? " (resiste)" : "";
            float r = DanoRecibido;
            if (r != 1f) efecto += " · estados x" + r.ToString("0.##", CultureInfo.InvariantCulture);
            string texto = $"Cuesta {d.costo} de maná.\nInflige {d.dano} de daño {(t != null ? t.nombre.ToLower() : d.tipo)}.\n" +
                           $"{actual.nombre}: {mult}{efecto} = {Puntos(d)} pts";
            return Escudo ? texto + "\n¡Este turno lo bloquea!" : texto;
        }

        int CostoElegido()
        {
            int total = 0;
            foreach (var c in manoCartas.CartasEnZona()) total += c.Costo;
            return total;
        }

        void Elegir(CardView carta)
        {
            int disponible = Actual.mana + Actual.reserva - CostoElegido();
            if (carta.Datos.EsArcano && ArcanosEnZona() >= 3) StartCoroutine(AvisarPista("Máximo 3 arcanos"));
            else if (carta.Datos.Es(DatosCarta.Poker) && EnZona(DatosCarta.Poker).Count >= 5) StartCoroutine(AvisarPista("Una mano de póker es de 5"));
            else if (carta.Costo <= disponible) manoCartas.MoverAZona(carta);
            else StartCoroutine(AvisarPista("No te alcanza el maná"));
            Refrescar();
        }

        void Devolver(CardView carta)
        {
            manoCartas.MoverAMano(carta);
            Refrescar();
        }

        int ArcanosEnZona() => manoCartas.CartasEnZona().FindAll(c => c.Datos.EsArcano).Count;

        /// <summary>Por qué no se puede jugar la zona tal como está (null si se puede).</summary>
        string MotivoBloqueo()
        {
            int n = ArcanosEnZona();
            if (n == 2) return "Pon 1 arcano o 3";
            if (n == 3 && Actual.trios >= MaxTrios) return "Ya tienes 5 tríos";
            if (EnZona(DatosCarta.Arsenal).Count > 0 && Actual.arsenal.Count < ArmasParaArsenal)
                return $"El Arsenal pide {ArmasParaArsenal} armas";
            return null;
        }

        int ManaMaxima(Jugador j) => Mathf.Min(ManaBar.MaxMana, j.maxMana + j.manaExtra);

        // ---------- Pantalla ----------

        void Refrescar()
        {
            var j = Actual;
            int costo = CostoElegido();
            int disponible = j.mana + j.reserva;

            hudJ1.Mostrar(1, jugadores[0].vida, jugadores[0].puntos, activo == 1);
            hudJ2.Mostrar(2, jugadores[1].vida, jugadores[1].puntos, activo == 2);
            barraMana.Mostrar(j.mana, ManaMaxima(j), j.reserva, costo);
            hudJ1.MostrarAumentos(jugadores[0].aumentos);
            hudJ2.MostrarAumentos(jugadores[1].aumentos);
            var estado = new List<string>(Estado(j));
            textoArcanos.gameObject.SetActive(estado.Count > 0);
            textoArcanos.text = string.Join("\n", estado);
            enemigo.MostrarVida(vidaEnemigo, actual.vida);
            textoTurno.text = "Turno J" + activo;
            textoRonda.text = $"RONDA {ronda}/{RondasTotales}";
            textoMazo.text = j.mazo.ToString();
            for (int i = 0; i < casillasEnemigo.Length; i++)
                casillasEnemigo[i].color = i < indiceEnemigo ? PaletaUI.Menta : i == indiceEnemigo ? PaletaUI.Oro : PaletaUI.Hueco;

            foreach (var c in manoCartas.CartasEnMano()) c.SetJugable(c.Costo <= disponible - costo);
            foreach (var c in manoCartas.CartasEnZona()) c.SetJugable(true);

            bool zonaVacia = manoCartas.CartasEnZona().Count == 0;
            string bloqueo = MotivoBloqueo();
            textoPista.gameObject.SetActive(zonaVacia);
            textoPista.text = "Toca o arrastra cartas aquí";
            textoPista.color = colorPista;
            // con cartas de póker en la zona se ve qué mano se está armando
            string aviso = bloqueo ?? VistaPoker();
            textoAvisoZona.gameObject.SetActive(aviso != null);
            textoAvisoZona.text = aviso;
            textoAvisoZona.color = bloqueo != null ? PaletaUI.Rosa : PaletaUI.Oro;
            botonJugar.interactable = !ocupado && !zonaVacia && bloqueo == null;
            botonPasar.interactable = !ocupado;
            manoCartas.Activa = !ocupado;
        }

        Color colorPista;

        void Awake() => colorPista = textoPista.color;

        IEnumerator AvisarPista(string mensaje) => AvisarPista(mensaje, PaletaUI.Rosa);

        IEnumerator AvisarPista(string mensaje, Color color)
        {
            // con cartas en la zona la pista grande queda tapada: se usa el aviso chico
            var texto = manoCartas.CartasEnZona().Count > 0 ? textoAvisoZona : textoPista;
            texto.gameObject.SetActive(true);
            texto.text = mensaje;
            texto.color = color;
            yield return new WaitForSeconds(1.2f);
            Refrescar();
        }

        // ---------- Jugada ----------

        IEnumerator Lanzar()
        {
            var jugada = manoCartas.CartasEnZona();
            if (ocupado || jugada.Count == 0 || MotivoBloqueo() != null) yield break;
            ocupado = true;
            manoCartas.Cancelar();

            var j = Actual;
            int costo = CostoElegido();
            int deMana = Mathf.Min(costo, j.mana);
            j.mana -= deMana;
            j.reserva -= costo - deMana;
            j.mazo = Mathf.Max(0, j.mazo - jugada.Count);

            var jugados = jugada.FindAll(c => c.Datos.EsArcano).ConvertAll(c => c.Datos);
            var normales = jugada.FindAll(c => !c.Datos.EsArcano);
            string soloTipo = jugados.Count == 1 ? jugados[0].tipo : null;

            // cada grupo de cartas especiales suma a su manera (CombateFalso.Prismaticos.cs)
            float total = DanoJugada(j, normales.ConvertAll(c => c.Datos), soloTipo, out var arsenal, out var mano);
            if (j.Tiene("todo")) total *= MultiplicadorTodoNada(j);
            bool critico = j.Tiene("critico") && normales.Count > 0 && Random.value < 0.25f;
            if (critico) total *= 2;
            int dano = Mathf.RoundToInt(total);

            bool hielo = TieneElemento(normales, "hielo");
            bool fuego = TieneElemento(normales, "fuego");
            foreach (var a in jugados) j.arcanos.Remove(a.arcano - 1);
            // jugarla le quita la Penitencia: ya no vuelve marcada
            foreach (var c in jugada)
            {
                if (c.Estados.Count == 0) continue;
                if (EsNormal(c.Datos)) j.marcadas.Remove(c.Datos);
                else j.especialesMarcadas.Remove(Clave(c.Datos));
            }
            foreach (var c in jugada) { c.transform.SetParent(null); Destroy(c.gameObject); }
            bool nuevoArsenal = SacarJugadas(j, normales.ConvertAll(c => c.Datos));
            j.jugoEsteTurno = true;
            bool todo = j.Tiene("todo") && TodaLaManoJugada();
            if (todo) j.todoNada++;

            if (soloTipo == ArcanosMayores.FamiliaMana) j.reserva = Mathf.Min(ManaBar.MaxReserva, j.reserva + 2);

            if (mano != null) StartCoroutine(AvisarPista(mano, PaletaUI.Oro));
            if (arsenal != null) yield return lluvia.Lanzar(arsenal.Value, arsenal.Value == LluviaArsenal.Nivel.Nuclear ? 10 : armasDisparadas);
            if (normales.Count > 0)
            {
                if (Escudo) enemigo.RecibirGolpe(0, Color.white, true);
                else
                {
                    j.puntos += dano;
                    j.danoEnemigo += dano;
                    j.porRonda[ronda - 1] += dano;
                    RegistrarEstilo(normales.ConvertAll(c => c.Datos), dano);
                    vidaEnemigo -= dano;
                    if (vidaEnemigo <= actual.vida * actual.umbralEstadoCartas) tocoUmbral = true;
                    Refrescar();
                    yield return Golpear(dano, critico ? PaletaUI.Oro : PaletaUI.DeJugador(activo));
                    if (hielo && vidaEnemigo > 0 && estadoVulnerable != null) yield return AplicarEstado(estadoVulnerable);
                    if (fuego && vidaEnemigo > 0 && estadoQuemadura != null) yield return AplicarEstado(estadoQuemadura);
                }
            }
            Refrescar();
            yield return new WaitForSeconds(0.45f);

            if (jugados.Count == 3) yield return FormarTrio(j, jugados);
            if (nuevoArsenal) AgregarArsenal(j);
            if (todo) yield return Aviso("Todo o nada · jugaste toda la mano", $"¡TODO! x{MultiplicadorTodoNada(j):0.0}", PaletaUI.Oro, null, 16);

            if (vidaEnemigo <= 0 && actual.levantarse && !seLevanto) yield return Levantarse();
            yield return RevisarEstadoCartas();
            if (vidaEnemigo <= 0)
            {
                yield return enemigo.Morir();
                yield return PremiarArmas();
                yield return SiguienteEnemigo();
                yield return OfrecerPendientes();
            }
            ocupado = false;
            Refrescar();
        }

        void NuevoEnemigo()
        {
            var lista = indiceEnemigo == 0 ? normales : indiceEnemigo == 1 ? medios : elites;
            actual = lista[Random.Range(0, lista.Length)];
            // el primero es el que se mostró en la preparación
            if (ModoPartida.PrimerEnemigo != null && indiceEnemigo == 0 && System.Array.IndexOf(lista, ModoPartida.PrimerEnemigo) >= 0)
                actual = ModoPartida.PrimerEnemigo;
            ModoPartida.PrimerEnemigo = null;
            vidaEnemigo = actual.vida;
            estados.Clear();
            seLevanto = false;
            tocoUmbral = estadoCartasLanzado = false;
            // la Penitencia dura hasta el final del combate con el que la lanzó
            foreach (var j in jugadores) { j.marcadas.Clear(); j.especialesMarcadas.Clear(); }
            foreach (var c in manoCartas.CartasEnMano()) c.QuitarEstados();
            foreach (var c in manoCartas.CartasEnZona()) c.QuitarEstados();
            enemigo.MostrarEstados(estados);
            foreach (var j in jugadores) j.danoEnemigo = 0;
            enemigo.Mostrar(actual, tipos);

            int oferta = ronda - 1;
            if (oferta < OfertasTotales && indiceEnemigo == enemigoDeOferta[oferta])
            {
                var nueva = new Oferta { numero = oferta + 1, opciones = Opciones(calidadDeOferta[oferta]) };
                foreach (var j in jugadores) j.pendientes.Enqueue(nueva);
            }
        }

        IEnumerator SiguienteEnemigo()
        {
            indiceEnemigo++;
            if (indiceEnemigo >= 3)
            {
                indiceEnemigo = 0;
                if (ronda >= RondasTotales)
                {
                    yield return Aviso("Fin de la partida", "¡A CONTAR!", PaletaUI.Oro, null);
                    // la lógica de fin de partida es del integrante 3; aquí solo se pasan los puntos
                    ResultadoPartida.Guardar(jugadores[0].porRonda, jugadores[1].porRonda);
                    UnityEngine.SceneManagement.SceneManager.LoadScene(escenaResultados);
                    yield break;
                }
                yield return Aviso("Ronda superada", "RONDA " + (ronda + 1), PaletaUI.Oro, null);
                ronda++;
            }
            NuevoEnemigo();
            Refrescar();
            yield return enemigo.Entrar();
        }

        /// <summary>Para el Perfil: el daño se reparte entre los tipos de las cartas jugadas.</summary>
        void RegistrarEstilo(List<DatosCarta> cartas, int dano)
        {
            float suma = 0;
            foreach (var c in cartas) suma += Mathf.Max(1, c.dano);
            foreach (var c in cartas) Estadisticas.SumarDano(activo, c.tipo, Mathf.RoundToInt(dano * Mathf.Max(1, c.dano) / suma));
        }

        // ---------- Buffs y debuffs del enemigo ----------

        static bool TieneElemento(List<CardView> cartas, string elemento) =>
            cartas.Exists(c => c.Datos.Es(DatosCarta.Hechizo)
                && System.Array.IndexOf(CartasEspeciales.Hechizos[c.Datos.indice - 1].elementos, elemento) >= 0);

        /// <summary>Pone el estado (o le renueva los turnos), con su nombre y sus flechas.</summary>
        IEnumerator AplicarEstado(DatosEstado d)
        {
            var e = estados.Find(x => x.datos == d);
            if (e == null)
            {
                e = new EstadoActivo { datos = d };
                estados.Add(e);
            }
            e.turnos = d.turnos;
            enemigo.MostrarEstados(estados, e);
            enemigo.Anunciar(d);
            yield return enemigo.Flechas(d.esBuff);
        }

        /// <summary>Pasa un turno: la Quemadura quema y los estados con turnos se gastan.</summary>
        IEnumerator PasarTurnoEstados()
        {
            foreach (var e in estados)
            {
                if (e.datos.danoPorTurno <= 0 || vidaEnemigo <= 1) continue;
                // quema pero no mata: el golpe final lo da un jugador
                int dano = Mathf.Min(vidaEnemigo - 1, Mathf.RoundToInt(actual.vida * e.datos.danoPorTurno));
                vidaEnemigo -= dano;
                if (vidaEnemigo <= actual.vida * actual.umbralEstadoCartas) tocoUmbral = true;
                enemigo.Impacto(dano, PaletaUI.Rojo, new Vector2(0, 0.3f), EnemyPanel.Efecto.Fuego);
                Refrescar();
                yield return new WaitForSeconds(0.5f);
            }
            foreach (var e in estados) if (e.turnos > 0) e.turnos--;
            estados.RemoveAll(e => e.datos.turnos > 0 && e.turnos <= 0);
            enemigo.MostrarEstados(estados);
        }

        /// <summary>
        /// La primera vez que la vida del enemigo baja del umbral (la mitad), lanza su estado
        /// de cartas. Se revisa después de cada jugada y al empezar cada turno (por la Quemadura).
        /// </summary>
        IEnumerator RevisarEstadoCartas()
        {
            if (estadoCartasLanzado || !tocoUmbral || actual.estadoCartas == null || vidaEnemigo <= 0) yield break;
            estadoCartasLanzado = true;
            yield return EstadoEnCartas(actual.estadoCartas);
        }

        /// <summary>
        /// El enemigo lanza un estado a todas las cartas de la mano (Penitencia): ataca, grita
        /// el nombre en rojo y el efecto cae en cada carta, una tras otra. Esas cartas quedan
        /// marcadas hasta el final del combate; las que lleguen después a la mano no lo tienen.
        /// </summary>
        IEnumerator EstadoEnCartas(DatosEstado d)
        {
            // que termine de repartir
            yield return new WaitForSeconds(0.4f);
            var candidatas = manoCartas.CartasEnMano().FindAll(c => !c.Datos.EsArcano);
            if (candidatas.Count == 0) yield break;
            var j = Actual;
            foreach (var c in candidatas)
            {
                if (EsNormal(c.Datos)) j.marcadas.Add(c.Datos);
                else j.especialesMarcadas.Add(Clave(c.Datos));
            }

            yield return enemigo.Lanzar(d);
            foreach (var c in candidatas)
            {
                StartCoroutine(c.RecibirEstado(d, enemigo.EfectoDebuff));
                yield return new WaitForSeconds(0.15f);
            }
            yield return new WaitForSeconds(0.6f);
            string cartas = candidatas.Count == 1 ? "1 carta" : $"{candidatas.Count} cartas";
            StartCoroutine(AvisarPista($"{d.nombre}: {cartas} {d.resumen}", d.Color));
            Refrescar();
        }

        /// <summary>
        /// La primera vez que llega a 0: se cura (cruces verdes), gana su buff hasta el final
        /// del combate y avisa. La segunda vez ya muere.
        /// </summary>
        IEnumerator Levantarse()
        {
            seLevanto = true;
            yield return new WaitForSeconds(0.4f);
            int desde = Mathf.Max(0, vidaEnemigo);
            int hasta = Mathf.RoundToInt(actual.vida * actual.curaAlLevantarse);
            vidaEnemigo = hasta;
            yield return enemigo.Curar(hasta - desde, desde, hasta, actual.vida);
            if (actual.estadoAlLevantarse != null) yield return AplicarEstado(actual.estadoAlLevantarse);
            string buff = actual.estadoAlLevantarse != null ? " y gana " + actual.estadoAlLevantarse.nombre : "";
            yield return Aviso($"{actual.nombre} se levanta{buff}", "¡NO CAE!", PaletaUI.Menta, null);
        }

        // ---------- Turno ----------

        IEnumerator CambioDeTurno()
        {
            if (ocupado) yield break;
            ocupado = true;
            manoCartas.Cancelar();
            foreach (var c in manoCartas.CartasEnZona()) manoCartas.MoverAMano(c);
            Refrescar();

            // pasa un turno para los buffs y debuffs; después actúa el enemigo
            yield return PasarTurnoEstados();
            bool bloquea = actual.habilidad == "bloqueo" && Random.value < ProbabilidadBloqueo;
            enemigo.Actuar(bloquea ? "bloqueo" : "ataque");
            enemigo.MostrarEscudo(bloquea);
            if (bloquea && estadoEscudo != null) StartCoroutine(AplicarEstado(estadoEscudo));

            // Todo o nada: pasar sin jugar nada también cuenta
            bool nada = Actual.Tiene("todo") && !Actual.jugoEsteTurno;
            if (nada) Actual.todoNada++;
            int robado = 0;
            if (nada) yield return Aviso("Todo o nada · pasaste sin jugar", $"¡NADA! x{MultiplicadorTodoNada(Actual):0.0}", PaletaUI.Oro, null, 16);

            int siguiente = activo == 1 ? 2 : 1;
            string texto = bloquea ? $"{actual.nombre} se protege · turno de" : "El enemigo actúa · turno de";
            // deja ver la animación del enemigo antes de tapar
            yield return new WaitForSeconds(0.6f);
            yield return pasaDispositivo.Mostrar(texto, siguiente, jugadores[0].puntos, jugadores[1].puntos,
                $"RONDA {ronda}/{RondasTotales}", () =>
            {
                // ya con la pantalla tapada: así no se alcanza a ver la mano del otro
                var j = Actual;
                j.reserva = Mathf.Min(ManaBar.MaxReserva, j.reserva + j.mana);
                j.mana = 0;
                activo = siguiente;
                var s = Actual;
                s.maxMana = Mathf.Min(ManaBar.MaxMana, s.maxMana + (s.Tiene("rapido") ? 2 : 1));
                s.mana = ManaMaxima(s);
                if (s.Tiene("ahorro")) s.reserva = Mathf.Min(ManaBar.MaxReserva, s.reserva + 1);
                s.jugoEsteTurno = false;
                if (s.Tiene("arcanos")) DarArcano(s, false);
                if (s.Tiene("prohibidos")) robado = TirarHechizo(s);
                RepartirMano();
                Refrescar();
            });
            if (robado > 0)
                yield return Aviso("Hechizo prohibido", CartasEspeciales.Hechizos[robado - 1].nombre.ToUpperInvariant(), PaletaUI.VioletaReserva, null);
            // si la Quemadura lo bajó del umbral al pasar el turno, la Penitencia cae en la mano nueva
            yield return RevisarEstadoCartas();
            yield return OfrecerPendientes();
            ocupado = false;
            Refrescar();
        }

        // ---------- Aumentos ----------

        void PrepararAumentos()
        {
            azar = new System.Random(Random.Range(int.MinValue, int.MaxValue));
            for (int i = 0; i < OfertasTotales; i++) enemigoDeOferta[i] = azar.Next(3);
            bool plataPrimero = azar.Next(2) == 0;
            calidadDeOferta[0] = plataPrimero ? CalidadAumento.Plata : CalidadAumento.Oro;
            calidadDeOferta[1] = plataPrimero ? CalidadAumento.Oro : CalidadAumento.Plata;
            calidadDeOferta[2] = CalidadAumento.Prismatico;
        }

        /// <summary>Tres aumentos distintos de esa calidad; la misma oferta va a los dos jugadores.</summary>
        List<DatosAumento> Opciones(CalidadAumento calidad)
        {
            var lista = new List<DatosAumento>(calidad switch
            {
                CalidadAumento.Plata => aumentosPlata,
                CalidadAumento.Oro => aumentosOro,
                _ => aumentosPrismaticos,
            });
            for (int i = lista.Count - 1; i > 0; i--)
            {
                int k = azar.Next(i + 1);
                (lista[i], lista[k]) = (lista[k], lista[i]);
            }
            return lista.GetRange(0, Mathf.Min(3, lista.Count));
        }

        IEnumerator OfrecerPendientes()
        {
            var j = Actual;
            while (j.pendientes.Count > 0)
            {
                var oferta = j.pendientes.Dequeue();
                manoCartas.Cancelar();
                DatosAumento elegido = null;
                yield return pantallaAumentos.Elegir(activo, oferta.opciones, oferta.numero, OfertasTotales,
                    a => elegido = a);
                j.aumentos.Add(elegido);
                if (elegido.id == "arcanos") DarArcano(j, true);
                if (elegido.id == "mano") manoCartas.Agregar(NuevaCarta());
                if (elegido.id == "raawr") yield return RaawrMerica(j);
                if (elegido.id == "poker") PasarAPoker();
                Refrescar();
            }
        }

        IEnumerator FormarTrio(Jugador j, List<DatosCarta> trio)
        {
            j.trios++;
            // lo que da este trío, por familia (el total acumulado queda bajo el maná)
            var ganado = new Dictionary<string, int>();
            foreach (var a in trio)
            {
                ganado[a.tipo] = (ganado.TryGetValue(a.tipo, out var n) ? n : 0) + 1;
                if (a.tipo == ArcanosMayores.FamiliaMana) j.manaExtra++;
                else j.bonoTipo[a.tipo] = (j.bonoTipo.TryGetValue(a.tipo, out var b) ? b : 0) + BonoTrio;
            }
            var lineas = new List<string>();
            foreach (var par in ganado)
                lineas.Add(par.Key == ArcanosMayores.FamiliaMana ? $"+{par.Value} maná por turno"
                    : $"+{Mathf.RoundToInt(par.Value * BonoTrio * 100)}% {NombreFamilia(par.Key)}");
            yield return Aviso($"Trío arcano {j.trios}/{MaxTrios} · bono permanente",
                string.Join("\n", lineas).ToUpperInvariant(), PaletaUI.Oro, null, 16);
        }

        IEnumerable<string> Bonos(Jugador j)
        {
            if (j.manaExtra > 0) yield return $"+{j.manaExtra} maná";
            foreach (var par in j.bonoTipo) yield return $"+{Mathf.RoundToInt(par.Value * 100)}% {NombreFamilia(par.Key)}";
        }

        /// <summary>Tapa la pantalla con un aviso grande; con la pantalla tapada corre "mientras".</summary>
        IEnumerator Aviso(string texto, string grande, Color color, System.Action mientras, int tamano = 24)
        {
            avisoTurno.blocksRaycasts = true;
            avisoTexto.text = texto;
            avisoJugador.text = grande;
            // a 16 px puede ir en varias líneas: crecen hacia abajo desde debajo del texto chico
            avisoJugador.fontSize = tamano;
            avisoJugador.verticalAlignment = tamano < 24 ? VerticalAlignmentOptions.Top : VerticalAlignmentOptions.Middle;
            avisoJugador.color = color;
            // deja ver la animación del enemigo antes de tapar
            yield return new WaitForSeconds(0.6f);
            for (float t = 0; t < 0.15f; t += Time.deltaTime) { avisoTurno.alpha = t / 0.15f; yield return null; }
            avisoTurno.alpha = 1;
            mientras?.Invoke();
            yield return new WaitForSeconds(1.1f);
            for (float t = 0; t < 0.2f; t += Time.deltaTime) { avisoTurno.alpha = 1f - t / 0.2f; yield return null; }
            avisoTurno.alpha = 0;
            avisoTurno.blocksRaycasts = false;
        }
    }
}
