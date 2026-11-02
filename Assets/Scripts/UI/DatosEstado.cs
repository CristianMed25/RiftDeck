using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Un buff o debuff: ícono (de Spire of Ash), qué hace y cuánto dura. Puede ir en el
    /// enemigo (fila de estados bajo su barra) o en las cartas de la mano (Penitencia).
    /// Al pararse encima sale su explicación. La tabla completa está en el README.
    /// Valores provisionales hasta que el integrante 1 defina buffs y debuffs.
    /// </summary>
    [CreateAssetMenu(menuName = "RiftDeck/Estado", fileName = "Estado")]
    public class DatosEstado : ScriptableObject
    {
        public string nombre;
        [TextArea] public string descripcion;
        [Tooltip("Lo corto que sale en el cuadro de la carta: \"Penitencia = +1 de costo\"")]
        public string resumen;
        public Sprite icono;
        [Tooltip("Buff (flechas que suben, Menta) o debuff (flechas que bajan, Rosa)")]
        public bool esBuff = true;
        [Tooltip("Turnos que dura; 0 = hasta el final del combate")]
        public int turnos;

        [Header("En el enemigo")]
        [Tooltip("Multiplica el daño que recibe el enemigo (0.5 = la mitad)")]
        public float danoRecibido = 1f;
        [Tooltip("Anula todo el daño mientras dure (el escudo del Caballero)")]
        public bool anulaDano;
        [Tooltip("Al pasar cada turno pierde esta parte de su vida máxima (0.05 = 5 %); nunca lo mata")]
        public float danoPorTurno;

        [Header("En las cartas")]
        [Tooltip("Va en las cartas de la mano y no en el enemigo; dura hasta que se juegan o acaba el turno")]
        public bool enCartas;
        [Tooltip("Cuánto sube (o baja, si es negativo) el costo de la carta")]
        public int costoExtra;

        /// <summary>Rojo en las cartas, Menta los buffs y Rosa los debuffs del enemigo.</summary>
        public Color Color => enCartas ? PaletaUI.Rojo : esBuff ? PaletaUI.Menta : PaletaUI.Rosa;
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
