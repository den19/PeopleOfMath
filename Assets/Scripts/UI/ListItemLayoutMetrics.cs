namespace PeopleOfMath.UI
{
    public static class ListItemLayoutMetrics
    {
        public const float TopPadding = 20f;
        public const float VerticalGap = 8f;
        public const float LeftPadding = 20f;
        public const float RightPadding = 20f;
        public const float PortraitSize = 180f;
        public const float PortraitGap = 20f;
        public const float TextColumnLeft = LeftPadding + PortraitSize + PortraitGap;
        public const float ActionButtonSize = 108f / 1.3f;
        public const float PortraitActionGap = 8f;
        public const float ActionButtonGap = 0f;
        public const float TextWidthInset = TextColumnLeft + RightPadding;
        public const float BioBaseHeight = 200f;
        public const float RowMinHeight = 480f;
        public const float NameFontSizeMinMultiplier = 0.5f;
        public const float NameLineHeightFactor = 1.1f;
        public const int NameMaxLines = 2;

        public static float ActionButtonsTop => TopPadding + PortraitSize + PortraitActionGap;

        public static float FavoriteButtonX => LeftPadding;

        public static float ShareButtonX => FavoriteButtonX;

        public static float CalendarButtonX => FavoriteButtonX;

        public static float FavoriteButtonY => ActionButtonsTop;

        public static float ShareButtonY => FavoriteButtonY + ActionButtonSize + ActionButtonGap;

        public static float CalendarButtonY => ShareButtonY + ActionButtonSize + ActionButtonGap;

        public static float LeftColumnHeight =>
            TopPadding + PortraitSize + PortraitActionGap
            + ActionButtonSize + ActionButtonGap
            + ActionButtonSize + ActionButtonGap
            + ActionButtonSize
            + TopPadding;

        public static float NameBlockHeight(float fontSizeMax) =>
            fontSizeMax * NameLineHeightFactor * NameMaxLines;
    }
}
