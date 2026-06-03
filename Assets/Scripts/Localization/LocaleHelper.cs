using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace PeopleOfMath.Localization
{
    public static class LocaleHelper
    {
        public const string PrefsKey = "app_locale";

        public static bool IsEnglish =>
            LocalizationSettings.SelectedLocale != null &&
            LocalizationSettings.SelectedLocale.Identifier.Code == "en";

        public static IEnumerator InitializeLocale()
        {
            yield return LocalizationSettings.InitializationOperation;

            var saved = PlayerPrefs.GetString(PrefsKey, "ru");
            var locale = LocalizationSettings.AvailableLocales.Locales.Find(
                l => l.Identifier.Code == saved);
            if (locale == null)
                locale = LocalizationSettings.AvailableLocales.Locales.Find(
                    l => l.Identifier.Code == "ru");

            if (locale != null)
                LocalizationSettings.SelectedLocale = locale;
        }

        public static void SetLocale(string code)
        {
            var locale = LocalizationSettings.AvailableLocales.Locales.Find(
                l => l.Identifier.Code == code);
            if (locale == null)
                return;

            LocalizationSettings.SelectedLocale = locale;
            PlayerPrefs.SetString(PrefsKey, code);
            PlayerPrefs.Save();
        }
    }
}
