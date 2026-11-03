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

        void OnValidate()
        {
            NormalizarMultiplicadores();
        }

        public float ObtenerMultiplicador(TipoDano tipo)
        {
            NormalizarMultiplicadores();
            int indice = (int)tipo;
            if (!Enum.IsDefined(typeof(TipoDano), tipo) || indice >= multiplicadores.Length)
                throw new ArgumentOutOfRangeException(nameof(tipo), "El tipo de daño no tiene multiplicador.");

            float multiplicador = multiplicadores[indice];
            if (multiplicador < 0f)
                throw new InvalidOperationException("El multiplicador de resistencia no puede ser negativo.");
            return multiplicador;
        }

        public void EstablecerMultiplicador(TipoDano tipo, float multiplicador)
        {
            if (!Enum.IsDefined(typeof(TipoDano), tipo))
                throw new ArgumentOutOfRangeException(nameof(tipo));
            if (multiplicador < 0f)
                throw new ArgumentOutOfRangeException(nameof(multiplicador));
            NormalizarMultiplicadores();
            multiplicadores[(int)tipo] = multiplicador;
        }

        void NormalizarMultiplicadores()
        {
            int cantidadTipos = Enum.GetValues(typeof(TipoDano)).Length;
            if (multiplicadores == null || multiplicadores.Length != cantidadTipos)
            {
                var anteriores = multiplicadores;
                multiplicadores = new float[cantidadTipos];
                for (int i = 0; i < multiplicadores.Length; i++)
                    multiplicadores[i] = anteriores != null && i < anteriores.Length ? anteriores[i] : 1f;
            }
        }
    }
}
