using UnityEditor;
using UnityEngine;

namespace RiftDeck.EditorTools
{
    /// <summary>
    /// Toda imagen que entre a Assets/Art se importa como pixel art: filtro Point,
    /// sin compresión, sin mipmaps y 16 px por unidad. Solo actúa la primera vez,
    /// así que si alguien cambia un ajuste a mano (por ejemplo, Sprite Mode Multiple
    /// para un spritesheet) no se lo pisa.
    /// </summary>
    public class ImportadorPixelArt : AssetPostprocessor
    {
        public const int PixelesPorUnidad = 16;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/")) return;

            var importador = (TextureImporter)assetImporter;
            if (!importador.importSettingsMissing) return;

            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.spritePixelsPerUnit = PixelesPorUnidad;
            importador.filterMode = FilterMode.Point;
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.mipmapEnabled = false;
            importador.alphaIsTransparency = true;
            importador.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
