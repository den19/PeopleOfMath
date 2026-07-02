using UnityEngine.Localization.Settings;

namespace PeopleOfMath.Localization
{
    public static class UiStrings
    {
        public static string Get(string key) =>
            LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);

        public static string Format(string key, params object[] args) =>
            string.Format(Get(key), args);
    }
}
