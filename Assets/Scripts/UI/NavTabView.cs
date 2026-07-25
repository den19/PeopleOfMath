using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    [DisallowMultipleComponent]
    public class NavTabView : MonoBehaviour
    {
        [SerializeField] NavTabId tabId;
        [SerializeField] Image selectionBg;
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text labelText;

        public NavTabId TabId => tabId;

        public void Configure(NavTabId id, Image selection, Image icon, TMP_Text label)
        {
            tabId = id;
            selectionBg = selection;
            iconImage = icon;
            labelText = label;
        }

        public void Apply(bool active)
        {
            var accent = UiTheme.GetTabAccent(tabId);
            if (selectionBg != null)
            {
                selectionBg.enabled = active;
                if (active)
                    selectionBg.color = new Color(accent.r, accent.g, accent.b, ThemeHelper.IsGlassmorphism ? 0.28f : 0.18f);
            }

            if (iconImage != null)
            {
                iconImage.color = active
                    ? accent
                    : new Color(
                        UiTheme.TextSecondary.r,
                        UiTheme.TextSecondary.g,
                        UiTheme.TextSecondary.b,
                        ThemeHelper.IsGlassmorphism ? 0.75f : 0.85f);
            }

            if (labelText != null)
            {
                labelText.color = active
                    ? accent
                    : UiTheme.TextSecondary;
            }
        }
    }
}
