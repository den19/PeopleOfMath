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
        const string HeartOutlinePath = "Assets/Resources/UI/HeartOutline.png";
        const string HeartFilledPath = "Assets/Resources/UI/HeartFilled.png";
        const string TabBrowsePath = "Assets/Resources/UI/TabBrowse.png";
        const string TabIndexPath = "Assets/Resources/UI/TabIndex.png";
        const string TabQuizPath = "Assets/Resources/UI/TabQuiz.png";
        const string TabSettingsPath = "Assets/Resources/UI/TabSettings.png";
        const string TabAboutPath = "Assets/Resources/UI/TabAbout.png";
        const int TextureSize = 64;
        const int TabIconSize = 128;
        const int CornerRadius = 18;
        const int Border = 22;

        static Sprite _roundedRect;
        static Sprite _buttonGradient;
        static Sprite _shareIcon;
        static Sprite _heartOutline;
        static Sprite _heartFilled;
        static Sprite _tabBrowse;
        static Sprite _tabIndex;
        static Sprite _tabQuiz;
        static Sprite _tabSettings;
        static Sprite _tabAbout;

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

        public static Sprite HeartOutline
        {
            get
            {
                EnsureSprites();
                return _heartOutline;
            }
        }

        public static Sprite HeartFilled
        {
            get
            {
                EnsureSprites();
                return _heartFilled;
            }
        }

        public static Sprite TabBrowse
        {
            get
            {
                EnsureSprites();
                return _tabBrowse;
            }
        }

        public static Sprite TabIndex
        {
            get
            {
                EnsureSprites();
                return _tabIndex;
            }
        }

        public static Sprite TabQuiz
        {
            get
            {
                EnsureSprites();
                return _tabQuiz;
            }
        }

        public static Sprite TabSettings
        {
            get
            {
                EnsureSprites();
                return _tabSettings;
            }
        }

        public static Sprite TabAbout
        {
            get
            {
                EnsureSprites();
                return _tabAbout;
            }
        }

        public static Sprite GetTabIcon(PeopleOfMath.UI.NavTabId tab) => tab switch
        {
            PeopleOfMath.UI.NavTabId.Browse => TabBrowse,
            PeopleOfMath.UI.NavTabId.Index => TabIndex,
            PeopleOfMath.UI.NavTabId.Favorites => HeartOutline,
            PeopleOfMath.UI.NavTabId.Quiz => TabQuiz,
            PeopleOfMath.UI.NavTabId.Settings => TabSettings,
            PeopleOfMath.UI.NavTabId.About => TabAbout,
            _ => TabBrowse
        };

        public static void ResetTabIconCache()
        {
            _tabBrowse = null;
            _tabIndex = null;
            _tabQuiz = null;
            _tabSettings = null;
            _tabAbout = null;
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

            if (_heartOutline == null)
                _heartOutline = LoadOrCreateHeartIcon(HeartOutlinePath, filled: false);

            if (_heartFilled == null)
                _heartFilled = LoadOrCreateHeartIcon(HeartFilledPath, filled: true);

            if (_tabBrowse == null)
                _tabBrowse = LoadOrCreateTabIcon(TabBrowsePath, DrawTabBrowseIcon);

            if (_tabIndex == null)
                _tabIndex = LoadOrCreateTabIcon(TabIndexPath, DrawTabIndexIcon);

            if (_tabQuiz == null)
                _tabQuiz = LoadOrCreateTabIcon(TabQuizPath, DrawTabQuizIcon);

            if (_tabSettings == null)
                _tabSettings = LoadOrCreateTabIcon(TabSettingsPath, DrawTabSettingsIcon);

            if (_tabAbout == null)
                _tabAbout = LoadOrCreateTabIcon(TabAboutPath, DrawTabAboutIcon);
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

        static Sprite LoadOrCreateHeartIcon(string path, bool filled)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
                return existing;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(0, 0, 0, 0);

            DrawHeartIcon(pixels, size, filled);
            tex.SetPixels32(pixels);
            tex.Apply();

            var png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            ConfigureSpriteImporter(path, 0);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void DrawHeartIcon(Color32[] pixels, int size, bool filled)
        {
            var white = new Color32(255, 255, 255, 255);
            const float stroke = 3.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var inside = IsInsideHeart(x, y, size);
                    if (filled)
                    {
                        if (inside)
                            pixels[y * size + x] = white;
                    }
                    else
                    {
                        var outline = inside && !IsInsideHeart(x, y, size, stroke);
                        if (outline)
                            pixels[y * size + x] = white;
                    }
                }
            }
        }

        static bool IsInsideHeart(float x, float y, int size, float shrink = 0f)
        {
            var nx = (x - size * 0.5f) / (size * 0.22f);
            var ny = -(y - size * 0.46f) / (size * 0.22f);
            if (shrink > 0f)
            {
                var scale = 1f - shrink / (size * 0.22f);
                nx /= scale;
                ny /= scale;
            }

            var a = nx * nx + ny * ny - 1f;
            return a * a * a - nx * nx * ny * ny * ny <= 0f;
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

        delegate void TabIconDrawer(Color32[] pixels, int size);

        static Sprite LoadOrCreateTabIcon(string path, TabIconDrawer drawer)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
                return existing;

            var size = TabIconSize;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(0, 0, 0, 0);

            drawer(pixels, size);
            tex.SetPixels32(pixels);
            tex.Apply();

            var png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            ConfigureSpriteImporter(path, 0);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void DrawTabBrowseIcon(Color32[] pixels, int size)
        {
            var white = new Color32(255, 255, 255, 255);
            var pad = size * 0.18f;
            var gap = size * 0.1f;
            var cell = (size - pad * 2f - gap) * 0.5f;
            var radius = cell * 0.22f;
            var stroke = size * 0.055f;

            DrawRoundedRectOutline(pixels, size, new Rect(pad, pad + cell + gap, cell, cell), radius, stroke, white);
            DrawRoundedRectOutline(pixels, size, new Rect(pad + cell + gap, pad + cell + gap, cell, cell), radius, stroke, white);
            DrawRoundedRectOutline(pixels, size, new Rect(pad, pad, cell, cell), radius, stroke, white);
            DrawRoundedRectOutline(pixels, size, new Rect(pad + cell + gap, pad, cell, cell), radius, stroke, white);
        }

        static void DrawTabIndexIcon(Color32[] pixels, int size)
        {
            var white = new Color32(255, 255, 255, 255);
            var stroke = size * 0.07f;
            var left = size * 0.22f;
            var right = size * 0.78f;
            var y1 = size * 0.30f;
            var y2 = size * 0.50f;
            var y3 = size * 0.70f;
            var bullet = size * 0.055f;

            DrawCircle(pixels, size, new Vector2(left, y1), bullet, white);
            DrawCircle(pixels, size, new Vector2(left, y2), bullet, white);
            DrawCircle(pixels, size, new Vector2(left, y3), bullet, white);
            DrawLine(pixels, size, new Vector2(left + size * 0.12f, y1), new Vector2(right, y1), stroke, white);
            DrawLine(pixels, size, new Vector2(left + size * 0.12f, y2), new Vector2(right, y2), stroke, white);
            DrawLine(pixels, size, new Vector2(left + size * 0.12f, y3), new Vector2(right * 0.82f, y3), stroke, white);
        }

        static void DrawTabQuizIcon(Color32[] pixels, int size)
        {
            var white = new Color32(255, 255, 255, 255);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var radius = size * 0.36f;
            var stroke = size * 0.07f;
            DrawCircleOutline(pixels, size, center, radius, stroke, white);

            // Question mark stem + curve + dot
            var cx = size * 0.5f;
            DrawLine(pixels, size, new Vector2(cx, size * 0.58f), new Vector2(cx, size * 0.48f), stroke, white);
            DrawCircleOutline(pixels, size, new Vector2(cx, size * 0.38f), size * 0.11f, stroke, white);
            DrawCircle(pixels, size, new Vector2(cx + size * 0.08f, size * 0.34f), stroke * 0.55f, white);
            DrawCircle(pixels, size, new Vector2(cx, size * 0.70f), stroke * 0.65f, white);
        }

        static void DrawTabSettingsIcon(Color32[] pixels, int size)
        {
            var white = new Color32(255, 255, 255, 255);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var outer = size * 0.38f;
            var inner = size * 0.18f;
            var stroke = size * 0.065f;
            var teeth = 8;

            for (var i = 0; i < teeth; i++)
            {
                var angle = i / (float)teeth * Mathf.PI * 2f;
                var tip = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (outer + size * 0.08f);
                var baseA = center + new Vector2(Mathf.Cos(angle - 0.22f), Mathf.Sin(angle - 0.22f)) * outer;
                var baseB = center + new Vector2(Mathf.Cos(angle + 0.22f), Mathf.Sin(angle + 0.22f)) * outer;
                DrawLine(pixels, size, baseA, tip, stroke, white);
                DrawLine(pixels, size, tip, baseB, stroke, white);
            }

            DrawCircleOutline(pixels, size, center, outer, stroke, white);
            DrawCircleOutline(pixels, size, center, inner, stroke, white);
        }

        static void DrawTabAboutIcon(Color32[] pixels, int size)
        {
            var white = new Color32(255, 255, 255, 255);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var radius = size * 0.36f;
            var stroke = size * 0.07f;
            DrawCircleOutline(pixels, size, center, radius, stroke, white);
            DrawCircle(pixels, size, new Vector2(center.x, size * 0.34f), stroke * 0.7f, white);
            DrawLine(
                pixels,
                size,
                new Vector2(center.x, size * 0.46f),
                new Vector2(center.x, size * 0.70f),
                stroke,
                white);
        }

        static void DrawCircleOutline(Color32[] pixels, int size, Vector2 center, float radius, float stroke, Color32 color)
        {
            var outer = radius + stroke * 0.5f;
            var inner = Mathf.Max(0f, radius - stroke * 0.5f);
            var outer2 = outer * outer;
            var inner2 = inner * inner;
            var minX = Mathf.Max(0, Mathf.FloorToInt(center.x - outer));
            var maxX = Mathf.Min(size - 1, Mathf.CeilToInt(center.x + outer));
            var minY = Mathf.Max(0, Mathf.FloorToInt(center.y - outer));
            var maxY = Mathf.Min(size - 1, Mathf.CeilToInt(center.y + outer));

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var dx = x - center.x;
                    var dy = y - center.y;
                    var d2 = dx * dx + dy * dy;
                    if (d2 <= outer2 && d2 >= inner2)
                        pixels[y * size + x] = color;
                }
            }
        }

        static void DrawRoundedRectOutline(Color32[] pixels, int size, Rect rect, float radius, float stroke, Color32 color)
        {
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var outside = !IsInsideRoundedRectBounds(x, y, rect, radius);
                    var inside = IsInsideRoundedRectBounds(x, y, new Rect(
                        rect.x + stroke,
                        rect.y + stroke,
                        Mathf.Max(0f, rect.width - stroke * 2f),
                        Mathf.Max(0f, rect.height - stroke * 2f)), Mathf.Max(0f, radius - stroke));
                    if (!outside && !inside)
                        pixels[y * size + x] = color;
                }
            }
        }

        static bool IsInsideRoundedRectBounds(float x, float y, Rect rect, float radius)
        {
            if (x < rect.x || x > rect.xMax || y < rect.y || y > rect.yMax)
                return false;

            var left = rect.x + radius;
            var right = rect.xMax - radius;
            var bottom = rect.y + radius;
            var top = rect.yMax - radius;

            if (x >= left && x <= right)
                return true;
            if (y >= bottom && y <= top)
                return true;

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
            else
            {
                cx = right;
                cy = top;
            }

            var dx = x - cx;
            var dy = y - cy;
            return dx * dx + dy * dy <= radius * radius;
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
