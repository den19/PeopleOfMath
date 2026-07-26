using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using PeopleOfMath.Quiz;
using PeopleOfMath.UI;
using UnityEngine;

namespace PeopleOfMath.Core
{
    /// <summary>
    /// Clears all persisted user data so the app behaves like a fresh install.
    /// </summary>
    public static class AppDataReset
    {
        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(LocaleHelper.PrefsKey);
            PlayerPrefs.DeleteKey(FontSizeHelper.PrefsKey);
            PlayerPrefs.DeleteKey(ThemeHelper.PrefsKey);
            FavoritesHelper.Clear();
            QuizStatsHelper.Clear();
            OnboardingOverlay.ResetProgress();
            PlayerPrefs.Save();

            LocaleHelper.SetLocale("ru");
            FontSizeHelper.Initialize();
            ThemeHelper.Initialize();

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
                OnboardingOverlay.TryShow(canvas.transform);
        }
    }
}
