using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] Button russianButton;
        [SerializeField] Button englishButton;
        [SerializeField] TMP_Text statusText;
        [SerializeField] Button fontNormalButton;
        [SerializeField] Button fontLargeButton;
        [SerializeField] Button fontExtraLargeButton;
        [SerializeField] TMP_Text fontStatusText;

        void Awake()
        {
            UiButtonStyler.Initialize(fontNormalButton, fontLargeButton);

            BindButton(russianButton, SelectRussian);
            BindButton(englishButton, SelectEnglish);
            BindButton(fontNormalButton, SelectFontNormal);
            BindButton(fontLargeButton, SelectFontLarge);
            BindButton(fontExtraLargeButton, SelectFontExtraLarge);
        }

        void OnEnable()
        {
            RefreshStatus();
        }

        static void BindButton(Button button, UnityAction handler)
        {
            if (button == null || handler == null)
                return;

            button.onClick.RemoveListener(handler);
            button.onClick.AddListener(handler);
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

        public void SelectFontNormal()
        {
            FontSizeHelper.SetLevel(FontSizeLevel.Normal);
            RefreshStatus();
        }

        public void SelectFontLarge()
        {
            FontSizeHelper.SetLevel(FontSizeLevel.Large);
            RefreshStatus();
        }

        public void SelectFontExtraLarge()
        {
            FontSizeHelper.SetLevel(FontSizeLevel.ExtraLarge);
            RefreshStatus();
        }

        void RefreshStatus()
        {
            var english = LocaleHelper.IsEnglish;

            if (statusText != null)
            {
                statusText.text = english
                    ? "Current language: English"
                    : "Текущий язык: русский";
            }

            if (fontStatusText != null)
            {
                var levelLabel = FontSizeHelper.GetLevelLabel(english, FontSizeHelper.CurrentLevel);
                fontStatusText.text = english
                    ? $"Current font size: {levelLabel}"
                    : $"Текущий размер шрифта: {levelLabel}";
            }

            UiButtonStyler.Apply(russianButton, english ? UiButtonStyle.Secondary : UiButtonStyle.Primary);
            UiButtonStyler.Apply(englishButton, english ? UiButtonStyle.Primary : UiButtonStyle.Secondary);

            var level = FontSizeHelper.CurrentLevel;
            UiButtonStyler.Apply(fontNormalButton, level == FontSizeLevel.Normal ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            UiButtonStyler.Apply(fontLargeButton, level == FontSizeLevel.Large ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            UiButtonStyler.Apply(fontExtraLargeButton, level == FontSizeLevel.ExtraLarge ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
        }
    }
}
