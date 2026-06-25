using PeopleOfMath.Core;
using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace PeopleOfMath.UI
{
    public class FavoritesPanel : MonoBehaviour
    {
        const string ListItemResourceName = "MathematicianListItem";

        [SerializeField] NavigationController navigation;
        [SerializeField] MathematicianRepository repository;
        [SerializeField] Transform listContent;
        [SerializeField] MathematicianListItem itemPrefab;
        [SerializeField] GameObject emptyState;

        void Awake()
        {
            if (itemPrefab == null)
                itemPrefab = Resources.Load<MathematicianListItem>(ListItemResourceName);
        }

        void OnEnable()
        {
            FavoritesHelper.FavoritesChanged += OnFavoritesChanged;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            FontSizeHelper.FontSizeChanged += OnFontSizeChanged;
            ThemeHelper.ThemeChanged += OnThemeChanged;
            Refresh();
        }

        void OnDisable()
        {
            FavoritesHelper.FavoritesChanged -= OnFavoritesChanged;
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            FontSizeHelper.FontSizeChanged -= OnFontSizeChanged;
            ThemeHelper.ThemeChanged -= OnThemeChanged;
        }

        void OnFavoritesChanged() => Refresh();

        void OnLocaleChanged(UnityEngine.Localization.Locale _) => Refresh();

        void OnFontSizeChanged() => Refresh();

        void OnThemeChanged() => RefreshTheme();

        void RefreshTheme()
        {
            if (listContent == null)
                return;

            foreach (Transform child in listContent)
                child.GetComponent<UiThemedCard>()?.Apply();
        }

        void Refresh()
        {
            foreach (Transform child in listContent)
                Destroy(child.gameObject);

            if (repository == null)
                return;

            var favorites = new System.Collections.Generic.List<MathematicianData>();
            foreach (var id in FavoritesHelper.GetOrderedIds())
            {
                var data = repository.GetById(id);
                if (data != null)
                    favorites.Add(data);
            }

            emptyState?.SetActive(favorites.Count == 0);
            UpdateEmptyStateMessage();

            if (itemPrefab == null)
            {
                Debug.LogError(
                    "FavoritesPanel: MathematicianListItem prefab is not assigned. " +
                    "Run PeopleOfMath → Regenerate Main Scene or add Assets/Resources/MathematicianListItem.prefab.");
                return;
            }

            foreach (var data in favorites)
            {
                var item = Instantiate(itemPrefab, listContent);
                item.Bind(data, id => navigation.ShowDetail(id));
            }

            GetComponent<FontSizeScope>()?.Apply();
        }

        void UpdateEmptyStateMessage()
        {
            if (emptyState == null)
                return;

            var text = emptyState.GetComponent<TMP_Text>();
            if (text == null)
                return;

            text.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "empty_favorites");
        }
    }
}
