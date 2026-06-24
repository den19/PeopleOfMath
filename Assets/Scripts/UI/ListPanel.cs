using PeopleOfMath.Core;
using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace PeopleOfMath.UI
{
    public class ListPanel : MonoBehaviour
    {
        const string ListItemResourceName = "MathematicianListItem";

        enum ListMode
        {
            None,
            Filter,
            Search
        }

        [SerializeField] NavigationController navigation;
        [SerializeField] MathematicianRepository repository;
        [SerializeField] Transform listContent;
        [SerializeField] MathematicianListItem itemPrefab;
        [SerializeField] GameObject emptyState;

        ListMode _mode = ListMode.None;
        FilterKind _kind;
        string _key;
        string _searchQuery;
        int _lastResultCount;

        void Awake()
        {
            if (itemPrefab == null)
                itemPrefab = Resources.Load<MathematicianListItem>(ListItemResourceName);
        }

        public void BindFilter(FilterKind kind, string key)
        {
            _mode = ListMode.Filter;
            _kind = kind;
            _key = key;
            Refresh();
        }

        public int BindSearch(string query)
        {
            _mode = ListMode.Search;
            _searchQuery = query?.Trim() ?? "";
            Refresh();
            return _lastResultCount;
        }

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            FontSizeHelper.FontSizeChanged += OnFontSizeChanged;
            ThemeHelper.ThemeChanged += OnThemeChanged;
            Refresh();
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            FontSizeHelper.FontSizeChanged -= OnFontSizeChanged;
            ThemeHelper.ThemeChanged -= OnThemeChanged;
        }

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

            var english = LocaleHelper.IsEnglish;
            var results = _mode switch
            {
                ListMode.Filter when !string.IsNullOrEmpty(_key) =>
                    FilterService.Filter(repository.All, _kind, _key, english),
                ListMode.Search when !string.IsNullOrWhiteSpace(_searchQuery) =>
                    SearchService.Search(repository.All, _searchQuery, english),
                _ => null
            };

            if (results == null)
                return;

            _lastResultCount = results.Count;
            emptyState?.SetActive(results.Count == 0);
            UpdateEmptyStateMessage();

            if (itemPrefab == null)
            {
                Debug.LogError(
                    "ListPanel: MathematicianListItem prefab is not assigned. " +
                    "Run PeopleOfMath → Regenerate Main Scene or add Assets/Resources/MathematicianListItem.prefab.");
                return;
            }

            foreach (var data in results)
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

            var key = _mode == ListMode.Search ? "empty_search" : "empty_list";
            text.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
        }
    }
}
