using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public static class UiButtonStyler
    {
        const string GlowChildName = "Glow";
        const string FillChildName = "Fill";
        const string TabIndicatorChildName = "TabIndicator";

        public static void Apply(Button button, UiButtonStyle style, bool showTabIndicator = false)
        {
            if (button == null)
                return;

            button.transition = Selectable.Transition.None;
            EnsurePressFeedback(button);

            switch (style)
            {
                case UiButtonStyle.Primary:
                    ApplyPrimary(button, showTabIndicator);
                    break;
                default:
                    ApplySecondary(button, showTabIndicator);
                    break;
            }

            ApplyLabelColor(button, style);
            button.GetComponent<UiButtonPressFeedback>()?.RefreshNormalFillColor();
        }

        static void EnsurePressFeedback(Button button)
        {
            if (button.GetComponent<UiButtonPressFeedback>() == null)
                button.gameObject.AddComponent<UiButtonPressFeedback>();
        }

        static void ApplyPrimary(Button button, bool showTabIndicator)
        {
            var glow = GetGlowImage(button);
            var fill = GetFillImage(button);
            var roundedSprite = UiSprites.RoundedRect;
            if (glow == null || fill == null || roundedSprite == null)
                return;

            RestoreDefaultMaterial(fill);
            RestoreDefaultMaterial(glow);

            if (ThemeHelper.IsGlassmorphism)
            {
                glow.sprite = roundedSprite;
                glow.type = Image.Type.Sliced;
                glow.color = new Color(1f, 1f, 1f, 0.08f);

                fill.sprite = roundedSprite;
                fill.type = Image.Type.Sliced;
                fill.color = showTabIndicator
                    ? new Color(1f, 1f, 1f, 0.18f)
                    : Color.white;

                var border = fill.GetComponent<Outline>();
                if (border != null)
                    border.effectColor = new Color(1f, 1f, 1f, 0.35f);

                ApplyTabIndicator(button, showTabIndicator, UiTheme.AccentWarm);
                return;
            }

            var gradientSprite = UiSprites.ButtonGradient;
            if (gradientSprite == null)
                return;

            glow.sprite = roundedSprite;
            glow.type = Image.Type.Sliced;
            glow.color = showTabIndicator ? UiTheme.GlowHighlighted : UiTheme.GlowHighlighted;

            fill.sprite = showTabIndicator ? roundedSprite : gradientSprite;
            fill.type = Image.Type.Sliced;
            fill.color = showTabIndicator ? UiTheme.ButtonSecondaryFill : Color.white;

            var defaultBorder = fill.GetComponent<Outline>();
            if (defaultBorder != null)
                defaultBorder.effectColor = showTabIndicator ? UiTheme.CardBorderActive : UiTheme.CardBorder;

            ApplyTabIndicator(button, showTabIndicator, UiTheme.PrimaryAccent);
        }

        static void ApplySecondary(Button button, bool showTabIndicator)
        {
            var glow = GetGlowImage(button);
            var fill = GetFillImage(button);
            var roundedSprite = UiSprites.RoundedRect;
            if (glow == null || fill == null || roundedSprite == null)
                return;

            RestoreDefaultMaterial(fill);
            RestoreDefaultMaterial(glow);

            glow.sprite = roundedSprite;
            glow.type = Image.Type.Sliced;
            glow.color = ThemeHelper.IsGlassmorphism
                ? new Color(1f, 1f, 1f, 0.06f)
                : new Color(UiTheme.Glow.r, UiTheme.Glow.g, UiTheme.Glow.b, 0.12f);

            fill.sprite = roundedSprite;
            fill.type = Image.Type.Sliced;
            fill.color = UiTheme.ButtonSecondaryFill;

            var border = fill.GetComponent<Outline>();
            if (border != null)
                border.effectColor = UiTheme.ButtonSecondaryBorder;

            ApplyTabIndicator(button, false, UiTheme.PrimaryAccent);
        }

        static void ApplyTabIndicator(Button button, bool visible, Color color)
        {
            var indicator = GetOrCreateTabIndicator(button);
            if (indicator == null)
                return;

            indicator.gameObject.SetActive(visible);
            if (!visible)
                return;

            indicator.color = color;
        }

        static Image GetOrCreateTabIndicator(Button button)
        {
            var existing = button.transform.Find(TabIndicatorChildName);
            if (existing != null)
                return existing.GetComponent<Image>();

            var go = new GameObject(TabIndicatorChildName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(button.transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.12f, 0f);
            rt.anchorMax = new Vector2(0.88f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 6f);
            rt.sizeDelta = new Vector2(0f, 6f);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = UiTheme.PrimaryAccent;
            go.transform.SetAsLastSibling();
            return image;
        }

        static void RestoreDefaultMaterial(Image image)
        {
            if (image == null || image.material == null)
                return;

            if (image.material.shader != null
                && image.material.shader.name == GlassThemeAssets.FrostedGlassShaderName)
            {
                Object.Destroy(image.material);
                image.material = null;
            }
        }

        static Image GetGlowImage(Button button)
        {
            var glow = button.transform.Find(GlowChildName);
            return glow != null ? glow.GetComponent<Image>() : null;
        }

        static Image GetFillImage(Button button)
        {
            var fill = button.transform.Find(FillChildName);
            return fill != null ? fill.GetComponent<Image>() : null;
        }

        static void ApplyLabelColor(Button button, UiButtonStyle style)
        {
            Color color;
            if (style == UiButtonStyle.Primary)
            {
                color = ThemeHelper.IsGlassmorphism ? UiTheme.PrimaryButtonText : Color.white;
            }
            else
            {
                // Secondary buttons use a light fill in Glass (and Light); TextPrimary is white in Glass.
                color = ThemeHelper.IsGlassmorphism ? UiTheme.PrimaryButtonText : UiTheme.TextPrimary;
            }

            foreach (Transform child in button.transform)
            {
                if (child.name is GlowChildName or FillChildName or TabIndicatorChildName)
                    continue;

                var tmp = child.GetComponent<TMP_Text>();
                if (tmp != null)
                    tmp.color = color;
            }
        }
    }
}
