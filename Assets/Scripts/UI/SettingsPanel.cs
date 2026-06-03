using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] Button russianButton;
        [SerializeField] Button englishButton;
        [SerializeField] TMP_Text statusText;

        void OnEnable()
        {
            RefreshStatus();
        }

        public void SelectRussian()
        {
            LocaleHelper.SetLocale("ru");
            RefreshStatus();
        }

        public void SelectEnglish()
        {
            LocaleHelper.SetLocale("en");
            RefreshStatus();
        }

        void RefreshStatus()
        {
            if (statusText == null)
                return;
            statusText.text = LocaleHelper.IsEnglish
                ? "Current language: English"
                : "Текущий язык: русский";
        }
    }
}
