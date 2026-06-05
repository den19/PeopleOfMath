using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class UiLayoutMetrics
    {
        public const float FontScale = 1.44f;
        public const float AdditionalFontScale = 1.2f;

        public const float FilterButtonWidth = 1440f;
        public const float FilterButtonHeight = 208f;
        public const float FilterButtonBaseFontSize = 18f;
        public const float FilterButtonLabelHorizontalInset = 40f;

        public static float FilterButtonFontSize => ScaleFont(FilterButtonBaseFontSize) * 4f;
        public static float FilterButtonLabelHeight => ScaleFont(FilterButtonBaseFontSize) * 4f;
        public static Vector2 FilterButtonLabelOffset => new Vector2(32f, -28f);

        public const int BrowseScrollPaddingLeft = 48;
        public const int BrowseScrollPaddingRight = 48;
        public const int BrowseScrollPaddingTop = 32;
        public const int BrowseScrollPaddingBottom = 48;
        public const float BrowseScrollSpacing = 24f;

        public const int GroupPaddingTop = 8;
        public const int GroupPaddingBottom = 24;
        public const float GroupSpacing = 16f;

        public const float SectionLabelBaseFontSize = 18f;
        public static float SectionLabelFontSize => ScaleFont(SectionLabelBaseFontSize) * 2f;
        public static float SectionLabelHeight => ScaleFont(72f);

        public const float ListItemRowHeight = 240f;
        public const float ListItemHorizontalInset = 40f;
        public const float ListItemTextLineHeight = 48f;
        public const float ListItemBioHeight = 100f;
        public const float ListItemNameBaseFontSize = 20f;
        public const float ListItemDatesBaseFontSize = 14f;
        public const float ListItemBioBaseFontSize = 13f;

        public static float ListItemNameFontSize => ScaleFont(ListItemNameBaseFontSize) * 2f;
        public static float ListItemDatesFontSize => ScaleFont(ListItemDatesBaseFontSize) * 2f;
        public static float ListItemBioFontSize => ScaleFont(ListItemBioBaseFontSize) * 2f;
        public static Vector2 ListItemNamePos => new Vector2(20f, -20f);
        public static Vector2 ListItemDatesPos => new Vector2(20f, -76f);
        public static Vector2 ListItemBioPos => new Vector2(20f, -116f);

        public const float EmptyStateBaseFontSize = 16f;
        public static float EmptyStateFontSize => ScaleFont(EmptyStateBaseFontSize) * 2f;
        public static Vector2 EmptyStatePosition => new Vector2(80f, -400f);
        public static float EmptyStateLineHeight = 48f;

        public static float ScaleFont(float size) => Mathf.Round(size * FontScale);
    }
}
