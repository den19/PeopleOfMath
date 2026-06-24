using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public static class UiButtonStyler
    {
        const string GlowChildName = "Glow";
        const string FillChildName = "Fill";

        public static void Apply(Button button, UiButtonStyle style)
        {
            if (button == null)
                return;

            button.transition = Selectable.Transition.None;

            switch (style)
            {
                case UiButtonStyle.Primary:
                    ApplyPrimary(button);
                    break;
                default:
                    ApplySecondary(button);
                    break;
            }

            ApplyLabelColor(button, style);
        }

        static void ApplyPrimary(Button button)
        {
            var glow = GetGlowImage(button);
            var fill = GetFillImage(button);
            var roundedSprite = UiSprites.RoundedRect;
            var gradientSprite = UiSprites.ButtonGradient;
            if (glow == null || fill == null || gradientSprite == null || roundedSprite == null)
                return;

            glow.sprite = roundedSprite;
            glow.type = Image.Type.Sliced;
            glow.color = UiTheme.GlowHighlighted;

            fill.sprite = gradientSprite;
            fill.type = Image.Type.Sliced;
            fill.color = Color.white;

            var border = fill.GetComponent<Outline>();
            if (border != null)
                border.effectColor = UiTheme.CardBorder;
        }

        static void ApplySecondary(Button button)
        {
            var glow = GetGlowImage(button);
            var fill = GetFillImage(button);
            var roundedSprite = UiSprites.RoundedRect;
            if (glow == null || fill == null || roundedSprite == null)
                return;

            glow.sprite = roundedSprite;
            glow.type = Image.Type.Sliced;
            glow.color = new Color(UiTheme.Glow.r, UiTheme.Glow.g, UiTheme.Glow.b, 0.12f);

            fill.sprite = roundedSprite;
            fill.type = Image.Type.Sliced;
            fill.color = UiTheme.ButtonSecondaryFill;

            var border = fill.GetComponent<Outline>();
            if (border != null)
                border.effectColor = UiTheme.ButtonSecondaryBorder;
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
            var color = style == UiButtonStyle.Primary
                ? Color.white
                : UiTheme.TextPrimary;

            foreach (Transform child in button.transform)
            {
                if (child.name is GlowChildName or FillChildName)
                    continue;

                var tmp = child.GetComponent<TMP_Text>();
                if (tmp != null)
                    tmp.color = color;
            }
        }
    }
}
