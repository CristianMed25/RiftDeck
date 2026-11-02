using System;
using System.Collections.Generic;

namespace RiftDeck.Combate
{
    public sealed class JugadorModel
    {
        readonly List<CartaSO> mano = new List<CartaSO>();

        public JugadorModel(int idJugador)
        {
            if (idJugador != 1 && idJugador != 2)
                throw new ArgumentOutOfRangeException(nameof(idJugador), "El jugador debe ser 1 o 2.");
            IdJugador = idJugador;
        }

        public int IdJugador { get; }
        public int PuntajeAcumulado { get; private set; }
        public IReadOnlyList<CartaSO> Mano => mano;

        public void AgregarCarta(CartaSO carta)
        {
            if (carta == null) throw new ArgumentNullException(nameof(carta));
            mano.Add(carta);
        }

        public bool UsarCarta(CartaSO carta)
        {
            if (carta == null) throw new ArgumentNullException(nameof(carta));
            return mano.Remove(carta);
        }

        public void AgregarPuntos(int puntos)
        {
            if (puntos < 0) throw new ArgumentOutOfRangeException(nameof(puntos));
            PuntajeAcumulado += puntos;
        }
    }
}
