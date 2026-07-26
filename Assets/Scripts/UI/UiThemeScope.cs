using PeopleOfMath.Core;
using PeopleOfMath.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class UiThemeScope : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [SerializeField] NavigationController navigation;
        [SerializeField] SettingsPanel settingsPanel;
        [SerializeField] PortraitGalleryView portraitGallery;
        [SerializeField] GlassThemeController glassController;

        void OnEnable()
        {
            ThemeHelper.ThemeChanged += Apply;
            Apply();
        }

        void OnDisable()
        {
            ThemeHelper.ThemeChanged -= Apply;
        }

        public void Apply()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (targetCamera != null)
                targetCamera.backgroundColor = UiTheme.CameraBackground;

            if (glassController == null)
                glassController = GetComponent<GlassThemeController>();

            glassController?.Apply();

            ApplyStructuralTheme();

            foreach (var binding in GetComponentsInChildren<UiThemeBinding>(true))
            {
                if (binding.GetComponent<TMP_Text>() != null && binding.GetComponentInParent<UiThemedCard>() != null)
                    continue;

                binding.Apply();
            }

            RefreshSemanticTextColors();

            foreach (var card in GetComponentsInChildren<UiThemedCard>(true))
                card.Apply();

            glassController?.ApplyGlassSurfaces();
            ApplyScrollSurfaces();

            navigation?.RefreshTabStyles();
            settingsPanel?.RefreshStatus();
            portraitGallery?.RefreshTheme();
            foreach (var about in GetComponentsInChildren<AboutPanel>(true))
                about.RefreshTheme();
            RefreshSecondaryButtons();
            RefreshHeaderTitles();
        }

        void RefreshHeaderTitles()
        {
            foreach (var binder in GetComponentsInChildren<HeaderTitleBinder>(true))
                binder.RefreshTitleColors();
        }

        void RefreshSemanticTextColors()
        {
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.GetComponentInParent<UiThemedCard>() != null)
                    continue;

                if (text.GetComponentInParent<Button>() != null)
                    continue;

                if (text.GetComponentInParent<OnboardingOverlay>() != null)
                    continue;

                if (text.GetComponentInParent<ConfirmDialogOverlay>() != null)
                    continue;

                var name = text.gameObject.name;
                if (name == "section_century")
                    text.color = UiTheme.GetFilterAccent(FilterKind.Century);
                else if (name == "section_country")
                    text.color = UiTheme.GetFilterAccent(FilterKind.Country);
                else if (name == "section_branch")
                    text.color = UiTheme.GetFilterAccent(FilterKind.Branch);
                else if (name == "Label")
                {
                    var detailSection = text.GetComponentInParent<LabeledTextDetailSection>();
                    text.color = detailSection != null
                        ? UiTheme.GetFilterAccent(detailSection.FilterKind)
                        : UiTheme.TextPrimary;
                }
                else if (name.StartsWith("section_")
                    || name is "Name" or "HomeTitle" or "IndexTitle" or "SettingsTitle"
                        or "FavoritesTitle" or "PlainTitle")
                {
                    text.color = UiTheme.TextPrimary;
                }
                else if (name is "Empty" or "Dates" or "Body" or "Caption")
                {
                    text.color = UiTheme.TextSecondary;
                }
            }
        }

        void ApplyStructuralTheme()
        {
            foreach (var panel in GetComponentsInChildren<Transform>(true))
            {
                if (!panel.name.EndsWith("Panel"))
                    continue;

                var image = panel.GetComponent<Image>();
                if (image != null)
                    image.color = UiTheme.SurfaceBackground;
            }

            foreach (var node in GetComponentsInChildren<Transform>(true))
            {
                if (node.name != "DecorGlow")
                    continue;

                var image = node.GetComponent<Image>();
                if (image != null)
                    image.color = UiTheme.Glow;
            }

            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.GetComponent<UiThemeBinding>() != null)
                    continue;

                if (text.gameObject.name == "Empty")
                    text.color = UiTheme.TextSecondary;
            }

        }

        void ApplyScrollSurfaces()
        {
            foreach (var scroll in GetComponentsInChildren<ScrollRect>(true))
            {
                var scrollImage = scroll.GetComponent<Image>();
                if (scrollImage != null)
                    scrollImage.color = UiTheme.ScrollBackground;

                if (scroll.viewport != null)
                {
                    var viewportImage = scroll.viewport.GetComponent<Image>();
                    if (viewportImage != null)
                        viewportImage.color = UiTheme.ViewportMask;
                }

                if (scroll.content == null)
                    continue;

                var contentImage = scroll.content.GetComponent<Image>();
                if (contentImage != null)
                    contentImage.color = UiTheme.SurfaceBackground;
            }
        }

        void RefreshSecondaryButtons()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button == null)
                    continue;

                if (button.GetComponentInParent<OnboardingOverlay>() != null)
                    continue;

                if (button.GetComponentInParent<ConfirmDialogOverlay>() != null)
                    continue;

                var name = button.gameObject.name;
                if (name is "BackButton" or "NextButton" or "WikipediaButton" or "WikidataButton")
                    UiButtonStyler.Apply(button, UiButtonStyle.Secondary);
            }
        }
    }
}
