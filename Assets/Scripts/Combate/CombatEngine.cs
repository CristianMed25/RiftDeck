using System;

namespace RiftDeck.Combate
{
    /// <summary>
    /// Servicio puro de cálculo. No conoce escenas, jugadores ni componentes de Unity.
    /// </summary>
    public sealed class CombatEngine
    {
        public int CalcularPuntos(CartaSO carta, ResistenciasData resistencias)
        {
            if (carta == null) throw new ArgumentNullException(nameof(carta));
            if (resistencias == null) throw new ArgumentNullException(nameof(resistencias));
            if (carta.valorBase < 0)
                throw new ArgumentOutOfRangeException(nameof(carta.valorBase), "El valor base no puede ser negativo.");

            double puntos = carta.valorBase * resistencias.ObtenerMultiplicador(carta.tipoDano);
            return Convert.ToInt32(Math.Round(puntos, MidpointRounding.AwayFromZero));
        }
    }
}
