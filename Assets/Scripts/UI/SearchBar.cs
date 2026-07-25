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
        const float DebounceSeconds = 0.28f;
        const float BusySpinDegreesPerSecond = 220f;
        const float ClearHitSize = 88f;
        const float ClearVisualSize = 48f;

        [SerializeField] NavigationController navigation;
        [SerializeField] TMP_InputField inputField;
        [SerializeField] Button clearButton;
        [SerializeField] UiThemedCard themedCard;
        [SerializeField] Image glowImage;
        [SerializeField] RectTransform busyIndicator;
        [SerializeField] Image busyRing;

        Coroutine _debounceRoutine;
        Coroutine _busySpinRoutine;
        Coroutine _endBusyRoutine;
        bool _suppressCallbacks;
        bool _isBusy;
        int _busyGeneration;

        public string Query => inputField != null ? inputField.text.Trim() : "";

        void Awake()
        {
            EnsureClearButtonVisuals();
            EnsureBusyIndicator();

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
            SetBusy(false);
        }

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            FontSizeHelper.FontSizeChanged += OnFontSizeChanged;
            ThemeHelper.ThemeChanged += OnThemeChanged;
            RefreshPlaceholder();
            RefreshInputTextColor();
            ApplyClearTheme();
            ApplyBusyTheme();
            themedCard?.Apply();
            ApplyGlow(false);
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            FontSizeHelper.FontSizeChanged -= OnFontSizeChanged;
            ThemeHelper.ThemeChanged -= OnThemeChanged;
            CancelDebounce();
            SetBusy(false);
        }

        void OnLocaleChanged(UnityEngine.Localization.Locale _) => RefreshPlaceholder();

        void OnFontSizeChanged() => RefreshPlaceholder();

        void OnThemeChanged()
        {
            themedCard?.Apply();
            ApplyGlow(inputField != null && inputField.isFocused);
            RefreshInputTextColor();
            ApplyClearTheme();
            ApplyBusyTheme();
        }

        void RefreshInputTextColor()
        {
            if (inputField?.textComponent is TMP_Text text)
                text.color = UiTheme.CardTextPrimary;
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

            CancelDebounce();
            SetBusy(false);
            _suppressCallbacks = true;
            inputField.text = "";
            _suppressCallbacks = false;
            UpdateClearVisibility();
            navigation?.ShowHome();
        }

        void ScheduleSearch()
        {
            CancelDebounce();
            var query = inputField != null ? inputField.text.Trim() : "";
            if (string.IsNullOrEmpty(query))
            {
                SetBusy(false);
                navigation?.ShowHome();
                return;
            }

            SetBusy(true);
            _debounceRoutine = StartCoroutine(DebounceSearch());
        }

        IEnumerator DebounceSearch()
        {
            yield return new WaitForSecondsRealtime(DebounceSeconds);
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
                SetBusy(false);
                navigation.ShowHome();
                return;
            }

            SetBusy(true);
            navigation.ShowSearch(query);
            var generation = _busyGeneration;
            if (_endBusyRoutine != null)
                StopCoroutine(_endBusyRoutine);
            _endBusyRoutine = StartCoroutine(EndBusyNextFrame(generation));
        }

        IEnumerator EndBusyNextFrame(int generation)
        {
            yield return null;
            _endBusyRoutine = null;
            if (generation == _busyGeneration)
                SetBusy(false);
        }

        void UpdateClearVisibility()
        {
            if (clearButton == null)
                return;

            var hasText = !string.IsNullOrEmpty(inputField?.text);
            clearButton.gameObject.SetActive(hasText && !_isBusy);
        }

        void SetFocused(bool focused) => ApplyGlow(focused);

        void ApplyGlow(bool highlighted)
        {
            if (glowImage != null)
                glowImage.color = highlighted ? UiTheme.GlowHighlighted : UiTheme.AccentMuted;
        }

        void RefreshPlaceholder()
        {
            if (inputField?.placeholder is not TMP_Text placeholder)
                return;

            placeholder.text =
                LocalizationSettings.StringDatabase.GetLocalizedString("UI", "search_placeholder");
            placeholder.color = UiTheme.CardTextSecondary;
        }

        void SetBusy(bool busy)
        {
            if (busy == _isBusy)
            {
                UpdateClearVisibility();
                return;
            }

            if (busy)
                _busyGeneration++;

            _isBusy = busy;
            if (busyIndicator != null)
                busyIndicator.gameObject.SetActive(busy);

            if (busy)
            {
                if (_busySpinRoutine == null && isActiveAndEnabled)
                    _busySpinRoutine = StartCoroutine(SpinBusyIndicator());
            }
            else if (_busySpinRoutine != null)
            {
                StopCoroutine(_busySpinRoutine);
                _busySpinRoutine = null;
            }

            UpdateClearVisibility();
        }

        IEnumerator SpinBusyIndicator()
        {
            while (_isBusy && busyIndicator != null)
            {
                busyIndicator.Rotate(0f, 0f, -BusySpinDegreesPerSecond * Time.unscaledDeltaTime);
                yield return null;
            }

            _busySpinRoutine = null;
        }

        void EnsureBusyIndicator()
        {
            if (busyIndicator == null)
            {
                var existing = transform.Find("BusyIndicator") as RectTransform;
                if (existing != null)
                {
                    busyIndicator = existing;
                    busyRing = existing.GetComponent<Image>();
                }
            }

            if (busyIndicator == null)
            {
                var go = new GameObject("BusyIndicator", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                busyIndicator = go.GetComponent<RectTransform>();
                busyRing = go.GetComponent<Image>();
            }

            busyIndicator.anchorMin = new Vector2(1f, 0.5f);
            busyIndicator.anchorMax = new Vector2(1f, 0.5f);
            busyIndicator.pivot = new Vector2(0.5f, 0.5f);
            busyIndicator.anchoredPosition = new Vector2(-46f, 0f);
            busyIndicator.sizeDelta = new Vector2(44f, 44f);
            busyIndicator.gameObject.SetActive(false);

            if (busyRing != null)
            {
                busyRing.sprite = UiSprites.RoundedRect;
                busyRing.type = Image.Type.Filled;
                busyRing.fillMethod = Image.FillMethod.Radial360;
                busyRing.fillOrigin = (int)Image.Origin360.Top;
                busyRing.fillClockwise = true;
                busyRing.fillAmount = 0.72f;
                busyRing.raycastTarget = false;
                ApplyBusyTheme();
            }

            // Soft inner hole so the arc reads as a spinner ring.
            var hole = busyIndicator.Find("Hole") as RectTransform;
            if (hole == null)
            {
                var holeGo = new GameObject("Hole", typeof(RectTransform), typeof(Image));
                holeGo.transform.SetParent(busyIndicator, false);
                hole = holeGo.GetComponent<RectTransform>();
                var holeImage = holeGo.GetComponent<Image>();
                holeImage.sprite = UiSprites.RoundedRect;
                holeImage.type = Image.Type.Sliced;
                holeImage.raycastTarget = false;
            }

            hole.anchorMin = new Vector2(0.5f, 0.5f);
            hole.anchorMax = new Vector2(0.5f, 0.5f);
            hole.pivot = new Vector2(0.5f, 0.5f);
            hole.anchoredPosition = Vector2.zero;
            hole.sizeDelta = new Vector2(22f, 22f);
            ApplyBusyTheme();
        }

        void ApplyBusyTheme()
        {
            if (busyRing != null)
            {
                var accent = UiTheme.PrimaryAccent;
                busyRing.color = new Color(accent.r, accent.g, accent.b, 0.95f);
            }

            var holeImage = busyIndicator != null
                ? busyIndicator.Find("Hole")?.GetComponent<Image>()
                : null;
            if (holeImage != null)
                holeImage.color = UiTheme.CardFill;
        }

        void EnsureClearButtonVisuals()
        {
            if (clearButton == null)
                return;

            var clearRt = clearButton.transform as RectTransform;
            if (clearRt != null)
            {
                clearRt.anchorMin = new Vector2(1f, 0.5f);
                clearRt.anchorMax = new Vector2(1f, 0.5f);
                clearRt.pivot = new Vector2(1f, 0.5f);
                clearRt.anchoredPosition = new Vector2(-10f, 0f);
                clearRt.sizeDelta = new Vector2(ClearHitSize, ClearHitSize);
            }

            var bg = clearButton.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = UiSprites.RoundedRect;
                bg.type = Image.Type.Sliced;
                bg.raycastTarget = true;
                clearButton.targetGraphic = bg;
            }

            var label = clearButton.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(clearButton.transform, false);
                var labelRt = labelGo.GetComponent<RectTransform>();
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;
                label = labelGo.GetComponent<TextMeshProUGUI>();
                label.text = "\u00d7";
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
            }

            label.fontSize = 36f;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;

            // Visual chip sits centered inside the larger hit target.
            var chip = clearButton.transform.Find("Chip") as RectTransform;
            if (chip == null && bg != null)
            {
                // Keep the button Image as the hit area; chip is drawn via bg size illusion:
                // use a child Chip image for the visible circle and leave root Image transparent.
                var chipGo = new GameObject("Chip", typeof(RectTransform), typeof(Image));
                chipGo.transform.SetParent(clearButton.transform, false);
                chipGo.transform.SetAsFirstSibling();
                chip = chipGo.GetComponent<RectTransform>();
                chip.anchorMin = new Vector2(0.5f, 0.5f);
                chip.anchorMax = new Vector2(0.5f, 0.5f);
                chip.pivot = new Vector2(0.5f, 0.5f);
                chip.anchoredPosition = Vector2.zero;
                chip.sizeDelta = new Vector2(ClearVisualSize, ClearVisualSize);
                var chipImage = chipGo.GetComponent<Image>();
                chipImage.sprite = UiSprites.RoundedRect;
                chipImage.type = Image.Type.Sliced;
                chipImage.raycastTarget = false;
            }
            else if (chip != null)
            {
                chip.sizeDelta = new Vector2(ClearVisualSize, ClearVisualSize);
            }

            if (bg != null)
                bg.color = Color.clear;

            ApplyClearTheme();

            if (inputField != null)
            {
                var inputRt = inputField.transform as RectTransform;
                if (inputRt != null)
                    inputRt.offsetMax = new Vector2(-(ClearHitSize + 8f), inputRt.offsetMax.y);
            }
        }

        void ApplyClearTheme()
        {
            var chipImage = clearButton != null
                ? clearButton.transform.Find("Chip")?.GetComponent<Image>()
                : null;
            if (chipImage != null)
            {
                var fill = ThemeHelper.IsGlassmorphism
                    ? new Color(1f, 1f, 1f, 0.22f)
                    : ThemeHelper.Current == AppTheme.Light
                        ? new Color(0.110f, 0.106f, 0.133f, 0.10f)
                        : new Color(1f, 1f, 1f, 0.12f);
                chipImage.color = fill;
            }

            var label = clearButton != null
                ? clearButton.transform.Find("Label")?.GetComponent<TextMeshProUGUI>()
                : null;
            if (label != null)
                label.color = UiTheme.CardTextPrimary;
        }
    }
}
