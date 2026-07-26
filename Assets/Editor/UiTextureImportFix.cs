using System.IO;
using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    /// <summary>
    /// Tightens import settings for small UI sprites under Resources/UI.
    /// </summary>
    public static class UiTextureImportFix
    {
        const string UiRoot = "Assets/Resources/UI";
        const int UiMaxSize = 256;

        [MenuItem("PeopleOfMath/Fix UI Texture Import (Sprite)")]
        public static void FixAll()
        {
            if (!Directory.Exists(UiRoot))
            {
                Debug.LogWarning("No Resources/UI folder.");
                return;
            }

            var count = 0;
            foreach (var file in Directory.GetFiles(UiRoot, "*.*", SearchOption.TopDirectoryOnly))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is not (".png" or ".jpg" or ".jpeg"))
                    continue;

                var assetPath = file.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                    continue;

                ApplyUiImportSettings(importer);
                importer.SaveAndReimport();
                count++;
            }

            Debug.Log($"Reimported {count} UI textures (max {UiMaxSize}, crunch on).");
        }

        internal static void ApplyUiImportSettings(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = UiMaxSize;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.compressionQuality = 50;
            importer.crunchedCompression = true;
            importer.alphaIsTransparency = true;

            ApplyPlatform(importer, "DefaultTexturePlatform", overridden: false);
            ApplyPlatform(importer, "Android", overridden: true);
            ApplyPlatform(importer, "iPhone", overridden: true);
            ApplyPlatform(importer, "WebGL", overridden: true);
            ApplyPlatform(importer, "Standalone", overridden: true);
        }

        static void ApplyPlatform(TextureImporter importer, string platform, bool overridden)
        {
            var ps = new TextureImporterPlatformSettings
            {
                name = platform,
                overridden = overridden,
                maxTextureSize = UiMaxSize,
                resizeAlgorithm = TextureResizeAlgorithm.Bilinear,
                format = TextureImporterFormat.Automatic,
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = 50,
                crunchedCompression = true,
                allowsAlphaSplitting = false,
            };
            importer.SetPlatformTextureSettings(ps);
        }
    }
}
