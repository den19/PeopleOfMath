using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class UiLayoutMetrics
    {
        public const float FontScale = 1.44f;
        public const float AdditionalFontScale = 1.2f;
        public const float FilterButtonBaseWidth = 100f;
        public const float FilterButtonWidth = 300f;
        public const float FilterButtonBaseHeight = 52f;
        public const float FilterButtonHeight = 78f;

        public static float ScaleFont(float size) => Mathf.Round(size * FontScale);
    }
}
