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
            if (!Directory.Exists(ResourcesRoot))
            {
                Debug.LogWarning("No Resources/Portraits folder.");
                return;
            }

            var count = 0;
            foreach (var file in Directory.GetFiles(ResourcesRoot, "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                    continue;

                var assetPath = file.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.maxTextureSize = 512;
                importer.SaveAndReimport();
                count++;
            }

            AssetDatabase.Refresh();
            WikimediaPortraitImporter.LinkAllFromFolders();
            Debug.Log($"Reimported {count} portrait textures as Sprites.");
        }
    }
}
