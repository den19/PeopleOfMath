using UnityEngine;

namespace PeopleOfMath.UI
{
    public static class FilterButtonMetrics
    {
        public const float Height = 208f;
        public const float LabelHorizontalInset = 40f;
        public const float TopInset = 28f;
        public const float BottomInset = 28f;
        public const float VerticalPadding = TopInset + BottomInset;
        public const float FontSizeMinMultiplier = 0.5f;

        public static Vector2 LabelOffset => new Vector2(32f, -TopInset);

        public static float FontSizeMin(float fontSizeMax) =>
            fontSizeMax * FontSizeMinMultiplier;

        public static float LabelHeight(float fontSizeMax) => fontSizeMax;
    }
}
