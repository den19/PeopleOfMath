using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    [RequireComponent(typeof(Button))]
    public class FavoriteIconButton : MonoBehaviour
    {
        [SerializeField] Image iconImage;

        Button _button;
        bool _initialized;
        bool _isFavorite;

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
            {
                _button.transition = Selectable.Transition.None;
                if (_button.GetComponent<UiButtonPressFeedback>() == null)
                    _button.gameObject.AddComponent<UiButtonPressFeedback>();
            }

            ApplyTheme();
            _initialized = true;
        }

        public void SetFavorite(bool isFavorite)
        {
            _isFavorite = isFavorite;
            ApplyTheme();
            RefreshListItemHighlight();
        }

        void ApplyTheme()
        {
            if (iconImage == null)
                return;

            iconImage.sprite = _isFavorite ? UiSprites.HeartFilled : UiSprites.HeartOutline;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.color = _isFavorite ? UiTheme.AccentWarm : UiTheme.CardTextSecondary;
        }

        void RefreshListItemHighlight()
        {
            var themedCard = GetComponentInParent<UiThemedCard>();
            if (themedCard != null && themedCard.Variant == UiCardVariant.ListItem)
                themedCard.SetHighlightActive(_isFavorite);
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
