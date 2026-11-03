using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiftDeck.UI
{
    public enum NivelEnemigo { Normal, Medio, Elite }

    /// <summary>
    /// Lo que la UI necesita de un enemigo: nombre, nivel, vida, resistencias, habilidad
    /// y sus animaciones. Igual que DatosCarta, se adapta al EnemyData del integrante 1
    /// cuando exista.
    /// </summary>
    [CreateAssetMenu(menuName = "RiftDeck/Enemigo", fileName = "Enemigo")]
    public class DatosEnemigo : ScriptableObject
    {
        [Serializable]
        public class Resistencia
        {
            public string tipo;
            public float multiplicador = 1f;
        }

        public string nombre;
        public NivelEnemigo nivel;
        public int vida = 500;
        [Tooltip("Escala entera en pantalla (los cuadros ya vienen recortados)")]
        public int escala = 2;
        [Tooltip("Vacío o \"bloqueo\": el enemigo puede anular el daño del turno siguiente")]
        public string habilidad;
        [Tooltip("La primera vez que su vida llega a 0 se cura y gana un buff; la segunda muere")]
        public bool levantarse;
        [Range(0.1f, 1f)] public float curaAlLevantarse = 0.5f;
        [Tooltip("Buff que gana al levantarse (dura hasta el final del combate)")]
        public DatosEstado estadoAlLevantarse;
        [Tooltip("Debuff que lanza a todas las cartas de la mano (Penitencia del Caballero)")]
        public DatosEstado estadoCartas;
        [Tooltip("Lo lanza una sola vez: la primera vez que su vida baja a esta parte (0.5 = la mitad)")]
        [Range(0.05f, 1f)] public float umbralEstadoCartas = 0.5f;
        public List<Resistencia> resistencias = new List<Resistencia>();
        [Tooltip("El primero es el de reposo. Nombres: idle, ataque, golpe, muerte, bloqueo")]
        public AnimacionSprites.Clip[] clips;

        public float Multiplicador(string tipo)
        {
            var r = resistencias != null ? resistencias.Find(x => x != null && x.tipo == tipo) : null;
            return r != null ? r.multiplicador : 1f;
        }

        public bool TieneSpritePrincipal =>
            clips != null && clips.Length > 0 &&
            clips[0] != null && clips[0].cuadros != null &&
            clips[0].cuadros.Length > 0 && clips[0].cuadros[0] != null;
    }
}
