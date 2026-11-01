using System;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Reproduce animaciones cuadro a cuadro en una Image de UI. El primer clip es el
    /// de reposo y se repite; los demás se reproducen una vez y vuelven al reposo
    /// (salvo los que no tienen "volver", como la muerte).
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class AnimacionSprites : MonoBehaviour
    {
        [Serializable]
        public class Clip
        {
            public string nombre;
            public Sprite[] cuadros;
            public float fps = 10;
            public bool volver = true;
        }

        [SerializeField] Clip[] clips;

        UnityEngine.UI.Image imagen;
        Clip actual;
        float tiempo;

        void Awake()
        {
            imagen = GetComponent<UnityEngine.UI.Image>();
            if (clips != null && clips.Length > 0) actual = clips[0];
        }

        /// <summary>Cambia todas las animaciones (al cambiar de enemigo).</summary>
        public void Cargar(Clip[] nuevos)
        {
            if (imagen == null) imagen = GetComponent<UnityEngine.UI.Image>();
            clips = nuevos;
            actual = clips != null && clips.Length > 0 ? clips[0] : null;
            tiempo = 0;
            if (actual != null && actual.cuadros.Length > 0) imagen.sprite = actual.cuadros[0];
        }

        public void Reproducir(string nombre)
        {
            var clip = Array.Find(clips, c => c.nombre == nombre);
            if (clip == null) return;
            actual = clip;
            tiempo = 0;
        }

        /// <summary>Segundos que dura un clip (0 si no existe).</summary>
        public float Duracion(string nombre)
        {
            var clip = Array.Find(clips, c => c.nombre == nombre);
            return clip == null || clip.fps <= 0 ? 0 : clip.cuadros.Length / clip.fps;
        }

        void Update()
        {
            if (actual == null || actual.cuadros.Length == 0) return;
            tiempo += Time.deltaTime;
            int cuadro = Mathf.FloorToInt(tiempo * actual.fps);
            if (cuadro >= actual.cuadros.Length)
            {
                if (actual != clips[0] && actual.volver) { Reproducir(clips[0].nombre); cuadro = 0; }
                else if (actual == clips[0]) { tiempo %= actual.cuadros.Length / actual.fps; cuadro = Mathf.FloorToInt(tiempo * actual.fps) % actual.cuadros.Length; }
                else cuadro = actual.cuadros.Length - 1;
            }
            imagen.sprite = actual.cuadros[cuadro];
        }
    }
}
