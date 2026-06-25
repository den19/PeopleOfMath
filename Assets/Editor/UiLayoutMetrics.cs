using PeopleOfMath.UI;
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
        public static float FilterButtonFontSizeMin =>
            FilterButtonMetrics.FontSizeMin(FilterButtonFontSize);
        public static float FilterButtonLabelHeight =>
            FilterButtonMetrics.Height - FilterButtonMetrics.VerticalPadding;
        public static Vector2 FilterButtonLabelOffset => FilterButtonMetrics.LabelOffset;

        public const float SearchBarBaseHeight = 56f;
        public const int SearchBarMarginTop = 24;
        public const int SearchBarMarginBottom = 16;
        public const float SearchBarBaseFontSize = 16f;
        public const float SearchBarIconInset = 28f;
        public const float SearchBarClearButtonWidth = 72f;

        public static float SearchBarHeight => ScaleFont(SearchBarBaseHeight) * 2f;
        public static float SearchBarFontSize => ScaleFont(SearchBarBaseFontSize) * 2f;
        public static float SearchBarTotalTopInset =>
            SearchBarMarginTop + SearchBarHeight + SearchBarMarginBottom;

        public const int BrowseScrollPaddingLeft = 32;
        public const int BrowseScrollPaddingRight = 32;
        public const int BrowseScrollPaddingTop = 32;
        public const int BrowseScrollPaddingBottom = 48;
        public const float BrowseScrollSpacing = 28f;

        public const int GroupPaddingTop = 12;
        public const int GroupPaddingBottom = 24;
        public const float GroupSpacing = 20f;

        public const float SectionLabelBaseFontSize = 18f;
        public static float SectionLabelFontSize => ScaleFont(SectionLabelBaseFontSize) * 2f;
        public static float SectionLabelHeight => ScaleFont(72f);

        public const float ListItemRowHeight = 480f;
        public const float ListItemHorizontalInset = 40f;
        public const float ListItemTopPadding = 20f;
        public const float ListItemVerticalGap = 8f;
        public const float ListItemTextLineHeight = 48f;
        public const float ListItemBioHeight = 200f;
        public const float ListItemThumbnailSize = 180f;
        public const float ListItemThumbnailGap = 20f;
        public const float ListItemLeftPadding = 20f;
        public const float ListItemTextRightInset = 20f;
        public const float ListItemShareButtonWidth = 72f;
        public static float ListItemTextColumnLeft =>
            ListItemLeftPadding + ListItemThumbnailSize + ListItemThumbnailGap;
        public static float ListItemTextWidthInset =>
            ListItemTextColumnLeft + ListItemTextRightInset + ListItemShareButtonWidth;
        public const float ListItemNameBaseFontSize = 20f;
        public const float ListItemDatesBaseFontSize = 14f;
        public const float ListItemBioBaseFontSize = 13f;

        public static float ListItemNameFontSize => ScaleFont(ListItemNameBaseFontSize) * 2f;
        public static float ListItemDatesFontSize => ScaleFont(ListItemDatesBaseFontSize) * 2f;
        public static float ListItemBioFontSize => ScaleFont(ListItemBioBaseFontSize) * 2f;
        public static float ListItemNameHeight => ListItemNameFontSize * 2f;
        public static Vector2 ListItemNamePos => new Vector2(ListItemTextColumnLeft, -ListItemTopPadding);
        public static Vector2 ListItemDatesPos => new Vector2(
            ListItemTextColumnLeft, -(ListItemTopPadding + ListItemNameHeight + ListItemVerticalGap));
        public static Vector2 ListItemBioPos => new Vector2(
            ListItemTextColumnLeft,
            -(ListItemTopPadding + ListItemNameHeight + ListItemVerticalGap
              + ListItemTextLineHeight + ListItemVerticalGap));

        public static float LetterButtonWidth => ScaleFont(64f);
        public static float LetterButtonHeight => ScaleFont(64f);
        public const float LetterButtonBaseFontSize = 18f;
        public static int LetterStripHeight => Mathf.RoundToInt(ScaleFont(88f));
        public static int LetterStripMarginTop => Mathf.RoundToInt(ScaleFont(16f));
        public static int LetterStripMarginBottom => Mathf.RoundToInt(ScaleFont(12f));
        public static float LetterStripSpacing => ScaleFont(8f);
        public static int LetterStripPaddingLeft => Mathf.RoundToInt(ScaleFont(24f));
        public static int LetterStripPaddingRight => Mathf.RoundToInt(ScaleFont(24f));

        public static float LetterButtonFontSize => ScaleFont(LetterButtonBaseFontSize) * 2f;

        public const float EmptyStateBaseFontSize = 16f;
        public static float EmptyStateFontSize => ScaleFont(EmptyStateBaseFontSize) * 2f;
        public static Vector2 EmptyStatePosition => new Vector2(80f, -400f);
        public static float EmptyStateLineHeight = 48f;

        public const float DetailContentScale = 2f;
        public const float DetailSectionPadding = 24f;
        public const float DetailSectionSpacing = 16f;
        public const float DetailScrollContentPadding = 16f;
        public const float DetailScrollContentSpacing = 8f;
        public const float DetailScrollMinHeight = 240f;
        public const float DetailFieldPadding = 10f;
        public const float DetailCaptionBaseFontSize = 11f;
        public const float DetailGalleryDotsHeight = 24f;
        public const float DetailGalleryDotSize = 10f;
        public const float DetailGalleryBottomInset = 48f;
        public const float DetailGalleryTopInset = 40f;

        public static float ScaleDetailFont(float baseSize) => ScaleFont(baseSize) * DetailContentScale;
        public static float ScaleDetailSize(float size) => ScaleFont(size) * DetailContentScale;
        public static int ScaleDetailPadding(float value) => Mathf.RoundToInt(value * DetailContentScale);

        public static float ScaleFont(float size) => Mathf.Round(size * FontScale);
    }
}
