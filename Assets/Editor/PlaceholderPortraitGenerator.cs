using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class PlaceholderPortraitGenerator
    {
        const string DevRoot = PortraitPlaceholderDetection.DevPlaceholdersRoot;

        [MenuItem("PeopleOfMath/Generate Placeholder Portraits (dev)")]
        public static void GenerateAll()
        {
            var json = File.ReadAllText("Assets/Data/mathematicians_catalog.json", Encoding.UTF8);
            var root = JsonUtility.FromJson<MathematicianCatalogRoot>(json);
            if (root?.mathematicians == null)
                return;

            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(DevRoot))
                AssetDatabase.CreateFolder("Assets/Data", "Placeholders");

            var hue = 0f;
            foreach (var entry in root.mathematicians)
            {
                var dir = $"{DevRoot}/{entry.id}";
                if (!AssetDatabase.IsValidFolder(dir))
                    AssetDatabase.CreateFolder(DevRoot, entry.id);

                for (var i = 1; i <= 2; i++)
                {
                    var path = $"{dir}/{i:D2}.jpg";
                    if (File.Exists(path) && !PortraitPlaceholderDetection.IsPlaceholderFile(path))
                        continue;

                    var tex = MakeTexture(entry.wikiTitleRu ?? entry.id, hue + i * 0.07f);
                    File.WriteAllBytes(path, tex.EncodeToJPG(80));
                    Object.DestroyImmediate(tex);
                    PortraitPlaceholderDetection.MarkAsPlaceholder(path);
                    ConfigureSprite(path);
                }

                hue += 0.013f;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Dev placeholders written to {DevRoot} (not loaded in game).");
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
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
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
