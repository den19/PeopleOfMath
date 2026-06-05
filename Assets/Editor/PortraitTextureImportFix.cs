using System.IO;
using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class PortraitTextureImportFix
    {
        const string ResourcesRoot = "Assets/Resources/Portraits";

        [MenuItem("PeopleOfMath/Fix Portrait Texture Import (Sprite)")]
        public static void FixAll()
        {
            var count = ReimportAll();
            AssetDatabase.Refresh();
            WikimediaPortraitImporter.LinkAllFromFolders();
            Debug.Log($"Reimported {count} portrait textures as Sprites.");
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
            importer.maxTextureSize = 512;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;

            var ext = Path.GetExtension(assetPath).ToLowerInvariant();
            if (ext is ".jpg" or ".jpeg")
                settings.alphaIsTransparency = false;

            importer.SetTextureSettings(settings);
        }
    }
}
