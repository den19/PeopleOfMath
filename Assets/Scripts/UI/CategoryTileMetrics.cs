using UnityEngine;

namespace PeopleOfMath.UI
{
    public static class CategoryTileMetrics
    {
        public const float CellWidth = 492f;
        public const float CellHeight = 460f;
        public const float Spacing = 20f;
        public const int Columns = 2;

        public const float MediaHeightRatio = 0.64f;
        public const float LabelHorizontalInset = 28f;
        /// <summary>Distance from tile bottom to the top edge of the Label (bottom-anchored).</summary>
        public const float LabelTopFromBottom = 108f;
        /// <summary>Distance from tile bottom to the top edge of the Count (bottom-anchored).</summary>
        public const float CountTopFromBottom = 52f;
        public const float LabelHeight = 48f;
        public const float CountHeight = 36f;

        public const float TitleBaseFontSize = 18f;
        public const float CountBaseFontSize = 13f;
        public const float GlyphBaseFontSize = 56f;

        public static float MediaHeight => CellHeight * MediaHeightRatio;

        /// <summary>Bottom-anchored Label position (pivot top-left).</summary>
        public static Vector2 LabelOffset =>
            new(LabelHorizontalInset, LabelTopFromBottom);

        /// <summary>Bottom-anchored Count position (pivot top-left).</summary>
        public static Vector2 CountOffset =>
            new(LabelHorizontalInset, CountTopFromBottom);
    }
}
