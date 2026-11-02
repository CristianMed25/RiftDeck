using System;
using UnityEngine;

namespace RiftDeck.Combate
{
    [CreateAssetMenu(menuName = "RiftDeck/Combate/Resistencias", fileName = "Resistencias")]
    public class ResistenciasData : ScriptableObject
    {
        [SerializeField] float[] multiplicadores =
        {
            1f, 1f, 1f, 1f
        };

        public float ObtenerMultiplicador(TipoDano tipo)
        {
            int indice = (int)tipo;
            if (indice < 0 || indice >= multiplicadores.Length)
                throw new ArgumentOutOfRangeException(nameof(tipo), "El tipo de daño no tiene multiplicador.");

            float multiplicador = multiplicadores[indice];
            if (multiplicador < 0f)
                throw new InvalidOperationException("El multiplicador de resistencia no puede ser negativo.");
            return multiplicador;
        }
    }
}
