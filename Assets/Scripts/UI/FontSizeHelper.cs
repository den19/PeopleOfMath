using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PeopleOfMath.UI
{
    public enum FontSizeLevel
    {
        Normal,
        Large,
        ExtraLarge
    }

    public static class FontSizeHelper
    {
        public const string PrefsKey = "app_font_size";

        static readonly Dictionary<int, float> BaseSizes = new();

        public static event Action FontSizeChanged;

        public static FontSizeLevel CurrentLevel { get; private set; } = FontSizeLevel.Normal;

        public static float Multiplier => GetMultiplier(CurrentLevel);

        public static void Initialize()
        {
            var saved = PlayerPrefs.GetInt(PrefsKey, (int)FontSizeLevel.Normal);
            CurrentLevel = Enum.IsDefined(typeof(FontSizeLevel), saved)
                ? (FontSizeLevel)saved
                : FontSizeLevel.Normal;
            BaseSizes.Clear();
            FontSizeChanged?.Invoke();
        }

        public static void SetLevel(FontSizeLevel level)
        {
            if (CurrentLevel == level)
                return;

            CurrentLevel = level;
            PlayerPrefs.SetInt(PrefsKey, (int)level);
            PlayerPrefs.Save();
            FontSizeChanged?.Invoke();
        }

        public static float GetMultiplier(FontSizeLevel level) => level switch
        {
            FontSizeLevel.Large => 1.15f,
            FontSizeLevel.ExtraLarge => 1.30f,
            _ => 1.0f
        };

        public static void ApplyTo(TMP_Text text)
        {
            if (text == null)
                return;

            var id = text.GetInstanceID();
            if (!BaseSizes.TryGetValue(id, out var baseSize))
            {
                // Prefab/scene text is authored at Normal (1x) scale on first Apply.
                baseSize = text.fontSize;
                if (baseSize < 1f)
                    return;
                BaseSizes[id] = baseSize;
            }

            text.fontSize = Mathf.Round(baseSize * Multiplier);
        }

        public static string GetLevelLabel(bool english, FontSizeLevel level) => level switch
        {
            FontSizeLevel.Large => english ? "Large" : "Крупный",
            FontSizeLevel.ExtraLarge => english ? "Extra large" : "Очень крупный",
            _ => english ? "Normal" : "Обычный"
        };
    }
}
