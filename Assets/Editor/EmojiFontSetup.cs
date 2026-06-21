using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

namespace PeopleOfMath.Editor
{
    public static class EmojiFontSetup
    {
        const string FontTtfPath = "Assets/TextMesh Pro/Fonts/NotoColorEmoji-Regular.ttf";
        const string EmojiAssetPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/Noto Color Emoji.asset";
        const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        const string LiberationSansPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [MenuItem("PeopleOfMath/Setup Emoji Font Fallback")]
        public static void SetupMenu()
        {
            if (!SetupEmojiFontFallback())
            {
                EditorUtility.DisplayDialog(
                    "Emoji Font",
                    $"Не удалось настроить emoji fallback.\n\nПоложите Noto Color Emoji TTF в:\n{FontTtfPath}",
                    "OK");
                return;
            }

            EditorUtility.DisplayDialog(
                "Emoji Font",
                "Emoji fallback подключён.\n\nTMP Settings → Fallback Emoji Text Assets:\nNoto Color Emoji",
                "OK");
        }

        public static bool SetupEmojiFontFallback()
        {
            if (TMP_Settings.instance == null)
            {
                Debug.LogError("TMP Settings не найдены. Импортируйте TMP Essential Resources.");
                return false;
            }

            if (!File.Exists(FontTtfPath))
            {
                Debug.LogError($"TTF не найден: {FontTtfPath}");
                return false;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontTtfPath);
            if (font == null)
            {
                Debug.LogError($"Не удалось загрузить Font из {FontTtfPath}");
                return false;
            }

            var emojiAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EmojiAssetPath);
            if (emojiAsset == null)
                emojiAsset = CreateColorFontAsset(font, EmojiAssetPath);

            if (emojiAsset == null)
            {
                Debug.LogError("Не удалось создать Color Font Asset для emoji.");
                return false;
            }

            WireTmpSettings(emojiAsset);
            TryAddLiberationFallback(emojiAsset);

            AssetDatabase.SaveAssets();
            Debug.Log($"Emoji font fallback configured: {EmojiAssetPath}");
            return true;
        }

        static TMP_FontAsset CreateColorFontAsset(Font font, string assetPath)
        {
            var folderPath = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(folderPath) && !Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                90,
                9,
                GlyphRenderMode.COLOR,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);

            if (fontAsset == null)
                return null;

            fontAsset.name = "Noto Color Emoji";

            AssetDatabase.CreateAsset(fontAsset, assetPath);
            if (fontAsset.material != null)
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);

            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        static void WireTmpSettings(TMP_FontAsset emojiAsset)
        {
            TMP_Settings.enableEmojiSupport = true;

            var list = TMP_Settings.emojiFallbackTextAssets ?? new List<TMP_Asset>();
            if (!list.Contains(emojiAsset))
            {
                list.Clear();
                list.Add(emojiAsset);
            }

            TMP_Settings.emojiFallbackTextAssets = list;

            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings != null)
                EditorUtility.SetDirty(settings);
        }

        static void TryAddLiberationFallback(TMP_FontAsset emojiAsset)
        {
            var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationSansPath);
            if (liberation == null)
                return;

            if (liberation.fallbackFontAssetTable == null)
                liberation.fallbackFontAssetTable = new List<TMP_FontAsset>();

            if (!liberation.fallbackFontAssetTable.Contains(emojiAsset))
                liberation.fallbackFontAssetTable.Add(emojiAsset);

            EditorUtility.SetDirty(liberation);
        }
    }
}
