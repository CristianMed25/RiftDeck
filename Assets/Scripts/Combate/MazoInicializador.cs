using UnityEngine;
using UnityEngine.UI;
using RiftDeck.UI;
using System;
using System.Collections.Generic;

namespace RiftDeck.Combate
{
    /// <summary>
    /// Reparte un mazo mínimo cuando la escena aún no tiene un generador de mazos.
    /// Todas las referencias se asignan desde Inspector; no depende de rutas de disco.
    /// </summary>
    public sealed class MazoInicializador : MonoBehaviour
    {
        [SerializeField] ManoCartas manoCartas;
        [SerializeField] CardView prefabCarta;
        [SerializeField] TiposDano tiposDano;
        [SerializeField] CartaSO[] cartasIniciales = new CartaSO[5];
        [SerializeField] CartaSO[] catalogoCartas;
        [SerializeField] bool usarCatalogoParaLaMano = true;
        [SerializeField] int cartasPorMano = 5;
        [Header("Visual del mazo")]
        [SerializeField] Image imagenReversoMazo;
        [SerializeField] DatosCosmetico[] dorsos;
        [SerializeField] Sprite reversoPorDefecto;
        readonly List<CartaSO> mazo = new List<CartaSO>();
        System.Random generador;

        void Start()
        {
            if (manoCartas == null || prefabCarta == null || tiposDano == null)
            {
                Debug.LogError("MazoInicializador: asigna ManoCartas, prefab de carta y TiposDano.", this);
                return;
            }

            ConfigurarVisualMazo();
            PrepararMazo();

            if (manoCartas.CartasEnMano().Count > 0)
                return;

            int repartidas = RobarHasta(cartasPorMano);
            if (repartidas == 0)
                Debug.LogError("MazoInicializador: no se pudo repartir ninguna carta inicial.", this);
        }

        void PrepararMazo()
        {
            mazo.Clear();
            var cartas = catalogoCartas != null && catalogoCartas.Length > 0
                ? catalogoCartas
                : cartasIniciales;
            for (int i = 0; i < 8; i++)
                foreach (var carta in cartas)
                    if (carta != null) mazo.Add(carta);

            generador = new System.Random(ModoPartida.Semilla == 0
                ? Environment.TickCount
                : ModoPartida.Semilla);
        }

        /// <summary>Roba cartas hasta completar el tamaño solicitado de la mano.</summary>
        public int RobarHasta(int cantidad)
        {
            if (manoCartas == null || prefabCarta == null || tiposDano == null) return 0;
            int robadas = 0;
            while (manoCartas.CartasEnMano().Count < cantidad && mazo.Count > 0)
            {
                int indice = generador.Next(mazo.Count);
                var datos = mazo[indice];
                mazo.RemoveAt(indice);
                var carta = Instantiate(prefabCarta);
                carta.BindCarta(datos, tiposDano);
                manoCartas.Agregar(carta);
                robadas++;
            }

            if (imagenReversoMazo != null)
                imagenReversoMazo.gameObject.SetActive(mazo.Count > 0);
            return robadas;
        }

        void ConfigurarVisualMazo()
        {
            if (imagenReversoMazo == null)
            {
                Debug.LogWarning("MazoInicializador: no hay Image asignada para el reverso del mazo.", this);
                return;
            }

            Sprite reverso = reversoPorDefecto;
            if (dorsos != null && dorsos.Length > 0)
            {
                string equipado = Inventario.Equipado(CategoriaCosmetico.Dorso);
                foreach (var dorso in dorsos)
                {
                    if (dorso != null && dorso.id == equipado)
                    {
                        reverso = dorso.vista;
                        break;
                    }
                }

                if (reverso == null)
                    foreach (var dorso in dorsos)
                        if (dorso != null && dorso.vista != null)
                        {
                            reverso = dorso.vista;
                            break;
                        }
            }

            if (reverso == null)
            {
                Debug.LogWarning("MazoInicializador: no se encontró un sprite de reverso válido.", this);
                return;
            }

            imagenReversoMazo.sprite = reverso;
            imagenReversoMazo.enabled = true;
            imagenReversoMazo.gameObject.SetActive(true);
            foreach (var grafico in imagenReversoMazo.GetComponentsInChildren<Graphic>(true))
                grafico.raycastTarget = false;
        }

        CartaSO[] ObtenerCartasDeLaPartida()
        {
            if (!usarCatalogoParaLaMano || catalogoCartas == null || catalogoCartas.Length == 0)
                return cartasIniciales ?? Array.Empty<CartaSO>();

            var disponibles = new System.Collections.Generic.List<CartaSO>();
            foreach (var carta in catalogoCartas)
                if (carta != null) disponibles.Add(carta);

            if (disponibles.Count == 0)
                return Array.Empty<CartaSO>();

            var resultado = new CartaSO[Mathf.Max(1, cartasPorMano)];
            var rng = new System.Random(ModoPartida.Semilla == 0
                ? Environment.TickCount
                : ModoPartida.Semilla);
            for (int i = 0; i < resultado.Length; i++)
                resultado[i] = disponibles[rng.Next(disponibles.Count)];
            return resultado;
        }
    }
}
