using PeopleOfMath.Data;
using PeopleOfMath.Quiz;
using UnityEngine;

namespace PeopleOfMath.UI
{
    // Append new values at the end only — inserting breaks serialized UiThemeBinding tokens in scenes.
    public enum UiThemeToken
    {
        Background,
        NavBar,
        NavBarAccent,
        CardFill,
        CardBorder,
        CardBorderActive,
        Glow,
        GlowHighlighted,
        TextPrimary,
        TextSecondary,
        ScrollBackground,
        ViewportMask,
        PortraitPlaceholder,
        GalleryDotActive,
        GalleryDotInactive,
        AccentWarm,
        SemanticSuccess,
        SemanticError,
        NavBarText,
        CardTextPrimary,
        CardTextSecondary
    }

    public static class UiTheme
    {
        struct Palette
        {
            public Color CameraBackground;
            public Color Background;
            public Color CardFill;
            public Color CardBorder;
            public Color CardBorderActive;
            public Color Glow;
            public Color GlowHighlighted;
            public Color PrimaryAccent;
            public Color AccentSecondary;
            public Color AccentTertiary;
            public Color AccentWarm;
            public Color AccentMuted;
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
            public Color SemanticSuccess;
            public Color SemanticError;
            public Color SemanticSuccessMuted;
            public Color SemanticErrorMuted;
        }

        static readonly Palette DarkPalette = new()
        {
            CameraBackground = new Color(0.039f, 0.031f, 0.071f, 1f), // #0A0812
            Background = new Color(0.039f, 0.031f, 0.071f, 1f),
            CardFill = new Color(1f, 1f, 1f, 0.06f),
            CardBorder = new Color(0.165f, 0.145f, 0.208f, 1f), // #2A2535
            CardBorderActive = new Color(0.749f, 0.353f, 0.949f, 0.45f),
            Glow = new Color(0.749f, 0.353f, 0.949f, 0.12f),
            GlowHighlighted = new Color(0.749f, 0.353f, 0.949f, 0.4f),
            PrimaryAccent = new Color(0.749f, 0.353f, 0.949f, 1f), // #BF5AF2
            AccentSecondary = new Color(0.482f, 0.420f, 0.949f, 1f), // #7B6BF2
            AccentTertiary = new Color(0.780f, 0.420f, 0.949f, 1f), // #C76BF2
            AccentWarm = new Color(0.910f, 0.659f, 0.220f, 1f), // #E8A838
            AccentMuted = new Color(0.749f, 0.353f, 0.949f, 0.25f),
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
            PrimaryButtonText = Color.white,
            SemanticSuccess = new Color(0.239f, 0.749f, 0.431f, 1f), // #3DBF6E
            SemanticError = new Color(0.878f, 0.271f, 0.271f, 1f), // #E04545
            SemanticSuccessMuted = new Color(0.239f, 0.749f, 0.431f, 0.55f),
            SemanticErrorMuted = new Color(0.878f, 0.271f, 0.271f, 0.85f)
        };

        static readonly Palette LightPalette = new()
        {
            CameraBackground = new Color(0.961f, 0.953f, 0.980f, 1f),
            Background = new Color(0.961f, 0.953f, 0.980f, 1f),
            CardFill = new Color(1f, 1f, 1f, 0.92f),
            CardBorder = new Color(0.894f, 0.886f, 0.925f, 1f), // #E4E2EC
            CardBorderActive = new Color(0.627f, 0.282f, 0.851f, 0.35f),
            Glow = new Color(0.749f, 0.353f, 0.949f, 0.08f),
            GlowHighlighted = new Color(0.749f, 0.353f, 0.949f, 0.28f),
            PrimaryAccent = new Color(0.627f, 0.282f, 0.851f, 1f), // #A048D9
            AccentSecondary = new Color(0.420f, 0.361f, 0.831f, 1f), // #6B5CD4
            AccentTertiary = new Color(0.718f, 0.361f, 0.851f, 1f),
            AccentWarm = new Color(0.831f, 0.580f, 0.165f, 1f), // #D4942A
            AccentMuted = new Color(0.627f, 0.282f, 0.851f, 0.2f),
            PrimaryPressed = new Color(0.627f, 0.125f, 0.941f, 1f),
            TextPrimary = new Color(0.110f, 0.106f, 0.133f, 1f),
            TextSecondary = new Color(0.388f, 0.384f, 0.478f, 1f),
            ButtonSecondaryBorder = new Color(0.894f, 0.886f, 0.925f, 1f),
            ButtonSecondaryFill = new Color(1f, 1f, 1f, 0.75f),
            NavBar = new Color(1f, 1f, 1f, 0.92f),
            ScrollBackground = new Color(0f, 0f, 0f, 0.04f),
            ViewportMask = new Color(1f, 1f, 1f, 0.01f),
            GalleryDotInactive = new Color(0.659f, 0.655f, 0.710f, 0.8f),
            GalleryDotActive = new Color(0.627f, 0.282f, 0.851f, 1f),
            PortraitPlaceholder = new Color(0f, 0f, 0f, 0.05f),
            PrimaryButtonText = Color.white,
            SemanticSuccess = new Color(0.176f, 0.659f, 0.361f, 1f),
            SemanticError = new Color(0.816f, 0.188f, 0.188f, 1f),
            SemanticSuccessMuted = new Color(0.176f, 0.659f, 0.361f, 0.55f),
            SemanticErrorMuted = new Color(0.816f, 0.188f, 0.188f, 0.85f)
        };

        static readonly Palette GlassPalette = new()
        {
            CameraBackground = new Color(0.239f, 0.082f, 0.471f, 1f),
            Background = new Color(0f, 0f, 0f, 0f),
            CardFill = new Color(1f, 1f, 1f, 0.20f),
            CardBorder = new Color(1f, 1f, 1f, 0.30f),
            CardBorderActive = new Color(1f, 1f, 1f, 0.50f),
            Glow = new Color(1f, 1f, 1f, 0.12f),
            GlowHighlighted = new Color(1f, 1f, 1f, 0.22f),
            PrimaryAccent = Color.white,
            AccentSecondary = new Color(0.784f, 0.722f, 1f, 1f), // #C8B8FF
            AccentTertiary = new Color(0.910f, 0.722f, 1f, 1f),
            AccentWarm = new Color(1f, 0.624f, 0.263f, 1f), // #FF9F43
            AccentMuted = new Color(1f, 1f, 1f, 0.18f),
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
            PrimaryButtonText = new Color(0.110f, 0.106f, 0.133f, 1f),
            SemanticSuccess = new Color(0.365f, 0.839f, 0.541f, 1f),
            SemanticError = new Color(1f, 0.420f, 0.420f, 1f),
            SemanticSuccessMuted = new Color(0.365f, 0.839f, 0.541f, 0.55f),
            SemanticErrorMuted = new Color(1f, 0.420f, 0.420f, 0.85f)
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
        public static Color CardBorderActive => Active.CardBorderActive;
        public static Color Glow => Active.Glow;
        public static Color GlowHighlighted => Active.GlowHighlighted;
        public static Color PrimaryAccent => Active.PrimaryAccent;
        public static Color AccentSecondary => Active.AccentSecondary;
        public static Color AccentTertiary => Active.AccentTertiary;
        public static Color AccentWarm => Active.AccentWarm;
        public static Color AccentMuted => Active.AccentMuted;
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
        public static Color SemanticSuccess => Active.SemanticSuccess;
        public static Color SemanticError => Active.SemanticError;
        public static Color SemanticSuccessMuted => Active.SemanticSuccessMuted;
        public static Color SemanticErrorMuted => Active.SemanticErrorMuted;

        public static Color NavBarText =>
            ThemeHelper.IsGlassmorphism ? PrimaryButtonText : TextPrimary;

        public static Color CardTextPrimary =>
            ThemeHelper.IsGlassmorphism ? PrimaryButtonText : TextPrimary;

        public static Color CardTextSecondary =>
            ThemeHelper.IsGlassmorphism
                ? new Color(0.388f, 0.384f, 0.478f, 1f)
                : TextSecondary;

        // Itten triadic hues (120° apart): century = warm orange, country = green, branch = blue-violet.
        const float CenturyHue = 32f;
        const float CountryHue = 148f;
        const float BranchHue = 268f;

        static float FilterHue(FilterKind kind) => kind switch
        {
            FilterKind.Country => CountryHue,
            FilterKind.Branch => BranchHue,
            _ => CenturyHue
        };

        public static Color GetFilterAccent(FilterKind kind)
        {
            var hue = FilterHue(kind);
            return ThemeHelper.Current switch
            {
                AppTheme.Light => FromHsl(hue, 0.72f, 0.42f),
                AppTheme.Glassmorphism => FromHsl(hue, 0.78f, 0.62f),
                _ => FromHsl(hue, 0.82f, 0.62f)
            };
        }

        public static Color GetFilterFill(FilterKind kind)
        {
            var hue = FilterHue(kind);
            return ThemeHelper.Current switch
            {
                AppTheme.Light => FromHsl(hue, 0.48f, 0.90f, 0.72f),
                AppTheme.Glassmorphism => FromHsl(hue, 0.70f, 0.72f, 0.34f),
                _ => FromHsl(hue, 0.58f, 0.32f, 0.38f)
            };
        }

        public static Color GetFilterTextPrimary(FilterKind kind)
        {
            if (ThemeHelper.IsGlassmorphism)
                return CardTextPrimary;

            var fill = GetFilterFill(kind);
            var surface = CompositeOverBackground(fill);
            return RelativeLuminance(surface) > 0.55f
                ? new Color(0.11f, 0.106f, 0.133f, 1f)
                : Color.white;
        }

        public static Color GetFilterTextSecondary(FilterKind kind)
        {
            var primary = GetFilterTextPrimary(kind);
            if (primary == Color.white)
                return new Color(1f, 1f, 1f, 0.72f);

            return new Color(0.388f, 0.384f, 0.478f, 1f);
        }

        public static Color GetFilterGlow(FilterKind kind, bool highlighted = false)
        {
            var accent = GetFilterAccent(kind);
            var alpha = highlighted ? 0.35f : 0.12f;
            return new Color(accent.r, accent.g, accent.b, alpha);
        }

        public static Color GetFilterBorder(FilterKind kind, bool highlighted = false)
        {
            var accent = GetFilterAccent(kind);
            var alpha = highlighted ? 0.55f : 0.32f;
            return new Color(accent.r, accent.g, accent.b, alpha);
        }

        static Color CompositeOverBackground(Color foreground)
        {
            var background = ThemeHelper.IsGlassmorphism
                ? new Color(0.239f, 0.082f, 0.471f, 1f)
                : Background;
            var alpha = foreground.a;
            return new Color(
                foreground.r * alpha + background.r * (1f - alpha),
                foreground.g * alpha + background.g * (1f - alpha),
                foreground.b * alpha + background.b * (1f - alpha),
                1f);
        }

        static float RelativeLuminance(Color color) =>
            0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;

        static Color FromHsl(float hue, float saturation, float lightness, float alpha = 1f)
        {
            var chroma = (1f - Mathf.Abs(2f * lightness - 1f)) * saturation;
            var huePrime = hue / 60f;
            var x = chroma * (1f - Mathf.Abs(huePrime % 2f - 1f));
            float r1, g1, b1;

            if (huePrime < 1f)
            {
                r1 = chroma;
                g1 = x;
                b1 = 0f;
            }
            else if (huePrime < 2f)
            {
                r1 = x;
                g1 = chroma;
                b1 = 0f;
            }
            else if (huePrime < 3f)
            {
                r1 = 0f;
                g1 = chroma;
                b1 = x;
            }
            else if (huePrime < 4f)
            {
                r1 = 0f;
                g1 = x;
                b1 = chroma;
            }
            else if (huePrime < 5f)
            {
                r1 = x;
                g1 = 0f;
                b1 = chroma;
            }
            else
            {
                r1 = chroma;
                g1 = 0f;
                b1 = x;
            }

            var match = lightness - chroma * 0.5f;
            return new Color(r1 + match, g1 + match, b1 + match, alpha);
        }

        public static Color GetQuizModeAccent(QuizMode mode) => mode switch
        {
            QuizMode.Portrait => PrimaryAccent,
            QuizMode.Fact => AccentSecondary,
            _ => AccentTertiary
        };

        public static bool IsTextToken(UiThemeToken token) => token switch
        {
            UiThemeToken.TextPrimary or UiThemeToken.TextSecondary
                or UiThemeToken.CardTextPrimary or UiThemeToken.CardTextSecondary
                or UiThemeToken.NavBarText => true,
            _ => false
        };

        public static Color GetToken(UiThemeToken token) => token switch
        {
            UiThemeToken.NavBar => NavBar,
            UiThemeToken.NavBarAccent => PrimaryAccent,
            UiThemeToken.CardFill => CardFill,
            UiThemeToken.CardBorder => CardBorder,
            UiThemeToken.CardBorderActive => CardBorderActive,
            UiThemeToken.Glow => Glow,
            UiThemeToken.GlowHighlighted => GlowHighlighted,
            UiThemeToken.TextPrimary => TextPrimary,
            UiThemeToken.TextSecondary => TextSecondary,
            UiThemeToken.ScrollBackground => ScrollBackground,
            UiThemeToken.ViewportMask => ViewportMask,
            UiThemeToken.PortraitPlaceholder => PortraitPlaceholder,
            UiThemeToken.GalleryDotActive => GalleryDotActive,
            UiThemeToken.GalleryDotInactive => GalleryDotInactive,
            UiThemeToken.AccentWarm => AccentWarm,
            UiThemeToken.SemanticSuccess => SemanticSuccess,
            UiThemeToken.SemanticError => SemanticError,
            UiThemeToken.NavBarText => NavBarText,
            UiThemeToken.CardTextPrimary => CardTextPrimary,
            UiThemeToken.CardTextSecondary => CardTextSecondary,
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
