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
        public List<Resistencia> resistencias = new List<Resistencia>();
        [Tooltip("El primero es el de reposo. Nombres: idle, ataque, golpe, muerte, bloqueo")]
        public AnimacionSprites.Clip[] clips;

        public float Multiplicador(string tipo)
        {
            var r = resistencias.Find(x => x.tipo == tipo);
            return r != null ? r.multiplicador : 1f;
        }
    }
}
