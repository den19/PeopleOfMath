using UnityEngine;

namespace PeopleOfMath.UI
{
    public static class UiTheme
    {
        public static readonly Color Background = Color.black;

        public static readonly Color CardFill = new(1f, 1f, 1f, 0.06f);
        public static readonly Color CardBorder = new(0.749f, 0.353f, 0.949f, 0.45f);
        public static readonly Color Glow = new(0.749f, 0.353f, 0.949f, 0.25f);
        public static readonly Color GlowHighlighted = new(0.749f, 0.353f, 0.949f, 0.4f);

        public static readonly Color PrimaryAccent = new(0.749f, 0.353f, 0.949f, 1f);
        public static readonly Color PrimaryPressed = new(0.627f, 0.125f, 0.941f, 1f);

        public static readonly Color TextPrimary = Color.white;
        public static readonly Color TextSecondary = new(0.557f, 0.557f, 0.576f, 1f);

        public static readonly Color ButtonSecondaryBorder = new(0.227f, 0.227f, 0.235f, 1f);
        public static readonly Color ButtonSecondaryFill = new(0f, 0f, 0f, 0f);

        public static readonly Color NavBar = new(0f, 0f, 0f, 0.85f);
        public static readonly Color ScrollBackground = new(0f, 0f, 0f, 0.35f);
        public static readonly Color ViewportMask = new(0f, 0f, 0f, 0.01f);

        public static readonly Color GalleryDotInactive = new(0.557f, 0.557f, 0.576f, 0.8f);
        public static readonly Color GalleryDotActive = new(0.749f, 0.353f, 0.949f, 1f);

        public static readonly Color PortraitPlaceholder = new(1f, 1f, 1f, 0.06f);
    }

    public enum UiCardVariant
    {
        Filter,
        ListItem
    }

    public enum UiButtonStyle
    {
        Primary,
        Secondary
    }
}
