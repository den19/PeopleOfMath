using System.IO;
using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class UiSpriteFactory
    {
        const string SpriteFolder = "Assets/UI/Sprites";
        const string RoundedRectPath = SpriteFolder + "/RoundedRect.png";
        const string ButtonGradientPath = SpriteFolder + "/ButtonGradient.png";
        const int TextureSize = 64;
        const int CornerRadius = 18;
        const int Border = 22;

        static Sprite _roundedRect;
        static Sprite _buttonGradient;

        public static Sprite RoundedRect
        {
            get
            {
                EnsureSprites();
                return _roundedRect;
            }
        }

        public static Sprite ButtonGradient
        {
            get
            {
                EnsureSprites();
                return _buttonGradient;
            }
        }

        public static void EnsureSprites()
        {
            if (!Directory.Exists(SpriteFolder))
                Directory.CreateDirectory(SpriteFolder);

            if (_roundedRect == null)
                _roundedRect = LoadOrCreateRoundedRect();

            if (_buttonGradient == null)
                _buttonGradient = LoadOrCreateButtonGradient();
        }

        static Sprite LoadOrCreateRoundedRect()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedRectPath);
            if (existing != null)
                return existing;

            var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[TextureSize * TextureSize];
            for (var y = 0; y < TextureSize; y++)
            {
                for (var x = 0; x < TextureSize; x++)
                {
                    var inside = IsInsideRoundedRect(x, y, TextureSize, TextureSize, CornerRadius);
                    pixels[y * TextureSize + x] = inside
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(0, 0, 0, 0);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            var png = tex.EncodeToPNG();
            File.WriteAllBytes(RoundedRectPath, png);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(RoundedRectPath);
            ConfigureSpriteImporter(RoundedRectPath, Border);
            return AssetDatabase.LoadAssetAtPath<Sprite>(RoundedRectPath);
        }

        static Sprite LoadOrCreateButtonGradient()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonGradientPath);
            if (existing != null)
                return existing;

            var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var top = new Color(0.165f, 0.059f, 0.271f, 1f);
            var bottom = new Color(0.039f, 0.039f, 0.039f, 1f);
            var pixels = new Color32[TextureSize * TextureSize];
            for (var y = 0; y < TextureSize; y++)
            {
                var t = y / (float)(TextureSize - 1);
                var color = Color.Lerp(bottom, top, t);
                for (var x = 0; x < TextureSize; x++)
                {
                    var pixelInside = IsInsideRoundedRect(x, y, TextureSize, TextureSize, CornerRadius);
                    pixels[y * TextureSize + x] = pixelInside
                        ? (Color32)color
                        : new Color32(0, 0, 0, 0);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            var png = tex.EncodeToPNG();
            File.WriteAllBytes(ButtonGradientPath, png);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(ButtonGradientPath);
            ConfigureSpriteImporter(ButtonGradientPath, Border);
            return AssetDatabase.LoadAssetAtPath<Sprite>(ButtonGradientPath);
        }

        static void ConfigureSpriteImporter(string path, int border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.SaveAndReimport();
        }

        static bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            var left = radius;
            var right = width - radius - 1;
            var bottom = radius;
            var top = height - radius - 1;

            if (x >= left && x <= right)
                return y >= 0 && y < height;
            if (y >= bottom && y <= top)
                return x >= 0 && x < width;

            float cx;
            float cy;
            if (x < left && y < bottom)
            {
                cx = left;
                cy = bottom;
            }
            else if (x > right && y < bottom)
            {
                cx = right;
                cy = bottom;
            }
            else if (x < left && y > top)
            {
                cx = left;
                cy = top;
            }
            else if (x > right && y > top)
            {
                cx = right;
                cy = top;
            }
            else
            {
                return x >= 0 && x < width && y >= 0 && y < height;
            }

            var dx = x - cx;
            var dy = y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }
    }
}
