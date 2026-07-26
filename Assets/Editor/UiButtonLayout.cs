using PeopleOfMath.UI;
using TMPro;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class UiButtonLayout
    {
        public const float StandardLabelFontBase = 17f;
        public static readonly Vector2 StandardLabelOffset = new(12f, -18f);
        public static readonly Vector2 StandardLabelSizeDelta = new(-20f, 24f);

        public static readonly Vector2 SectionNavBarPosition = new(0f, 45f);
        public static readonly Vector2 SectionNavBarSize = new(0f, 90f);

        /// <summary>Matches BottomBar HorizontalLayoutGroup left/right padding.</summary>
        public const float EdgeInset = 4f;
        public const float ReferenceCanvasWidth = 1080f;
        public const float NavActionButtonWidth = 220f;
        public const float NavActionButtonHeight = 66f;

        /// <summary>Single-row Figma-style tab bar height (reference 1080 canvas).</summary>
        public const float BottomBarHeight = 148f;
        public static readonly Vector2 BottomBarPosition = new(0f, BottomBarHeight * 0.5f);
        public static readonly Vector2 BottomBarSize = new(0f, BottomBarHeight);

        /// <summary>Icon/selection sit in the upper portion of each equal-width tab cell.</summary>
        public const float TabIconAnchorY = 0.64f;
        public const float TabIconSize = 72f;
        public const float TabSelectionSize = 104f;
        public const float TabCaptionFontBase = 18f;
        public const float TabCaptionHeight = 36f;
        public const float TabCaptionBottomPad = 6f;

        public static Vector2 SectionNavBackPosition =>
            new(EdgeInset, -12f);

        public static Vector2 SectionNavNextPosition =>
            new(ReferenceCanvasWidth - EdgeInset - NavActionButtonWidth, -12f);

        public static Vector2 HeaderBackPosition =>
            new(EdgeInset, -48f);

        public static Vector2 NavActionButtonSize =>
            new(NavActionButtonWidth, NavActionButtonHeight);

        public readonly struct SceneButton
        {
            public string Name { get; }
            public Vector2 Position { get; }
            public Vector2 Size { get; }
            public string LocalizationKey { get; }
            public UiButtonStyle Style { get; }
            public string IconGlyph { get; }
            public NavTabId? TabId { get; }

            public SceneButton(
                string name,
                Vector2 position,
                Vector2 size,
                string localizationKey,
                UiButtonStyle style,
                string iconGlyph = null,
                NavTabId? tabId = null)
            {
                Name = name;
                Position = position;
                Size = size;
                LocalizationKey = localizationKey;
                Style = style;
                IconGlyph = iconGlyph;
                TabId = tabId;
            }
        }

        public static readonly SceneButton BottomBrowse = new(
            "BrowseTab", Vector2.zero, Vector2.zero, "tab_browse", UiButtonStyle.Secondary, tabId: NavTabId.Browse);

        public static readonly SceneButton BottomIndex = new(
            "IndexTab", Vector2.zero, Vector2.zero, "tab_index", UiButtonStyle.Secondary, tabId: NavTabId.Index);

        public static readonly SceneButton BottomFavorites = new(
            "FavoritesTab", Vector2.zero, Vector2.zero, "btn_favorites", UiButtonStyle.Secondary, tabId: NavTabId.Favorites);

        public static readonly SceneButton BottomQuiz = new(
            "QuizTab", Vector2.zero, Vector2.zero, "tab_quiz", UiButtonStyle.Secondary, tabId: NavTabId.Quiz);

        public static readonly SceneButton BottomSettings = new(
            "SettingsTab", Vector2.zero, Vector2.zero, "tab_settings", UiButtonStyle.Secondary, tabId: NavTabId.Settings);

        public static readonly SceneButton BottomAbout = new(
            "AboutTab", Vector2.zero, Vector2.zero, "tab_about", UiButtonStyle.Secondary, tabId: NavTabId.About);

        public static readonly SceneButton HeaderBack = new(
            "BackButton", HeaderBackPosition, NavActionButtonSize, "btn_back", UiButtonStyle.Secondary);

        public static readonly SceneButton SectionNavBack = new(
            "BackButton", SectionNavBackPosition, NavActionButtonSize, "btn_back", UiButtonStyle.Secondary);

        public static readonly SceneButton SectionNavNext = new(
            "NextButton", SectionNavNextPosition, NavActionButtonSize, "btn_next", UiButtonStyle.Secondary);

        public static readonly SceneButton SettingsRussian = new(
            "RuButton", new Vector2(40f, -160f), new Vector2(400f, 64f), "btn_russian", UiButtonStyle.Primary);

        public static readonly SceneButton SettingsEnglish = new(
            "EnButton", new Vector2(40f, -240f), new Vector2(400f, 64f), "btn_english", UiButtonStyle.Secondary);

        public static readonly SceneButton SettingsFontNormal = new(
            "FontNormalButton", new Vector2(40f, -480f), new Vector2(400f, 64f), "btn_font_normal", UiButtonStyle.Primary);

        public static readonly SceneButton SettingsFontLarge = new(
            "FontLargeButton", new Vector2(40f, -560f), new Vector2(400f, 64f), "btn_font_large", UiButtonStyle.Secondary);

        public static readonly SceneButton SettingsFontExtraLarge = new(
            "FontExtraLargeButton", new Vector2(40f, -640f), new Vector2(400f, 64f), "btn_font_extra_large", UiButtonStyle.Secondary);

        public static readonly SceneButton SettingsThemeDark = new(
            "DarkThemeButton", new Vector2(40f, -880f), new Vector2(400f, 64f), "btn_theme_dark", UiButtonStyle.Primary);

        public static readonly SceneButton SettingsThemeLight = new(
            "LightThemeButton", new Vector2(40f, -960f), new Vector2(400f, 64f), "btn_theme_light", UiButtonStyle.Secondary);

        public static readonly SceneButton SettingsThemeGlass = new(
            "GlassThemeButton", new Vector2(40f, -1040f), new Vector2(400f, 64f), "btn_theme_glass", UiButtonStyle.Secondary);

        public static void ApplyTopLeftAnchoredRect(RectTransform rt, Vector2 position, Vector2 size)
        {
            if (rt == null)
                return;

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        public static void ApplyBottomLeftAnchoredRect(RectTransform rt, Vector2 position, Vector2 size)
        {
            if (rt == null)
                return;

            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        public static void ApplyBottomStretchBarRect(RectTransform rt, Vector2 position, Vector2 size)
        {
            if (rt == null)
                return;

            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        public static void ConfigureStandardLabel(GameObject labelGo)
        {
            if (labelGo == null)
                return;

            var rt = labelGo.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = StandardLabelOffset;
                rt.sizeDelta = StandardLabelSizeDelta;
            }

            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                return;

            var fontSize = UiLayoutMetrics.ScaleFont(StandardLabelFontBase);
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = UiTheme.TextPrimary;
            tmp.raycastTarget = false;
        }

        public static void ConfigureTabIcon(RectTransform iconRt)
        {
            if (iconRt == null)
                return;

            iconRt.anchorMin = new Vector2(0.5f, TabIconAnchorY);
            iconRt.anchorMax = new Vector2(0.5f, TabIconAnchorY);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(TabIconSize, TabIconSize);
        }

        public static void ConfigureTabSelection(RectTransform selectionRt)
        {
            if (selectionRt == null)
                return;

            selectionRt.anchorMin = new Vector2(0.5f, TabIconAnchorY);
            selectionRt.anchorMax = new Vector2(0.5f, TabIconAnchorY);
            selectionRt.pivot = new Vector2(0.5f, 0.5f);
            selectionRt.anchoredPosition = Vector2.zero;
            selectionRt.sizeDelta = new Vector2(TabSelectionSize, TabSelectionSize);
        }

        public static void ConfigureTabCaption(GameObject labelGo)
        {
            if (labelGo == null)
                return;

            var rt = labelGo.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, TabCaptionBottomPad);
                rt.sizeDelta = new Vector2(-4f, TabCaptionHeight);
            }

            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                return;

            var fontSize = UiLayoutMetrics.ScaleFont(TabCaptionFontBase);
            tmp.fontSize = fontSize;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Max(14f, fontSize * 0.55f);
            tmp.fontSizeMax = fontSize + 1f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = UiTheme.GetNavTabInactiveLabelColor();
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.fontStyle = FontStyles.Bold;
        }
    }
}
