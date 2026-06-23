using PeopleOfMath.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.Editor
{
    public static class UiStyleBuilder
    {
        const string GlowChildName = "Glow";
        const string FillChildName = "Fill";
        const float GlowExpand = 6f;

        public static void ApplyPanelBackground(Image image, Color color)
        {
            if (image == null)
                return;

            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = color;
        }

        public static void ApplyCardStyle(GameObject root, UiCardVariant variant)
        {
            if (root == null)
                return;

            UiSpriteFactory.EnsureSprites();
            var sprite = UiSpriteFactory.RoundedRect;

            ClearRootImage(root);

            var glow = GetOrCreateChild(root.transform, GlowChildName);
            ConfigureStretchLayer(glow, GlowExpand, sprite, UiTheme.Glow);

            var button = root.GetComponent<Button>();
            var fill = GetOrCreateChild(root.transform, FillChildName);
            ConfigureStretchLayer(fill, 1f, sprite, UiTheme.CardFill, raycastTarget: button != null);

            var border = fill.GetComponent<Outline>() ?? fill.AddComponent<Outline>();
            border.effectColor = UiTheme.CardBorder;
            border.effectDistance = new Vector2(1f, -1f);
            border.useGraphicAlpha = true;

            if (button != null)
            {
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
                colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                colors.selectedColor = Color.white;
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                button.colors = colors;
                button.targetGraphic = fill.GetComponent<Image>();
            }

            MoveLabelAboveFill(root.transform);
        }

        public static void ApplyPrimaryButton(GameObject root)
        {
            if (root == null)
                return;

            UiSpriteFactory.EnsureSprites();
            var rounded = UiSpriteFactory.RoundedRect;
            var gradient = UiSpriteFactory.ButtonGradient;

            ClearRootImage(root);

            var glow = GetOrCreateChild(root.transform, GlowChildName);
            ConfigureStretchLayer(glow, GlowExpand, rounded, UiTheme.GlowHighlighted);

            var fill = GetOrCreateChild(root.transform, FillChildName);
            ConfigureStretchLayer(fill, 1f, gradient, Color.white, raycastTarget: true);

            var border = fill.GetComponent<Outline>() ?? fill.AddComponent<Outline>();
            border.effectColor = UiTheme.CardBorder;
            border.effectDistance = new Vector2(1f, -1f);
            border.useGraphicAlpha = true;

            ConfigureButton(root, fill.GetComponent<Image>());
            MoveLabelAboveFill(root.transform);
            SetLabelColor(root.transform, UiTheme.TextPrimary);
        }

        public static void ApplySecondaryButton(GameObject root)
        {
            if (root == null)
                return;

            UiSpriteFactory.EnsureSprites();
            var sprite = UiSpriteFactory.RoundedRect;

            ClearRootImage(root);

            var glow = GetOrCreateChild(root.transform, GlowChildName);
            ConfigureStretchLayer(glow, GlowExpand, sprite, new Color(UiTheme.Glow.r, UiTheme.Glow.g, UiTheme.Glow.b, 0.12f));

            var fill = GetOrCreateChild(root.transform, FillChildName);
            ConfigureStretchLayer(fill, 1f, sprite, UiTheme.ButtonSecondaryFill, raycastTarget: true);

            var border = fill.GetComponent<Outline>() ?? fill.AddComponent<Outline>();
            border.effectColor = UiTheme.ButtonSecondaryBorder;
            border.effectDistance = new Vector2(1f, -1f);
            border.useGraphicAlpha = true;

            ConfigureButton(root, fill.GetComponent<Image>());
            MoveLabelAboveFill(root.transform);
            SetLabelColor(root.transform, UiTheme.TextPrimary);
        }

        public static void ApplyNavBarStyle(GameObject bar)
        {
            if (bar == null)
                return;

            var image = bar.GetComponent<Image>();
            ApplyPanelBackground(image, UiTheme.NavBar);

            UiSpriteFactory.EnsureSprites();
            var glowLine = GetOrCreateChild(bar.transform, "TopGlow");
            var rt = glowLine.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, 2);

            var glowImage = glowLine.GetComponent<Image>() ?? glowLine.AddComponent<Image>();
            glowImage.sprite = null;
            glowImage.color = UiTheme.PrimaryAccent;
            glowImage.raycastTarget = false;
            glowLine.transform.SetAsFirstSibling();
        }

        public static void ApplyScrollBackground(Image image)
        {
            if (image == null)
                return;

            UiSpriteFactory.EnsureSprites();
            image.sprite = UiSpriteFactory.RoundedRect;
            image.type = Image.Type.Sliced;
            image.color = UiTheme.ScrollBackground;
        }

        static void ConfigureButton(GameObject root, Image targetGraphic)
        {
            var button = root.GetComponent<Button>();
            if (button == null)
                return;

            button.targetGraphic = targetGraphic;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = UiTheme.PrimaryPressed;
            colors.selectedColor = UiTheme.PrimaryAccent;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;
        }

        static GameObject GetOrCreateChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
                return existing.gameObject;

            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void ClearRootImage(GameObject root)
        {
            var rootImage = root.GetComponent<Image>();
            if (rootImage == null)
                return;

            rootImage.sprite = null;
            rootImage.type = Image.Type.Simple;
            rootImage.color = Color.clear;
            rootImage.raycastTarget = false;
        }

        static void ConfigureStretchLayer(GameObject go, float inset, Sprite sprite, Color color, bool raycastTarget = false)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = raycastTarget;
        }

        static void MoveLabelAboveFill(Transform root)
        {
            var glow = root.Find(GlowChildName);
            var fill = root.Find(FillChildName);
            if (glow != null)
                glow.SetAsFirstSibling();
            if (fill != null)
                fill.SetSiblingIndex(glow != null ? 1 : 0);

            foreach (Transform child in root)
            {
                if (child.name is GlowChildName or FillChildName)
                    continue;
                child.SetAsLastSibling();
            }
        }

        static void SetLabelColor(Transform root, Color color)
        {
            foreach (Transform child in root)
            {
                if (child.name is GlowChildName or FillChildName)
                    continue;

                var tmp = child.GetComponent<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                    tmp.color = color;
            }
        }
    }
}
