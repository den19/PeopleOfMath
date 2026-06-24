using PeopleOfMath.Core;
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
                targetCamera.backgroundColor = UiTheme.Background;

            ApplyStructuralTheme();

            foreach (var binding in GetComponentsInChildren<UiThemeBinding>(true))
                binding.Apply();

            foreach (var card in GetComponentsInChildren<UiThemedCard>(true))
                card.Apply();

            navigation?.RefreshTabStyles();
            settingsPanel?.RefreshStatus();
            portraitGallery?.RefreshTheme();
            RefreshSecondaryButtons();
        }

        void ApplyStructuralTheme()
        {
            foreach (var panel in GetComponentsInChildren<Transform>(true))
            {
                if (!panel.name.EndsWith("Panel"))
                    continue;

                var image = panel.GetComponent<Image>();
                if (image != null)
                    image.color = UiTheme.Background;
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

            foreach (var scroll in GetComponentsInChildren<ScrollRect>(true))
            {
                if (scroll.content == null)
                    continue;

                var image = scroll.content.GetComponent<Image>();
                if (image == null || scroll.content.GetComponent<UiThemeBinding>() != null)
                    continue;

                image.color = UiTheme.Background;
            }
        }

        void RefreshSecondaryButtons()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button == null)
                    continue;

                var name = button.gameObject.name;
                if (name is "BackButton" or "NextButton")
                    UiButtonStyler.Apply(button, UiButtonStyle.Secondary);
            }
        }
    }
}
