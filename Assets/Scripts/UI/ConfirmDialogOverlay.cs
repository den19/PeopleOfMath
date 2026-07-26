using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    /// <summary>
    /// Lightweight modal confirm dialog matching onboarding card styling.
    /// </summary>
    public class ConfirmDialogOverlay : MonoBehaviour
    {
        Action _onConfirm;
        Image _cardImage;
        TMP_Text _title;
        TMP_Text _body;
        Button _confirmButton;
        Button _cancelButton;

        public static void Show(
            Transform parent,
            string title,
            string body,
            string confirmLabel,
            string cancelLabel,
            Action onConfirm)
        {
            if (parent == null || onConfirm == null)
                return;

            var existing = parent.GetComponentInChildren<ConfirmDialogOverlay>(true);
            if (existing != null)
                Destroy(existing.gameObject);

            var root = new GameObject("ConfirmDialogOverlay", typeof(RectTransform), typeof(ConfirmDialogOverlay));
            root.transform.SetParent(parent, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            root.GetComponent<ConfirmDialogOverlay>().Build(title, body, confirmLabel, cancelLabel, onConfirm);
        }

        void Build(string title, string body, string confirmLabel, string cancelLabel, Action onConfirm)
        {
            _onConfirm = onConfirm;

            var scrimGo = new GameObject("Scrim", typeof(RectTransform), typeof(Image));
            scrimGo.transform.SetParent(transform, false);
            var scrimRt = scrimGo.GetComponent<RectTransform>();
            scrimRt.anchorMin = Vector2.zero;
            scrimRt.anchorMax = Vector2.one;
            scrimRt.offsetMin = Vector2.zero;
            scrimRt.offsetMax = Vector2.zero;
            var scrim = scrimGo.GetComponent<Image>();
            scrim.color = new Color(0f, 0f, 0f, 0.72f);
            scrim.raycastTarget = true;

            var card = CreateCard(scrimGo.transform);
            _cardImage = card.GetComponent<Image>();
            _title = CreateText(card, "Title", 34f, FontStyles.Bold, new Vector2(0f, -28f), new Vector2(-48f, -120f));
            _body = CreateText(card, "Body", 26f, FontStyles.Normal, new Vector2(0f, -130f), new Vector2(-48f, -320f));
            _title.text = title;
            _body.text = body;

            _cancelButton = CreateButton(card, "CancelButton", cancelLabel, new Vector2(-220f, 36f), UiButtonStyle.Secondary);
            _confirmButton = CreateButton(card, "ConfirmButton", confirmLabel, new Vector2(220f, 36f), UiButtonStyle.Primary);
            _cancelButton.onClick.AddListener(Close);
            _confirmButton.onClick.AddListener(OnConfirmClicked);

            ApplyTheme();
        }

        void OnEnable() => ThemeHelper.ThemeChanged += ApplyTheme;

        void OnDisable() => ThemeHelper.ThemeChanged -= ApplyTheme;

        void ApplyTheme()
        {
            if (_cardImage != null)
                _cardImage.color = ResolveCardFill();

            var textPrimary = ResolveTextPrimary();
            if (_title != null)
                _title.color = textPrimary;
            if (_body != null)
                _body.color = textPrimary;

            if (_cancelButton != null)
                UiButtonStyler.Apply(_cancelButton, UiButtonStyle.Secondary);
            if (_confirmButton != null)
                UiButtonStyler.Apply(_confirmButton, UiButtonStyle.Primary);
        }

        void OnConfirmClicked()
        {
            var action = _onConfirm;
            Close();
            action?.Invoke();
        }

        void Close() => Destroy(gameObject);

        static bool UsesLightCard() =>
            ThemeHelper.Current is AppTheme.Light or AppTheme.Glassmorphism;

        static Color ResolveTextPrimary() =>
            UsesLightCard() ? UiTheme.CardTextPrimary : UiTheme.TextPrimary;

        static Color ResolveCardFill() => ThemeHelper.Current switch
        {
            AppTheme.Light => new Color(1f, 1f, 1f, 0.98f),
            AppTheme.Glassmorphism => new Color(1f, 1f, 1f, 0.90f),
            _ => new Color(0.12f, 0.10f, 0.16f, 0.96f)
        };

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

        static TMP_Text CreateText(
            Transform parent,
            string name,
            float fontSize,
            FontStyles style,
            Vector2 topLeft,
            Vector2 bottomRight)
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

        static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            UiButtonStyle style)
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
    }
}
