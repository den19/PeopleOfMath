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

        public static readonly Vector2 BottomBarPosition = new(0f, 70f);
        public static readonly Vector2 BottomBarSize = new(0f, 140f);

        public readonly struct SceneButton
        {
            public string Name { get; }
            public Vector2 Position { get; }
            public Vector2 Size { get; }
            public string LocalizationKey { get; }
            public UiButtonStyle Style { get; }

            public SceneButton(string name, Vector2 position, Vector2 size, string localizationKey, UiButtonStyle style)
            {
                Name = name;
                Position = position;
                Size = size;
                LocalizationKey = localizationKey;
                Style = style;
            }
        }

        public static readonly SceneButton HeaderBack = new(
            "BackButton", new Vector2(20f, -60f), new Vector2(160f, 56f), "btn_back", UiButtonStyle.Secondary);

        public static readonly SceneButton SectionNavBack = new(
            "BackButton", new Vector2(45f, -12f), new Vector2(220f, 66f), "btn_back", UiButtonStyle.Secondary);

        public static readonly SceneButton SectionNavNext = new(
            "NextButton", new Vector2(699f, -12f), new Vector2(220f, 66f), "btn_next", UiButtonStyle.Secondary);

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

        public static readonly SceneButton BottomBrowse = new(
            "BrowseTab", new Vector2(19f, -22f), new Vector2(440f, 72f), "tab_browse", UiButtonStyle.Primary);

        public static readonly SceneButton BottomSettings = new(
            "SettingsTab", new Vector2(520f, -22f), new Vector2(440f, 72f), "tab_settings", UiButtonStyle.Secondary);

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
    }
}
