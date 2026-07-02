using PeopleOfMath.Data;
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

        [SerializeField] UiCardVariant variant = UiCardVariant.Filter;
        [SerializeField] FilterKind filterKind = FilterKind.Century;

        bool _highlightActive;

        public UiCardVariant Variant => variant;

        public void Configure(UiCardVariant cardVariant, FilterKind kind = FilterKind.Century)
        {
            variant = cardVariant;
            filterKind = kind;
            Apply();
        }

        public void SetHighlightActive(bool active)
        {
            _highlightActive = active;
            Apply();
        }

        public void Apply()
        {
            var glow = transform.Find(GlowChildName)?.GetComponent<Image>();
            var fill = transform.Find(FillChildName)?.GetComponent<Image>();
            var borderColor = ResolveBorderColor();
            var glowColor = ResolveGlowColor();

            if (glow != null)
                glow.color = glowColor;

            if (fill != null)
            {
                var glassSurface = fill.GetComponent<UiGlassSurface>();
                if (variant == UiCardVariant.Filter)
                {
                    var fillColor = UiTheme.GetFilterFill(filterKind);
                    if (ThemeHelper.IsGlassmorphism && glassSurface != null)
                    {
                        glassSurface.SetTintOverride(fillColor);
                        glassSurface.ApplyGlass(true);
                        glassSurface.RefreshTint();
                    }
                    else
                    {
                        glassSurface?.ClearTintOverride();
                        fill.color = fillColor;
                    }
                }
                else if (ThemeHelper.IsGlassmorphism && glassSurface != null)
                {
                    glassSurface.ClearTintOverride();
                    glassSurface.ApplyGlass(true);
                    glassSurface.RefreshTint();
                }
                else
                {
                    glassSurface?.ClearTintOverride();
                    fill.color = UiTheme.CardFill;
                }

                var border = fill.GetComponent<Outline>();
                if (border != null)
                {
                    if (ThemeHelper.IsGlassmorphism)
                        border.enabled = true;
                    border.effectColor = borderColor;
                }
            }

            var rootImage = GetComponent<Image>();
            if (rootImage != null && glow == null && fill == null)
                rootImage.color = UiTheme.CardFill;

            ApplyCardTextColors();

            var portrait = transform.Find(PortraitChildName)?.GetComponent<Image>();
            if (portrait != null)
                portrait.color = portrait.sprite != null ? Color.white : UiTheme.PortraitPlaceholder;
        }

        void ApplyCardTextColors()
        {
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                switch (text.gameObject.name)
                {
                    case "Label" when variant == UiCardVariant.Filter:
                        text.color = UiTheme.GetFilterTextPrimary(filterKind);
                        break;
                    case "Name":
                    case "Label":
                        text.color = UiTheme.CardTextPrimary;
                        break;
                    case "Dates":
                    case "Bio":
                    case "Body":
                        text.color = UiTheme.CardTextSecondary;
                        break;
                }
            }
        }

        Color ResolveBorderColor()
        {
            if (_highlightActive)
                return variant == UiCardVariant.Filter
                    ? UiTheme.GetFilterBorder(filterKind, highlighted: true)
                    : UiTheme.CardBorderActive;

            return variant == UiCardVariant.Filter
                ? UiTheme.GetFilterBorder(filterKind)
                : UiTheme.CardBorder;
        }

        Color ResolveGlowColor()
        {
            if (variant == UiCardVariant.ListItem)
            {
                return _highlightActive
                    ? new Color(UiTheme.AccentWarm.r, UiTheme.AccentWarm.g, UiTheme.AccentWarm.b, 0.22f)
                    : Color.clear;
            }

            return _highlightActive
                ? UiTheme.GetFilterGlow(filterKind, highlighted: true)
                : UiTheme.GetFilterGlow(filterKind);
        }
    }
}
