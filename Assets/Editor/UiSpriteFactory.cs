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
        const string ShareIconPath = "Assets/Resources/UI/ShareIcon.png";
        const int TextureSize = 64;
        const int CornerRadius = 18;
        const int Border = 22;

        static Sprite _roundedRect;
        static Sprite _buttonGradient;
        static Sprite _shareIcon;

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

        public static Sprite ShareIcon
        {
            get
            {
                EnsureSprites();
                return _shareIcon;
            }
        }

        public static void EnsureSprites()
        {
            if (!Directory.Exists(SpriteFolder))
                Directory.CreateDirectory(SpriteFolder);

            var resourcesUiFolder = Path.GetDirectoryName(ShareIconPath);
            if (!string.IsNullOrEmpty(resourcesUiFolder) && !Directory.Exists(resourcesUiFolder))
                Directory.CreateDirectory(resourcesUiFolder);

            if (_roundedRect == null)
                _roundedRect = LoadOrCreateRoundedRect();

            if (_buttonGradient == null)
                _buttonGradient = LoadOrCreateButtonGradient();

            if (_shareIcon == null)
                _shareIcon = LoadOrCreateShareIcon();
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

        static Sprite LoadOrCreateShareIcon()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(ShareIconPath);
            if (existing != null)
                return existing;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(0, 0, 0, 0);

            DrawShareIcon(pixels, size);
            tex.SetPixels32(pixels);
            tex.Apply();

            var png = tex.EncodeToPNG();
            File.WriteAllBytes(ShareIconPath, png);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(ShareIconPath);
            ConfigureSpriteImporter(ShareIconPath, 0);
            return AssetDatabase.LoadAssetAtPath<Sprite>(ShareIconPath);
        }

        static void DrawShareIcon(Color32[] pixels, int size)
        {
            var white = new Color32(255, 255, 255, 255);
            var left = new Vector2(18f, 32f);
            var top = new Vector2(46f, 20f);
            var bottom = new Vector2(46f, 44f);

            DrawCircle(pixels, size, left, 6f, white);
            DrawCircle(pixels, size, top, 6f, white);
            DrawCircle(pixels, size, bottom, 6f, white);
            DrawLine(pixels, size, left, top, 3f, white);
            DrawLine(pixels, size, left, bottom, 3f, white);
        }

        static void DrawCircle(Color32[] pixels, int size, Vector2 center, float radius, Color32 color)
        {
            var r2 = radius * radius;
            var minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
            var maxX = Mathf.Min(size - 1, Mathf.CeilToInt(center.x + radius));
            var minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
            var maxY = Mathf.Min(size - 1, Mathf.CeilToInt(center.y + radius));

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var dx = x - center.x;
                    var dy = y - center.y;
                    if (dx * dx + dy * dy <= r2)
                        pixels[y * size + x] = color;
                }
            }
        }

        static void DrawLine(Color32[] pixels, int size, Vector2 from, Vector2 to, float thickness, Color32 color)
        {
            var steps = Mathf.CeilToInt(Vector2.Distance(from, to) * 2f);
            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)steps;
                var point = Vector2.Lerp(from, to, t);
                DrawCircle(pixels, size, point, thickness * 0.5f, color);
            }
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
