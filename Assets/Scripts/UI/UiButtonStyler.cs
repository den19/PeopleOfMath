using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public static class UiButtonStyler
    {
        const string GlowChildName = "Glow";
        const string FillChildName = "Fill";

        static Sprite _gradientSprite;
        static Sprite _roundedSprite;

        public static void Initialize(Button primarySample, Button secondarySample)
        {
            if (primarySample != null)
                _gradientSprite = GetFillImage(primarySample)?.sprite;

            if (secondarySample != null)
                _roundedSprite = GetFillImage(secondarySample)?.sprite;
        }

        public static void Apply(Button button, UiButtonStyle style)
        {
            if (button == null)
                return;

            switch (style)
            {
                case UiButtonStyle.Primary:
                    ApplyPrimary(button);
                    break;
                default:
                    ApplySecondary(button);
                    break;
            }
        }

        static void ApplyPrimary(Button button)
        {
            var glow = GetGlowImage(button);
            var fill = GetFillImage(button);
            if (glow == null || fill == null || _gradientSprite == null || _roundedSprite == null)
                return;

            glow.sprite = _roundedSprite;
            glow.type = Image.Type.Sliced;
            glow.color = UiTheme.GlowHighlighted;

            fill.sprite = _gradientSprite;
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
            if (glow == null || fill == null || _roundedSprite == null)
                return;

            glow.sprite = _roundedSprite;
            glow.type = Image.Type.Sliced;
            glow.color = new Color(UiTheme.Glow.r, UiTheme.Glow.g, UiTheme.Glow.b, 0.12f);

            fill.sprite = _roundedSprite;
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
    }
}
