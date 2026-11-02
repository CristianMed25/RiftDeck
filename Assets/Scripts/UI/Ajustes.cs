using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Opciones de la pantalla Configuración, guardadas en PlayerPrefs. Todo es local:
    /// sin cuenta ni nube. Se aplican al abrir el juego y cada vez que cambian.
    /// </summary>
    public static class Ajustes
    {
        public const int LargoNombre = 8;
        // Baja, Media y Alta en los 6 niveles de calidad del proyecto
        static readonly int[] NivelesCalidad = { 1, 3, 5 };

        public static float Musica
        {
            get => PlayerPrefs.GetFloat("musica", 0.7f);
            set { PlayerPrefs.SetFloat("musica", value); Guardar(); }
        }

        public static float Efectos
        {
            get => PlayerPrefs.GetFloat("efectos", 0.9f);
            set { PlayerPrefs.SetFloat("efectos", value); Guardar(); }
        }

        public static bool Vibracion
        {
            get => PlayerPrefs.GetInt("vibracion", 1) == 1;
            set { PlayerPrefs.SetInt("vibracion", value ? 1 : 0); Guardar(); }
        }

        /// <summary>0 Baja, 1 Media, 2 Alta.</summary>
        public static int Calidad
        {
            get => PlayerPrefs.GetInt("calidad", 1);
            set { PlayerPrefs.SetInt("calidad", value); Guardar(); }
        }

        /// <summary>x1 o x2: la escala de tiempo del Combate.</summary>
        public static int Velocidad
        {
            get => PlayerPrefs.GetInt("velocidad", 1);
            set { PlayerPrefs.SetInt("velocidad", value); Guardar(); }
        }

        /// <summary>Nombre escrito por el jugador ("" si no puso ninguno).</summary>
        public static string NombrePropio(int jugador) => PlayerPrefs.GetString("nombre_j" + jugador, "");

        public static void CambiarNombre(int jugador, string nombre)
        {
            nombre = nombre.Trim();
            if (nombre.Length > LargoNombre) nombre = nombre.Substring(0, LargoNombre);
            PlayerPrefs.SetString("nombre_j" + jugador, nombre);
            PlayerPrefs.Save();
        }

        /// <summary>Para textos grandes: el nombre propio o "Jugador N".</summary>
        public static string Nombre(int jugador)
        {
            var propio = NombrePropio(jugador);
            return propio != "" ? propio : "Jugador " + jugador;
        }

        /// <summary>Para el HUD y las tablas: el nombre propio o "JN".</summary>
        public static string NombreCorto(int jugador)
        {
            var propio = NombrePropio(jugador);
            return propio != "" ? propio : "J" + jugador;
        }

        public static void Vibrar()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Vibracion) Handheld.Vibrate();
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Aplicar()
        {
            int nivel = NivelesCalidad[Mathf.Clamp(Calidad, 0, NivelesCalidad.Length - 1)];
            if (nivel < QualitySettings.names.Length) QualitySettings.SetQualityLevel(nivel, true);
            // todavía no hay música: los efectos mandan el volumen general
            AudioListener.volume = Efectos;
        }

        /// <summary>Borra el oro y los cosméticos; conserva las opciones y los nombres.</summary>
        public static void BorrarProgreso()
        {
            float musica = Musica, efectos = Efectos;
            bool vibracion = Vibracion;
            int calidad = Calidad, velocidad = Velocidad;
            string j1 = NombrePropio(1), j2 = NombrePropio(2);
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetFloat("musica", musica);
            PlayerPrefs.SetFloat("efectos", efectos);
            PlayerPrefs.SetInt("vibracion", vibracion ? 1 : 0);
            PlayerPrefs.SetInt("calidad", calidad);
            PlayerPrefs.SetInt("velocidad", velocidad);
            PlayerPrefs.SetString("nombre_j1", j1);
            PlayerPrefs.SetString("nombre_j2", j2);
            Inventario.Recargar();
        }

        static void Guardar()
        {
            PlayerPrefs.Save();
            Aplicar();
        }
    }
}
