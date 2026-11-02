using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Un buff o debuff que lleva el enemigo: ícono (de Spire of Ash), qué hace y cuánto dura.
    /// Se ve en la fila de estados bajo el enemigo y al pararse encima sale su explicación.
    /// Valores provisionales hasta que el integrante 1 defina buffs y debuffs.
    /// </summary>
    [CreateAssetMenu(menuName = "RiftDeck/Estado", fileName = "Estado")]
    public class DatosEstado : ScriptableObject
    {
        public string nombre;
        [TextArea] public string descripcion;
        public Sprite icono;
        [Tooltip("Buff (flechas que suben, Menta) o debuff (flechas que bajan, Rosa)")]
        public bool esBuff = true;
        [Tooltip("Turnos que dura; 0 = hasta el final del combate")]
        public int turnos;
        [Tooltip("Multiplica el daño que recibe el enemigo (0.5 = la mitad)")]
        public float danoRecibido = 1f;
        [Tooltip("Anula todo el daño mientras dure (el escudo del Caballero)")]
        public bool anulaDano;
    }

    /// <summary>Un estado puesto en el enemigo, con los turnos que le quedan.</summary>
    public class EstadoActivo
    {
        public DatosEstado datos;
        /// <summary>0 = hasta el final del combate.</summary>
        public int turnos;

        public string Duracion => turnos <= 0 ? "Dura hasta el final del combate."
            : turnos == 1 ? "Queda 1 turno." : $"Quedan {turnos} turnos.";
    }
}
