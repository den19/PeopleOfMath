using System.Collections;
using PeopleOfMath.Core;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class SearchBar : MonoBehaviour
    {
        const float DebounceSeconds = 0.8f;

        [SerializeField] NavigationController navigation;
        [SerializeField] TMP_InputField inputField;
        [SerializeField] Button clearButton;
        [SerializeField] UiThemedCard themedCard;
        [SerializeField] Image glowImage;

        Coroutine _debounceRoutine;
        bool _suppressCallbacks;

        public string Query => inputField != null ? inputField.text.Trim() : "";

        void Awake()
        {
            if (inputField != null)
            {
                inputField.onValueChanged.AddListener(OnInputChanged);
                inputField.onSubmit.AddListener(OnSubmit);
                inputField.onSelect.AddListener(_ => SetFocused(true));
                inputField.onDeselect.AddListener(_ => SetFocused(false));
            }

            if (clearButton != null)
                clearButton.onClick.AddListener(OnClearClicked);

            UpdateClearVisibility();
        }

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            FontSizeHelper.FontSizeChanged += OnFontSizeChanged;
            ThemeHelper.ThemeChanged += OnThemeChanged;
            RefreshPlaceholder();
            RefreshInputTextColor();
            themedCard?.Apply();
            ApplyGlow(false);
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            FontSizeHelper.FontSizeChanged -= OnFontSizeChanged;
            ThemeHelper.ThemeChanged -= OnThemeChanged;
        }

        void OnLocaleChanged(UnityEngine.Localization.Locale _) => RefreshPlaceholder();

        void OnFontSizeChanged() => RefreshPlaceholder();

        void OnThemeChanged()
        {
            themedCard?.Apply();
            ApplyGlow(inputField != null && inputField.isFocused);
            RefreshInputTextColor();
        }

        void RefreshInputTextColor()
        {
            if (inputField?.textComponent is TMP_Text text)
                text.color = UiTheme.TextPrimary;
        }

        public void SetQuerySilently(string query)
        {
            if (inputField == null)
                return;

            _suppressCallbacks = true;
            inputField.SetTextWithoutNotify(query ?? "");
            _suppressCallbacks = false;
            UpdateClearVisibility();
        }

        void OnInputChanged(string _)
        {
            UpdateClearVisibility();
            if (_suppressCallbacks || navigation == null)
                return;

            ScheduleSearch();
        }

        void OnSubmit(string text)
        {
            if (_suppressCallbacks || navigation == null)
                return;

            CancelDebounce();
            TriggerSearch(text);
        }

        void OnClearClicked()
        {
            if (inputField == null)
                return;

            _suppressCallbacks = true;
            inputField.text = "";
            _suppressCallbacks = false;
            UpdateClearVisibility();
            navigation?.ShowHome();
        }

        void ScheduleSearch()
        {
            CancelDebounce();
            _debounceRoutine = StartCoroutine(DebounceSearch());
        }

        IEnumerator DebounceSearch()
        {
            yield return new WaitForSeconds(DebounceSeconds);
            _debounceRoutine = null;
            TriggerSearch(inputField != null ? inputField.text : "");
        }

        void CancelDebounce()
        {
            if (_debounceRoutine != null)
            {
                StopCoroutine(_debounceRoutine);
                _debounceRoutine = null;
            }
        }

        void TriggerSearch(string raw)
        {
            var query = raw?.Trim() ?? "";
            if (string.IsNullOrEmpty(query))
            {
                navigation.ShowHome();
                return;
            }

            navigation.ShowSearch(query);
        }

        void UpdateClearVisibility()
        {
            if (clearButton != null)
                clearButton.gameObject.SetActive(!string.IsNullOrEmpty(inputField?.text));
        }

        void SetFocused(bool focused) => ApplyGlow(focused);

        void ApplyGlow(bool highlighted)
        {
            if (glowImage != null)
                glowImage.color = highlighted ? UiTheme.GlowHighlighted : UiTheme.Glow;
        }

        void RefreshPlaceholder()
        {
            if (inputField?.placeholder is not TMP_Text placeholder)
                return;

            placeholder.text =
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "search_placeholder");
            placeholder.color = UiTheme.TextSecondary;
        }
    }
}
