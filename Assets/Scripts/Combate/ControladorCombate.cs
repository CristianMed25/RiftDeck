using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using RiftDeck.Partida;
using RiftDeck.UI;

namespace RiftDeck.Combate
{
    /// <summary>
    /// Adaptador de escena: traduce la selección de la UI al servicio de combate y
    /// devuelve el resultado al flujo local de dos jugadores.
    /// </summary>
    [DisallowMultipleComponent]
    public class ControladorCombate : MonoBehaviour
    {
        [SerializeField] ManoCartas manoCartas;
        [SerializeField] ControladorTurnos controladorTurnos;
        [SerializeField] ResistenciasData resistenciasObjetivo;
        [SerializeField] DatosEnemigo enemigoObjetivo;
        [SerializeField] DatosEnemigo[] catalogoEnemigos;
        [SerializeField] EnemyPanel panelEnemigo;
        [SerializeField] CartasEspeciales efectosAtaque;
        [SerializeField] TiposDano tiposDano;
        [SerializeField] PlayerHUD hudJugador1;
        [SerializeField] PlayerHUD hudJugador2;
        [SerializeField] ManaBar barraMana;
        [SerializeField] MazoInicializador mazoInicializador;
        [SerializeField, Min(1)] int manaMaximo = 6;
        [SerializeField, Min(1)] int tamanoMano = 5;
        [SerializeField, Min(1)] int vidaMaximaEnemigo = 1500;
        [SerializeField] UnityEngine.UI.Button botonJugar;
        [SerializeField] UnityEngine.UI.Button botonSaltarTurno;
        [SerializeField] TMP_Text textoAviso;
        [SerializeField] TMP_Text textoRonda;
        [SerializeField] ControladorFinPartida controladorFinPartida;
        [SerializeField, Min(0.1f)] float duracionAviso = 2f;
        [SerializeField, Min(0)] int vidaJugador1 = 3;
        [SerializeField, Min(0)] int vidaJugador2 = 3;
        [SerializeField] UnityEvent<int> puntuacionActualizada;
        [SerializeField] UnityEvent uiRefrescada;

        readonly JugadorModel[] jugadores = { new JugadorModel(1), new JugadorModel(2) };
        readonly int[] manaActual = new int[2];
        readonly CombatEngine motor = new CombatEngine();
        Coroutine avisoEnCurso;
        int vidaEnemigoActual;

        void Awake()
        {
            if (controladorFinPartida == null)
                controladorFinPartida = GetComponent<ControladorFinPartida>();
            if (manoCartas == null)
                Debug.LogError("ControladorCombate: falta asignar ManoCartas.", this);
            if (controladorTurnos == null)
                Debug.LogError("ControladorCombate: falta asignar ControladorTurnos.", this);
            if (enemigoObjetivo == null && resistenciasObjetivo == null)
                Debug.LogError("ControladorCombate: asigna DatosEnemigo o ResistenciasData.", this);
            if (botonJugar == null)
                Debug.LogError("ControladorCombate: falta asignar el botón JUGAR.", this);
            if (enemigoObjetivo != null && !enemigoObjetivo.TieneSpritePrincipal)
                Debug.LogError($"ControladorCombate: el enemigo '{enemigoObjetivo.name}' no tiene un cuadro de sprite válido.", this);
        }

        void Start()
        {
            enemigoObjetivo = SeleccionarEnemigo();
            vidaMaximaEnemigo = enemigoObjetivo != null ? Mathf.Max(1, enemigoObjetivo.vida) : vidaMaximaEnemigo;
            vidaEnemigoActual = vidaMaximaEnemigo;
            manaActual[0] = manaActual[1] = manaMaximo;
            if (panelEnemigo != null && enemigoObjetivo != null && tiposDano != null)
            {
                panelEnemigo.Efectos = efectosAtaque;
                panelEnemigo.Mostrar(enemigoObjetivo, tiposDano);
                panelEnemigo.MostrarVida(vidaEnemigoActual, vidaMaximaEnemigo);
            }
            RobarYPrepararTurno();
            ActualizarMana();
            ActualizarHud(controladorTurnos != null ? controladorTurnos.Partida : null, 1);
            ActualizarRonda();
        }

        DatosEnemigo SeleccionarEnemigo()
        {
            if (ModoPartida.PrimerEnemigo != null)
            {
                var enemigoPreparado = ModoPartida.PrimerEnemigo;
                ModoPartida.PrimerEnemigo = null;
                return enemigoPreparado;
            }

            if (catalogoEnemigos == null || catalogoEnemigos.Length == 0)
                return enemigoObjetivo;

            var disponibles = new System.Collections.Generic.List<DatosEnemigo>();
            foreach (var enemigo in catalogoEnemigos)
                if (enemigo != null) disponibles.Add(enemigo);
            if (disponibles.Count == 0) return enemigoObjetivo;

            var rng = new System.Random(ModoPartida.Semilla == 0
                ? Environment.TickCount
                : ModoPartida.Semilla);
            return disponibles[rng.Next(disponibles.Count)];
        }

        void OnEnable()
        {
            if (manoCartas != null) manoCartas.PidioElegir += AlElegirCarta;
            if (manoCartas != null) manoCartas.PidioDevolver += AlDevolverCarta;
            if (botonJugar != null) botonJugar.onClick.AddListener(AlJugar);
            if (botonSaltarTurno != null) botonSaltarTurno.onClick.AddListener(SaltarTurno);
        }

        void OnDisable()
        {
            if (manoCartas != null) manoCartas.PidioElegir -= AlElegirCarta;
            if (manoCartas != null) manoCartas.PidioDevolver -= AlDevolverCarta;
            if (botonJugar != null) botonJugar.onClick.RemoveListener(AlJugar);
            if (botonSaltarTurno != null) botonSaltarTurno.onClick.RemoveListener(SaltarTurno);
        }

        void AlElegirCarta(CardView vista)
        {
            if (vista == null || vista.Carta == null)
            {
                Debug.LogError("ControladorCombate: la carta seleccionada no tiene un CartaSO asignado.");
                return;
            }

            if (!manoCartas.EnZona(vista))
            {
                manoCartas.MoverAZona(vista);
                if (CostoZona() > manaActual[JugadorActivo() - 1])
                {
                    manoCartas.MoverAMano(vista);
                    MostrarAviso("¡No tienes suficiente maná!");
                }
                ActualizarMana();
            }
        }

        void AlDevolverCarta(CardView vista)
        {
            if (vista == null || !manoCartas.EnZona(vista)) return;
            manoCartas.MoverAMano(vista);
            ActualizarMana();
        }

        public void AlJugar()
        {
            GestorPartida partida = controladorTurnos != null ? controladorTurnos.Partida : null;
            if (partida == null || !partida.PartidaIniciada || partida.PartidaTerminada)
            {
                Debug.LogWarning("ControladorCombate: no hay una partida activa.");
                return;
            }

            List<CardView> cartas = manoCartas != null ? manoCartas.CartasEnZona() : null;
            if (cartas == null || cartas.Count == 0)
            {
                MostrarAviso("¡Selecciona al menos una carta para jugar!");
                Debug.LogWarning("ControladorCombate: no hay cartas seleccionadas para jugar.");
                return;
            }

            try
            {
                int jugador = partida.JugadorActivo;
                int costoTotal = CostoZona();
                if (costoTotal > manaActual[jugador - 1])
                {
                    MostrarAviso("¡No tienes suficiente maná!");
                    return;
                }
                int puntosTotales = 0;
                foreach (var carta in cartas)
                {
                    if (carta == null || carta.Carta == null)
                        throw new InvalidOperationException("Una carta seleccionada no tiene datos de combate.");

                    ResistenciasData resistencias = CrearResistenciasObjetivo();
                    if (resistencias == null)
                        throw new InvalidOperationException("Falta asignar las resistencias del objetivo.");

                    try
                    {
                        puntosTotales += motor.CalcularPuntos(carta.Carta, resistencias);
                    }
                    finally
                    {
                        if (enemigoObjetivo != null)
                            Destroy(resistencias);
                    }
                }

                jugadores[jugador - 1].AgregarPuntos(puntosTotales);
                partida.SumarPuntos(jugador, puntosTotales);
                manaActual[jugador - 1] -= costoTotal;
                vidaEnemigoActual = Mathf.Max(0, vidaEnemigoActual - puntosTotales);
                if (panelEnemigo != null)
                {
                    panelEnemigo.MostrarVida(vidaEnemigoActual, vidaMaximaEnemigo);
                    panelEnemigo.RecibirGolpe(
                        puntosTotales,
                        PaletaUI.Rojo,
                        false,
                        ObtenerEfectoAtaque(cartas[0].Carta.tipoDano));
                }
                ActualizarHud(partida, jugador);
                ActualizarMana();
                puntuacionActualizada?.Invoke(partida.ObtenerPuntosTotales(jugador));
                manoCartas.DescartarZona();
                StartCoroutine(EsperarGolpeYCambiarTurno(
                    partida,
                    jugador,
                    cartas.Count,
                    puntosTotales,
                    ObtenerEfectoAtaque(cartas[0].Carta.tipoDano)));
            }

            catch (ArgumentException excepcion)
            {
                Debug.LogError($"ControladorCombate: no se pudo resolver la jugada. {excepcion.Message}");
            }
            catch (InvalidOperationException excepcion)
            {
                Debug.LogError($"ControladorCombate: configuración de combate inválida. {excepcion.Message}");
            }
        }

        IEnumerator EsperarGolpeYCambiarTurno(
            GestorPartida partida,
            int jugador,
            int cantidadCartas,
            int dano,
            EnemyPanel.Efecto efecto)
        {
            if (panelEnemigo != null)
                yield return panelEnemigo.EsperarFinGolpe(dano, false, efecto);

            yield return controladorTurnos.CambiarTurno(
                $"Jugador {jugador} jugó {cantidadCartas} carta(s)",
                () =>
                {
                    ActualizarDespuesDeTurno(partida);
                    uiRefrescada?.Invoke();
                },
                RobarYPrepararTurno);
        }

        public void SaltarTurno()
        {
            GestorPartida partida = controladorTurnos != null ? controladorTurnos.Partida : null;
            if (partida == null || !partida.PartidaIniciada || partida.PartidaTerminada)
            {
                Debug.LogWarning("ControladorCombate: no hay una partida activa para saltar turno.");
                return;
            }

            manoCartas.DescartarZona();

            StartCoroutine(controladorTurnos.CambiarTurno(
                $"Jugador {partida.JugadorActivo} pasó el turno",
                () =>
                {
                    ActualizarDespuesDeTurno(partida);
                    uiRefrescada?.Invoke();
                },
                RobarYPrepararTurno));
        }

        void MostrarAviso(string mensaje)
        {
            if (textoAviso == null)
            {
                Debug.LogWarning($"ControladorCombate: no hay texto de aviso asignado. Mensaje: {mensaje}", this);
                return;
            }

            if (avisoEnCurso != null)
                StopCoroutine(avisoEnCurso);
            avisoEnCurso = StartCoroutine(MostrarAvisoTemporal(mensaje));
        }

        System.Collections.IEnumerator MostrarAvisoTemporal(string mensaje)
        {
            textoAviso.text = mensaje;
            textoAviso.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(duracionAviso);
            if (textoAviso != null)
                textoAviso.gameObject.SetActive(false);
            avisoEnCurso = null;
        }

        ResistenciasData CrearResistenciasObjetivo()
        {
            if (enemigoObjetivo == null)
                return resistenciasObjetivo;

            var datos = ScriptableObject.CreateInstance<ResistenciasData>();
            foreach (TipoDano tipo in Enum.GetValues(typeof(TipoDano)))
                datos.EstablecerMultiplicador(tipo, enemigoObjetivo.Multiplicador(tipo.ToString()));
            return datos;
        }

        void ActualizarHud(GestorPartida partida, int jugadorActivo)
        {
            if (partida == null) return;
            if (hudJugador1 != null)
                hudJugador1.Mostrar(1, vidaJugador1, partida.ObtenerPuntosTotales(1), jugadorActivo == 1);
            if (hudJugador2 != null)
                hudJugador2.Mostrar(2, vidaJugador2, partida.ObtenerPuntosTotales(2), jugadorActivo == 2);
        }

        void ActualizarDespuesDeTurno(GestorPartida partida)
        {
            if (!partida.PartidaTerminada)
            {
                manaActual[partida.JugadorActivo - 1] = manaMaximo;
            }
            ActualizarHud(partida, partida.JugadorActivo);
            ActualizarMana();
            ActualizarRonda();

            if (!partida.PartidaTerminada)
                return;

            if (manoCartas != null) manoCartas.Activa = false;
            if (botonJugar != null) botonJugar.interactable = false;
            if (botonSaltarTurno != null) botonSaltarTurno.interactable = false;
            if (controladorFinPartida == null)
            {
                Debug.LogError("ControladorCombate: falta ControladorFinPartida para cargar Resultados.", this);
                return;
            }

            controladorFinPartida.FinalizarPartida();
        }

        void ActualizarRonda()
        {
            if (textoRonda == null || controladorTurnos == null || controladorTurnos.Partida == null)
                return;

            GestorPartida partida = controladorTurnos.Partida;
            textoRonda.text = $"RONDA {partida.RondaActual}/{GestorPartida.RondasTotales}";
        }

        int JugadorActivo()
        {
            return controladorTurnos != null && controladorTurnos.Partida != null
                ? controladorTurnos.Partida.JugadorActivo
                : 1;
        }

        int CostoZona()
        {
            int total = 0;
            foreach (var carta in manoCartas.CartasEnZona())
                if (carta != null) total += carta.Costo;
            return total;
        }

        void RobarYPrepararTurno()
        {
            if (manoCartas == null) return;
            manoCartas.DescartarMano();
            if (mazoInicializador != null)
                mazoInicializador.RobarHasta(tamanoMano);
        }

        static EnemyPanel.Efecto ObtenerEfectoAtaque(TipoDano tipo)
        {
            switch (tipo)
            {
                case TipoDano.Cortante:
                    return EnemyPanel.Efecto.Fuego;
                case TipoDano.Perforante:
                    return EnemyPanel.Efecto.Bala;
                case TipoDano.Impacto:
                    return EnemyPanel.Efecto.ExplosionGrande;
                case TipoDano.Elemental:
                    return EnemyPanel.Efecto.Rayo;
                default:
                    return EnemyPanel.Efecto.Ninguno;
            }
        }

        void ActualizarMana()
        {
            if (barraMana == null) return;
            int activo = JugadorActivo();
            barraMana.Mostrar(manaActual[activo - 1], manaMaximo, 0, CostoZona());
        }
    }
}
