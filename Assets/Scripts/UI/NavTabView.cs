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
                var inactive = UiTheme.GetNavTabInactiveIconColor();
                iconImage.color = active ? accent : inactive;
            }

            if (labelText != null)
            {
                labelText.color = active
                    ? accent
                    : UiTheme.GetNavTabInactiveLabelColor();
                // Bold inactive labels stay legible on near-white Light nav bars.
                labelText.fontStyle = active || ThemeHelper.Current != AppTheme.Light
                    ? FontStyles.Normal
                    : FontStyles.Bold;
            }
        }
    }
}
