using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    [DisallowMultipleComponent]
    public class UiThemedCard : MonoBehaviour
    {
        const string GlowChildName = "Glow";
        const string FillChildName = "Fill";
        const string PortraitChildName = "Portrait";

        public void Apply()
        {
            var glow = transform.Find(GlowChildName)?.GetComponent<Image>();
            var fill = transform.Find(FillChildName)?.GetComponent<Image>();
            if (glow != null)
                glow.color = UiTheme.Glow;

            if (fill != null)
            {
                var glassSurface = fill.GetComponent<UiGlassSurface>();
                if (ThemeHelper.IsGlassmorphism && glassSurface != null)
                {
                    glassSurface.RefreshTint();
                }
                else
                {
                    fill.color = UiTheme.CardFill;
                }

                var border = fill.GetComponent<Outline>();
                if (border != null)
                    border.effectColor = UiTheme.CardBorder;
            }

            var rootImage = GetComponent<Image>();
            if (rootImage != null && glow == null && fill == null)
                rootImage.color = UiTheme.CardFill;

            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                switch (text.gameObject.name)
                {
                    case "Name":
                    case "Label":
                        text.color = UiTheme.TextPrimary;
                        break;
                    case "Dates":
                    case "Bio":
                    case "Body":
                        text.color = UiTheme.TextSecondary;
                        break;
                }
            }

            var portrait = transform.Find(PortraitChildName)?.GetComponent<Image>();
            if (portrait != null)
                portrait.color = portrait.sprite != null ? Color.white : UiTheme.PortraitPlaceholder;
        }
    }
}
