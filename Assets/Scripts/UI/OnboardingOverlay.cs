using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class OnboardingOverlay : MonoBehaviour
    {
        public const string PlayerPrefsKey = "onboarding_complete";

        CanvasGroup _canvasGroup;
        TMP_Text _title;
        TMP_Text _body;
        TMP_Text _stepLabel;
        Button _nextButton;
        Button _skipButton;
        Image _cardImage;
        int _step;

        static readonly string[] StepTitleKeys =
        {
            "onboarding_title_filters",
            "onboarding_title_detail",
            "onboarding_title_quiz"
        };

        static readonly string[] StepBodyKeys =
        {
            "onboarding_body_filters",
            "onboarding_body_detail",
            "onboarding_body_quiz"
        };

        public static bool IsComplete => PlayerPrefs.GetInt(PlayerPrefsKey, 0) == 1;

        public static void MarkComplete()
        {
            PlayerPrefs.SetInt(PlayerPrefsKey, 1);
            PlayerPrefs.Save();
        }

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
        }

        public static void TryShow(Transform parent)
        {
            if (IsComplete || parent == null)
                return;

            var existing = parent.GetComponentInChildren<OnboardingOverlay>(true);
            if (existing != null)
                return;

            var root = new GameObject("OnboardingOverlay", typeof(RectTransform), typeof(OnboardingOverlay));
            root.transform.SetParent(parent, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            root.GetComponent<OnboardingOverlay>().BuildUi();
        }

        void BuildUi()
        {
            var scrimGo = new GameObject("Scrim", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            scrimGo.transform.SetParent(transform, false);
            var scrimRt = scrimGo.GetComponent<RectTransform>();
            scrimRt.anchorMin = Vector2.zero;
            scrimRt.anchorMax = Vector2.one;
            scrimRt.offsetMin = Vector2.zero;
            scrimRt.offsetMax = Vector2.zero;

            var scrim = scrimGo.GetComponent<Image>();
            scrim.color = new Color(0f, 0f, 0f, 0.72f);
            scrim.raycastTarget = true;
            _canvasGroup = scrimGo.GetComponent<CanvasGroup>();

            var card = CreateCard(scrimGo.transform);
            _cardImage = card.GetComponent<Image>();
            _title = CreateText(card, "Title", 34f, FontStyles.Bold, new Vector2(0f, -28f), new Vector2(-48f, -120f));
            _body = CreateText(card, "Body", 26f, FontStyles.Normal, new Vector2(0f, -130f), new Vector2(-48f, -320f));
            _stepLabel = CreateText(card, "Step", 22f, FontStyles.Normal, new Vector2(0f, -330f), new Vector2(-48f, -380f));

            _skipButton = CreateButton(card, "SkipButton", UiStrings.Get("onboarding_skip"), new Vector2(-220f, 36f), UiButtonStyle.Secondary);
            _nextButton = CreateButton(card, "NextButton", UiStrings.Get("onboarding_next"), new Vector2(220f, 36f), UiButtonStyle.Primary);

            _skipButton.onClick.AddListener(Complete);
            _nextButton.onClick.AddListener(OnNextClicked);

            _step = 0;
            RefreshStep();
            ApplyTheme();
        }

        void OnEnable()
        {
            ThemeHelper.ThemeChanged += ApplyTheme;
        }

        void OnDisable()
        {
            ThemeHelper.ThemeChanged -= ApplyTheme;
        }

        void ApplyTheme()
        {
            if (_cardImage != null)
                _cardImage.color = ResolveCardFill();

            var textPrimary = ResolveTextPrimary();
            var textSecondary = ResolveTextSecondary();

            if (_title != null)
                _title.color = textPrimary;
            if (_body != null)
                _body.color = textPrimary;
            if (_stepLabel != null)
                _stepLabel.color = textSecondary;

            if (_skipButton != null)
                UiButtonStyler.Apply(_skipButton, UiButtonStyle.Secondary);
            if (_nextButton != null)
                UiButtonStyler.Apply(_nextButton, UiButtonStyle.Primary);
        }

        static bool UsesLightOnboardingCard() =>
            ThemeHelper.Current is AppTheme.Light or AppTheme.Glassmorphism;

        static Color ResolveTextPrimary() =>
            UsesLightOnboardingCard() ? UiTheme.CardTextPrimary : UiTheme.TextPrimary;

        static Color ResolveTextSecondary() =>
            UsesLightOnboardingCard() ? UiTheme.CardTextSecondary : UiTheme.TextSecondary;

        static Color ResolveCardFill()
        {
            return ThemeHelper.Current switch
            {
                AppTheme.Light => new Color(1f, 1f, 1f, 0.98f),
                AppTheme.Glassmorphism => new Color(1f, 1f, 1f, 0.90f),
                _ => new Color(0.12f, 0.10f, 0.16f, 0.96f)
            };
        }

        static RectTransform CreateCard(Transform parent)
        {
            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(920f, 520f);

            var image = card.GetComponent<Image>();
            image.sprite = UiSprites.RoundedRect;
            image.type = Image.Type.Sliced;
            image.color = ResolveCardFill();
            return rt;
        }

        static TMP_Text CreateText(Transform parent, string name, float fontSize, FontStyles style, Vector2 topLeft, Vector2 bottomRight)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(48f, bottomRight.y);
            rt.offsetMax = new Vector2(-48f, topLeft.y);

            var text = go.GetComponent<TMP_Text>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = ResolveTextPrimary();
            text.alignment = name == "Body" ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Top;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, UiButtonStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = new Vector2(360f, 88f);

            var rootImage = go.GetComponent<Image>();
            rootImage.color = Color.clear;
            rootImage.raycastTarget = false;

            var glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(go.transform, false);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image), typeof(Outline));
            fill.transform.SetParent(go.transform, false);

            foreach (Transform child in new[] { glow.transform, fill.transform })
            {
                var childRt = child.GetComponent<RectTransform>();
                childRt.anchorMin = Vector2.zero;
                childRt.anchorMax = Vector2.one;
                childRt.offsetMin = child == glow.transform ? new Vector2(-4f, -4f) : Vector2.zero;
                childRt.offsetMax = child == glow.transform ? new Vector2(4f, 4f) : Vector2.zero;
                var image = child.GetComponent<Image>();
                image.sprite = UiSprites.RoundedRect;
                image.type = Image.Type.Sliced;
            }

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var tmp = labelGo.GetComponent<TMP_Text>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 26f;

            var button = go.GetComponent<Button>();
            button.targetGraphic = fill.GetComponent<Image>();
            UiButtonStyler.Apply(button, style);
            return button;
        }

        void OnNextClicked()
        {
            _step++;
            if (_step >= StepTitleKeys.Length)
            {
                Complete();
                return;
            }

            RefreshStep();
        }

        void RefreshStep()
        {
            if (_title != null)
                _title.text = UiStrings.Get(StepTitleKeys[_step]);
            if (_body != null)
                _body.text = UiStrings.Get(StepBodyKeys[_step]);
            if (_stepLabel != null)
                _stepLabel.text = UiStrings.Format("onboarding_step", _step + 1, StepTitleKeys.Length);
            if (_nextButton != null)
                SetButtonLabel(_nextButton, UiStrings.Get(_step == StepTitleKeys.Length - 1 ? "onboarding_done" : "onboarding_next"));
            if (_skipButton != null)
                SetButtonLabel(_skipButton, UiStrings.Get("onboarding_skip"));
        }

        static void SetButtonLabel(Button button, string text)
        {
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = text;
        }

        /// <summary>
        /// Android/system back: dismiss onboarding (same as Skip) instead of exiting the app.
        /// </summary>
        public bool TryHandleBack()
        {
            Complete();
            return true;
        }

        void Complete()
        {
            MarkComplete();
            Destroy(gameObject);
        }
    }
}
