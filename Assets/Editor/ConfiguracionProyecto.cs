using UnityEditor;
using UnityEngine;

namespace RiftDeck.EditorTools
{
    /// <summary>
    /// Deja el proyecto con la configuración acordada: horizontal, texto serializado
    /// para git y sin antialiasing (el pixel art se ve borroso con AA).
    /// Menú: RiftDeck > Configurar proyecto. También se puede correr en batchmode con
    /// -executeMethod RiftDeck.EditorTools.ConfiguracionProyecto.ConfigurarYSalir
    /// </summary>
    public static class ConfiguracionProyecto
    {
        [MenuItem("RiftDeck/Configurar proyecto")]
        public static void Configurar()
        {
            PlayerSettings.productName = "RiftDeck";
            PlayerSettings.companyName = "Grupo9UNAL";

            // Solo horizontal, pero gira entre izquierda y derecha
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Escenas y prefabs como texto para poder comparar y mezclar en git
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";

            int nivelActual = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.antiAliasing = 0;
            }
            QualitySettings.SetQualityLevel(nivelActual, false);

            AssetDatabase.SaveAssets();
            Debug.Log("RiftDeck: proyecto configurado (horizontal, Force Text, sin AA).");
        }

        public static void ConfigurarYSalir()
        {
            Configurar();
            EditorApplication.Exit(0);
        }
    }
}
