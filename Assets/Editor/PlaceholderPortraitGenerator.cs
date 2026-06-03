using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class PlaceholderPortraitGenerator
    {
        const string ResourcesRoot = "Assets/Resources/Portraits";

        [MenuItem("PeopleOfMath/Generate Placeholder Portraits (dev)")]
        public static void GenerateAll()
        {
            var json = File.ReadAllText("Assets/Data/mathematicians_catalog.json", Encoding.UTF8);
            var root = JsonUtility.FromJson<MathematicianCatalogRoot>(json);
            if (root?.mathematicians == null)
                return;

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(ResourcesRoot))
                AssetDatabase.CreateFolder("Assets/Resources", "Portraits");

            var hue = 0f;
            foreach (var entry in root.mathematicians)
            {
                var dir = $"{ResourcesRoot}/{entry.id}";
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    var parent = ResourcesRoot;
                    AssetDatabase.CreateFolder(parent, entry.id);
                }

                for (var i = 1; i <= 2; i++)
                {
                    var path = $"{dir}/{i:D2}.jpg";
                    if (File.Exists(path))
                        continue;

                    var tex = MakeTexture(entry.wikiTitleRu ?? entry.id, hue + i * 0.07f);
                    File.WriteAllBytes(path, tex.EncodeToJPG(80));
                    Object.DestroyImmediate(tex);
                    ConfigureSprite(path);
                }

                hue += 0.013f;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WikimediaPortraitImporter.LinkAllFromFolders();
            Debug.Log("Placeholder portraits generated under Assets/Resources/Portraits.");
        }

        static Texture2D MakeTexture(string label, float hue)
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            var bg = Color.HSVToRGB(hue % 1f, 0.35f, 0.45f);
            var fg = Color.HSVToRGB(hue % 1f, 0.2f, 0.95f);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                tex.SetPixel(x, y, bg);

            var margin = 24;
            for (var y = margin; y < size - margin; y++)
            for (var x = margin; x < size - margin; x++)
                if ((x + y) % 32 < 16)
                    tex.SetPixel(x, y, fg);

            tex.Apply();
            return tex;
        }

        static void ConfigureSprite(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
        }
    }
}
