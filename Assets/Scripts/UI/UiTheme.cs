using UnityEngine;

namespace PeopleOfMath.UI
{
    public enum UiThemeToken
    {
        Background,
        NavBar,
        NavBarAccent,
        CardFill,
        CardBorder,
        Glow,
        GlowHighlighted,
        TextPrimary,
        TextSecondary,
        ScrollBackground,
        ViewportMask,
        PortraitPlaceholder,
        GalleryDotActive,
        GalleryDotInactive
    }

    public static class UiTheme
    {
        struct Palette
        {
            public Color CameraBackground;
            public Color Background;
            public Color CardFill;
            public Color CardBorder;
            public Color Glow;
            public Color GlowHighlighted;
            public Color PrimaryAccent;
            public Color PrimaryPressed;
            public Color TextPrimary;
            public Color TextSecondary;
            public Color ButtonSecondaryBorder;
            public Color ButtonSecondaryFill;
            public Color NavBar;
            public Color ScrollBackground;
            public Color ViewportMask;
            public Color GalleryDotInactive;
            public Color GalleryDotActive;
            public Color PortraitPlaceholder;
            public Color PrimaryButtonText;
        }

        // DarkPalette: original Syncra look (black + purple accent).
        static readonly Palette DarkPalette = new()
        {
            CameraBackground = Color.black,
            Background = Color.black,
            CardFill = new Color(1f, 1f, 1f, 0.06f),
            CardBorder = new Color(0.749f, 0.353f, 0.949f, 0.45f),
            Glow = new Color(0.749f, 0.353f, 0.949f, 0.25f),
            GlowHighlighted = new Color(0.749f, 0.353f, 0.949f, 0.4f),
            PrimaryAccent = new Color(0.749f, 0.353f, 0.949f, 1f),
            PrimaryPressed = new Color(0.627f, 0.125f, 0.941f, 1f),
            TextPrimary = Color.white,
            TextSecondary = new Color(0.557f, 0.557f, 0.576f, 1f),
            ButtonSecondaryBorder = new Color(0.227f, 0.227f, 0.235f, 1f),
            ButtonSecondaryFill = new Color(0f, 0f, 0f, 0f),
            NavBar = new Color(0f, 0f, 0f, 0.85f),
            ScrollBackground = new Color(0f, 0f, 0f, 0.35f),
            ViewportMask = new Color(0f, 0f, 0f, 0.01f),
            GalleryDotInactive = new Color(0.557f, 0.557f, 0.576f, 0.8f),
            GalleryDotActive = new Color(0.749f, 0.353f, 0.949f, 1f),
            PortraitPlaceholder = new Color(1f, 1f, 1f, 0.06f),
            PrimaryButtonText = Color.white
        };

        static readonly Palette LightPalette = new()
        {
            CameraBackground = new Color(0.961f, 0.953f, 0.980f, 1f),
            Background = new Color(0.961f, 0.953f, 0.980f, 1f),
            CardFill = new Color(1f, 1f, 1f, 0.92f),
            CardBorder = new Color(0.749f, 0.353f, 0.949f, 0.35f),
            Glow = new Color(0.749f, 0.353f, 0.949f, 0.15f),
            GlowHighlighted = new Color(0.749f, 0.353f, 0.949f, 0.28f),
            PrimaryAccent = new Color(0.749f, 0.353f, 0.949f, 1f),
            PrimaryPressed = new Color(0.627f, 0.125f, 0.941f, 1f),
            TextPrimary = new Color(0.110f, 0.106f, 0.133f, 1f),
            TextSecondary = new Color(0.388f, 0.384f, 0.478f, 1f),
            ButtonSecondaryBorder = new Color(0.894f, 0.886f, 0.925f, 1f),
            ButtonSecondaryFill = new Color(1f, 1f, 1f, 0.75f),
            NavBar = new Color(1f, 1f, 1f, 0.92f),
            ScrollBackground = new Color(0f, 0f, 0f, 0.04f),
            ViewportMask = new Color(1f, 1f, 1f, 0.01f),
            GalleryDotInactive = new Color(0.659f, 0.655f, 0.710f, 0.8f),
            GalleryDotActive = new Color(0.749f, 0.353f, 0.949f, 1f),
            PortraitPlaceholder = new Color(0f, 0f, 0f, 0.05f),
            PrimaryButtonText = Color.white
        };

        static readonly Palette GlassPalette = new()
        {
            CameraBackground = new Color(0.239f, 0.082f, 0.471f, 1f), // #3D1578
            Background = new Color(0f, 0f, 0f, 0f),
            CardFill = new Color(1f, 1f, 1f, 0.14f),
            CardBorder = new Color(1f, 1f, 1f, 0.30f),
            Glow = new Color(1f, 1f, 1f, 0.12f),
            GlowHighlighted = new Color(1f, 1f, 1f, 0.22f),
            PrimaryAccent = Color.white,
            PrimaryPressed = new Color(0.92f, 0.92f, 0.94f, 1f),
            TextPrimary = Color.white,
            TextSecondary = new Color(1f, 1f, 1f, 0.72f),
            ButtonSecondaryBorder = new Color(1f, 1f, 1f, 0.35f),
            ButtonSecondaryFill = new Color(1f, 1f, 1f, 0.10f),
            NavBar = new Color(1f, 1f, 1f, 0.15f),
            ScrollBackground = new Color(0f, 0f, 0f, 0f),
            ViewportMask = new Color(0f, 0f, 0f, 0.01f),
            GalleryDotInactive = new Color(1f, 1f, 1f, 0.45f),
            GalleryDotActive = Color.white,
            PortraitPlaceholder = new Color(1f, 1f, 1f, 0.12f),
            PrimaryButtonText = new Color(0.110f, 0.106f, 0.133f, 1f)
        };

        static Palette Active => ThemeHelper.Current switch
        {
            AppTheme.Light => LightPalette,
            AppTheme.Glassmorphism => GlassPalette,
            _ => DarkPalette
        };

        public static Color CameraBackground => Active.CameraBackground;
        public static Color Background => Active.Background;
        public static Color SurfaceBackground => ThemeHelper.IsGlassmorphism ? Color.clear : Active.Background;
        public static Color CardFill => Active.CardFill;
        public static Color CardBorder => Active.CardBorder;
        public static Color Glow => Active.Glow;
        public static Color GlowHighlighted => Active.GlowHighlighted;
        public static Color PrimaryAccent => Active.PrimaryAccent;
        public static Color PrimaryPressed => Active.PrimaryPressed;
        public static Color TextPrimary => Active.TextPrimary;
        public static Color TextSecondary => Active.TextSecondary;
        public static Color ButtonSecondaryBorder => Active.ButtonSecondaryBorder;
        public static Color ButtonSecondaryFill => Active.ButtonSecondaryFill;
        public static Color NavBar => Active.NavBar;
        public static Color ScrollBackground => Active.ScrollBackground;
        public static Color ViewportMask => Active.ViewportMask;
        public static Color GalleryDotInactive => Active.GalleryDotInactive;
        public static Color GalleryDotActive => Active.GalleryDotActive;
        public static Color PortraitPlaceholder => Active.PortraitPlaceholder;
        public static Color PrimaryButtonText => Active.PrimaryButtonText;

        public static Color GetToken(UiThemeToken token) => token switch
        {
            UiThemeToken.NavBar => NavBar,
            UiThemeToken.NavBarAccent => PrimaryAccent,
            UiThemeToken.CardFill => CardFill,
            UiThemeToken.CardBorder => CardBorder,
            UiThemeToken.Glow => Glow,
            UiThemeToken.GlowHighlighted => GlowHighlighted,
            UiThemeToken.TextPrimary => TextPrimary,
            UiThemeToken.TextSecondary => TextSecondary,
            UiThemeToken.ScrollBackground => ScrollBackground,
            UiThemeToken.ViewportMask => ViewportMask,
            UiThemeToken.PortraitPlaceholder => PortraitPlaceholder,
            UiThemeToken.GalleryDotActive => GalleryDotActive,
            UiThemeToken.GalleryDotInactive => GalleryDotInactive,
            _ => SurfaceBackground
        };
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
