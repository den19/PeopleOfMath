using PeopleOfMath.Core;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class SettingsPanel : MonoBehaviour
    {
        const float ResetLabelY = -1200f;
        const float ResetButtonY = -1280f;

        [SerializeField] Button russianButton;
        [SerializeField] Button englishButton;
        [SerializeField] TMP_Text statusText;
        [SerializeField] Button fontNormalButton;
        [SerializeField] Button fontLargeButton;
        [SerializeField] Button fontExtraLargeButton;
        [SerializeField] TMP_Text fontStatusText;
        [SerializeField] Button darkThemeButton;
        [SerializeField] Button lightThemeButton;
        [SerializeField] Button glassThemeButton;
        [SerializeField] TMP_Text themeStatusText;
        [SerializeField] Button resetButton;
        [SerializeField] TMP_Text resetLabelText;

        void Awake()
        {
            EnsureResetUi();
            BindButton(russianButton, SelectRussian);
            BindButton(englishButton, SelectEnglish);
            BindButton(fontNormalButton, SelectFontNormal);
            BindButton(fontLargeButton, SelectFontLarge);
            BindButton(fontExtraLargeButton, SelectFontExtraLarge);
            BindButton(darkThemeButton, SelectDark);
            BindButton(lightThemeButton, SelectLight);
            BindButton(glassThemeButton, SelectGlass);
            BindButton(resetButton, PromptReset);
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

        public void SelectDark()
        {
            ThemeHelper.SetTheme(AppTheme.Dark);
            RefreshStatus();
        }

        public void SelectLight()
        {
            ThemeHelper.SetTheme(AppTheme.Light);
            RefreshStatus();
        }

        public void SelectGlass()
        {
            ThemeHelper.SetTheme(AppTheme.Glassmorphism);
            RefreshStatus();
        }

        public void PromptReset()
        {
            var canvas = GetComponentInParent<Canvas>();
            var parent = canvas != null ? canvas.transform : transform.root;
            ConfirmDialogOverlay.Show(
                parent,
                UiStrings.Get("settings_reset_confirm_title"),
                UiStrings.Get("settings_reset_confirm_body"),
                UiStrings.Get("settings_reset_confirm"),
                UiStrings.Get("settings_reset_cancel"),
                ApplyReset);
        }

        void ApplyReset()
        {
            AppDataReset.ResetAll();
            RefreshStatus();
        }

        public void RefreshStatus()
        {
            var english = LocaleHelper.IsEnglish;

            if (statusText != null)
            {
                var languageLabel = english ? "English" : "русский";
                statusText.text = UiStrings.Format("settings_current_language", languageLabel);
            }

            if (fontStatusText != null)
            {
                var levelLabel = FontSizeHelper.GetLevelLabel(english, FontSizeHelper.CurrentLevel);
                fontStatusText.text = UiStrings.Format("settings_current_font_size", levelLabel);
            }

            if (themeStatusText != null)
            {
                var themeLabel = ThemeHelper.GetThemeLabel(english, ThemeHelper.Current);
                themeStatusText.text = UiStrings.Format("settings_current_theme", themeLabel);
            }

            if (resetLabelText != null)
                resetLabelText.text = UiStrings.Get("settings_reset");

            SetButtonLabel(resetButton, UiStrings.Get("btn_reset_cache"));

            UiButtonStyler.Apply(russianButton, english ? UiButtonStyle.Secondary : UiButtonStyle.Primary);
            UiButtonStyler.Apply(englishButton, english ? UiButtonStyle.Primary : UiButtonStyle.Secondary);

            var level = FontSizeHelper.CurrentLevel;
            UiButtonStyler.Apply(fontNormalButton, level == FontSizeLevel.Normal ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            UiButtonStyler.Apply(fontLargeButton, level == FontSizeLevel.Large ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            UiButtonStyler.Apply(fontExtraLargeButton, level == FontSizeLevel.ExtraLarge ? UiButtonStyle.Primary : UiButtonStyle.Secondary);

            var currentTheme = ThemeHelper.Current;
            UiButtonStyler.Apply(darkThemeButton, currentTheme == AppTheme.Dark ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            UiButtonStyler.Apply(lightThemeButton, currentTheme == AppTheme.Light ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            UiButtonStyler.Apply(glassThemeButton, currentTheme == AppTheme.Glassmorphism ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            UiButtonStyler.Apply(resetButton, UiButtonStyle.Secondary);
        }

        void EnsureResetUi()
        {
            if (resetButton == null)
            {
                var existing = transform.Find("ResetButton")?.GetComponent<Button>();
                if (existing != null)
                    resetButton = existing;
            }

            if (resetLabelText == null)
            {
                var existingLabel = transform.Find("ResetLabel")?.GetComponent<TMP_Text>();
                if (existingLabel != null)
                    resetLabelText = existingLabel;
            }

            if (resetLabelText == null)
                resetLabelText = CreateResetLabel();

            if (resetButton == null)
                resetButton = CreateResetButton();
        }

        TMP_Text CreateResetLabel()
        {
            var template = transform.Find("ThemeLabel")?.gameObject
                ?? transform.Find("FontSizeLabel")?.gameObject
                ?? transform.Find("LangLabel")?.gameObject;
            if (template == null)
                return null;

            var labelGo = Instantiate(template, transform);
            labelGo.name = "ResetLabel";
            var rt = labelGo.GetComponent<RectTransform>();
            if (rt != null)
                rt.anchoredPosition = new Vector2(40f, ResetLabelY);

            var lse = labelGo.GetComponent<LocalizeStringEvent>();
            if (lse != null)
                Destroy(lse);

            var tmp = labelGo.GetComponent<TMP_Text>();
            if (tmp != null)
                tmp.text = UiStrings.Get("settings_reset");
            return tmp;
        }

        Button CreateResetButton()
        {
            var template = glassThemeButton != null ? glassThemeButton.gameObject
                : lightThemeButton != null ? lightThemeButton.gameObject
                : darkThemeButton != null ? darkThemeButton.gameObject
                : null;
            if (template == null)
                return null;

            var btnGo = Instantiate(template, transform);
            btnGo.name = "ResetButton";
            var rt = btnGo.GetComponent<RectTransform>();
            if (rt != null)
                rt.anchoredPosition = new Vector2(40f, ResetButtonY);

            foreach (var lse in btnGo.GetComponentsInChildren<LocalizeStringEvent>(true))
                Destroy(lse);

            var button = btnGo.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                UiButtonStyler.Apply(button, UiButtonStyle.Secondary);
                SetButtonLabel(button, UiStrings.Get("btn_reset_cache"));
            }

            return button;
        }

        static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
                return;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = text;
        }
    }
}
