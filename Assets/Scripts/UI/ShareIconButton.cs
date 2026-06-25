using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    [RequireComponent(typeof(Button))]
    public class ShareIconButton : MonoBehaviour
    {
        [SerializeField] Image iconImage;

        Button _button;
        bool _initialized;

        void Awake() => EnsureInitialized();

        void OnEnable()
        {
            ThemeHelper.ThemeChanged += ApplyTheme;
            EnsureInitialized();
        }

        void OnDisable() => ThemeHelper.ThemeChanged -= ApplyTheme;

        void EnsureInitialized()
        {
            if (_initialized)
                return;

            _button = GetComponent<Button>();

            if (iconImage == null)
                iconImage = transform.Find("Icon")?.GetComponent<Image>();

            var background = GetComponent<Image>();
            if (background != null)
            {
                background.color = Color.clear;
                background.raycastTarget = true;
            }

            if (_button != null)
                _button.transition = Selectable.Transition.None;

            ApplyIcon();
            ApplyTheme();
            _initialized = true;
        }

        void ApplyIcon()
        {
            if (iconImage == null)
                return;

            iconImage.sprite = UiSprites.ShareIcon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        void ApplyTheme()
        {
            if (iconImage != null)
                iconImage.color = UiTheme.TextSecondary;
        }

        public void SetClickHandler(UnityAction handler)
        {
            EnsureInitialized();
            if (_button == null)
                return;

            _button.onClick.RemoveAllListeners();
            if (handler != null)
                _button.onClick.AddListener(handler);
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
