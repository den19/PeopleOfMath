using System.IO;
using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class PortraitTextureImportFix
    {
        const string ResourcesRoot = "Assets/Resources/Portraits";
        const int PortraitMaxSize = 512;

        [MenuItem("PeopleOfMath/Fix Portrait Texture Import (Sprite)")]
        public static void FixAll()
        {
            var count = ReimportAll();
            AssetDatabase.Refresh();
            WikimediaPortraitImporter.LinkAllFromFolders();
            Debug.Log($"Reimported {count} portrait textures as Sprites (max {PortraitMaxSize}, crunch on).");
        }

        public static int ReimportAll()
        {
            if (!Directory.Exists(ResourcesRoot))
            {
                Debug.LogWarning("No Resources/Portraits folder.");
                return 0;
            }

            var count = 0;
            foreach (var file in Directory.GetFiles(ResourcesRoot, "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                    continue;

                if (PortraitPlaceholderDetection.IsPlaceholderFile(file))
                    continue;

                var assetPath = file.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                    continue;

                ApplyPortraitImportSettings(importer, assetPath);
                importer.SaveAndReimport();
                count++;
            }

            return count;
        }

        internal static void ApplyPortraitImportSettings(TextureImporter importer, string assetPath)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = PortraitMaxSize;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.compressionQuality = 50;
            importer.crunchedCompression = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.mipmapEnabled = false;
            settings.readable = false;

            var ext = Path.GetExtension(assetPath).ToLowerInvariant();
            if (ext is ".jpg" or ".jpeg")
                settings.alphaIsTransparency = false;

            importer.SetTextureSettings(settings);

            ApplyPlatform(importer, "DefaultTexturePlatform", overridden: false, TextureImporterFormat.Automatic);
            // ASTC 8x8: strong size win for UI portraits while remaining recognizable.
            ApplyPlatform(importer, "Android", overridden: true, TextureImporterFormat.ASTC_8x8);
            ApplyPlatform(importer, "iPhone", overridden: true, TextureImporterFormat.ASTC_6x6);
            ApplyPlatform(importer, "WebGL", overridden: true, TextureImporterFormat.Automatic);
            ApplyPlatform(importer, "Standalone", overridden: true, TextureImporterFormat.Automatic);
        }

        static void ApplyPlatform(
            TextureImporter importer,
            string platform,
            bool overridden,
            TextureImporterFormat format)
        {
            var ps = new TextureImporterPlatformSettings
            {
                name = platform,
                overridden = overridden,
                maxTextureSize = PortraitMaxSize,
                resizeAlgorithm = TextureResizeAlgorithm.Bilinear,
                format = format,
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = 50,
                crunchedCompression = true,
                allowsAlphaSplitting = false,
            };
            importer.SetPlatformTextureSettings(ps);
        }
    }
}
