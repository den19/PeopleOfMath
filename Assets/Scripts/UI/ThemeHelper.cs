using System;
using UnityEngine;

namespace PeopleOfMath.UI
{
    public enum AppTheme
    {
        Dark, // Canonical Syncra brand theme (default)
        Light,
        Glassmorphism
    }

    public static class ThemeHelper
    {
        public const string PrefsKey = "app_theme";

        public static event Action ThemeChanged;

        public static AppTheme Current { get; private set; } = AppTheme.Dark;

        public static bool IsGlassmorphism => Current == AppTheme.Glassmorphism;

        public static void Initialize()
        {
            var saved = PlayerPrefs.GetInt(PrefsKey, (int)AppTheme.Dark);
            Current = Enum.IsDefined(typeof(AppTheme), saved)
                ? (AppTheme)saved
                : AppTheme.Dark;
            ThemeChanged?.Invoke();
        }

        public static void SetTheme(AppTheme theme)
        {
            if (Current == theme)
                return;

            Current = theme;
            PlayerPrefs.SetInt(PrefsKey, (int)theme);
            PlayerPrefs.Save();
            ThemeChanged?.Invoke();
        }

        public static string GetThemeLabel(bool english, AppTheme theme) => theme switch
        {
            AppTheme.Light => english ? "Light" : "Светлая",
            AppTheme.Glassmorphism => english ? "Glass" : "Стекло",
            _ => english ? "Dark" : "Тёмная"
        };
    }
}
