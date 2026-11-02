using System;
using UnityEngine;
using RiftDeck.UI;

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

        public void EstablecerMultiplicador(TipoDano tipo, float multiplicador)
        {
            if (multiplicador < 0f)
                throw new ArgumentOutOfRangeException(nameof(multiplicador));
            multiplicadores[(int)tipo] = multiplicador;
        }

        public static ResistenciasData DesdeEnemigo(DatosEnemigo enemigo)
        {
            if (enemigo == null) throw new ArgumentNullException(nameof(enemigo));

            var datos = CreateInstance<ResistenciasData>();
            foreach (TipoDano tipo in Enum.GetValues(typeof(TipoDano)))
                datos.EstablecerMultiplicador(tipo, enemigo.Multiplicador(tipo.ToString()));
            return datos;
        }
    }
}
