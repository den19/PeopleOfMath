using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

namespace PeopleOfMath.UI
{
    public class HeaderTitleBinder : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] LocalizeStringEvent homeTitleEvent;
        [SerializeField] LocalizeStringEvent indexTitleEvent;
        [SerializeField] LocalizeStringEvent settingsTitleEvent;
        [SerializeField] LocalizeStringEvent favoritesTitleEvent;
        [SerializeField] LocalizeStringEvent quizTitleEvent;
        [SerializeField] LocalizeStringEvent aboutTitleEvent;
        [SerializeField] LocalizedString detailTitle;

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            ThemeHelper.ThemeChanged += OnThemeChanged;
            RefreshTitleColors();
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            ThemeHelper.ThemeChanged -= OnThemeChanged;
        }

        void OnLocaleChanged(Locale _) => RefreshCurrent();

        void OnThemeChanged() => RefreshTitleColors();

        public void RefreshTitleColors()
        {
            var color = _mode == TitleMode.Filter && _pendingKind.HasValue
                ? UiTheme.GetFilterAccent(_pendingKind.Value)
                : UiTheme.NavBarText;
            if (titleText != null)
                titleText.color = color;

            ApplyTitleColor(homeTitleEvent, color);
            ApplyTitleColor(indexTitleEvent, color);
            ApplyTitleColor(settingsTitleEvent, color);
            ApplyTitleColor(favoritesTitleEvent, color);
            ApplyTitleColor(quizTitleEvent, color);
            ApplyTitleColor(aboutTitleEvent, color);
        }

        static void ApplyTitleColor(LocalizeStringEvent titleEvent, Color color)
        {
            if (titleEvent == null)
                return;

            var tmp = titleEvent.GetComponent<TMP_Text>();
            if (tmp != null)
                tmp.color = color;
        }

        FilterKind? _pendingKind;
        string _pendingKey;
        string _pendingSearchQuery;
        int _pendingSearchCount;
        TitleMode _mode = TitleMode.Home;

        enum TitleMode
        {
            Home,
            Index,
            Settings,
            Filter,
            Search,
            Detail,
            Favorites,
            Quiz,
            About
        }

        void HideLocalizedTitles()
        {
            homeTitleEvent?.gameObject.SetActive(false);
            indexTitleEvent?.gameObject.SetActive(false);
            settingsTitleEvent?.gameObject.SetActive(false);
            favoritesTitleEvent?.gameObject.SetActive(false);
            quizTitleEvent?.gameObject.SetActive(false);
            aboutTitleEvent?.gameObject.SetActive(false);
        }

        public void SetHomeTitle()
        {
            _mode = TitleMode.Home;
            HideLocalizedTitles();
            homeTitleEvent?.gameObject.SetActive(true);
            if (titleText != null)
                titleText.gameObject.SetActive(false);
            if (homeTitleEvent != null)
            {
                homeTitleEvent.enabled = true;
                homeTitleEvent.RefreshString();
            }

            RefreshTitleColors();
        }

        public void SetIndexTitle()
        {
            _mode = TitleMode.Index;
            HideLocalizedTitles();
            indexTitleEvent?.gameObject.SetActive(true);
            if (titleText != null)
                titleText.gameObject.SetActive(false);
            indexTitleEvent?.RefreshString();
            RefreshTitleColors();
        }

        public void SetSettingsTitle()
        {
            _mode = TitleMode.Settings;
            HideLocalizedTitles();
            settingsTitleEvent?.gameObject.SetActive(true);
            if (titleText != null)
                titleText.gameObject.SetActive(false);
            settingsTitleEvent?.RefreshString();
            RefreshTitleColors();
        }

        public void SetFavoritesTitle()
        {
            _mode = TitleMode.Favorites;
            HideLocalizedTitles();
            favoritesTitleEvent?.gameObject.SetActive(true);
            if (titleText != null)
                titleText.gameObject.SetActive(false);
            favoritesTitleEvent?.RefreshString();
            RefreshTitleColors();
        }

        public void SetQuizTitle()
        {
            _mode = TitleMode.Quiz;
            HideLocalizedTitles();
            quizTitleEvent?.gameObject.SetActive(true);
            if (titleText != null)
                titleText.gameObject.SetActive(false);
            quizTitleEvent?.RefreshString();
            RefreshTitleColors();
        }

        public void SetAboutTitle()
        {
            _mode = TitleMode.About;
            HideLocalizedTitles();
            aboutTitleEvent?.gameObject.SetActive(true);
            if (titleText != null)
                titleText.gameObject.SetActive(false);
            aboutTitleEvent?.RefreshString();
            RefreshTitleColors();
        }

        public void SetFilterTitle(FilterKind kind, string key)
        {
            _mode = TitleMode.Filter;
            _pendingKind = kind;
            _pendingKey = key;
            HideLocalizedTitles();
            if (titleText != null)
                titleText.gameObject.SetActive(true);
            RefreshFilterTitle();
            RefreshTitleColors();
        }

        public void SetSearchTitle(string query, int count)
        {
            _mode = TitleMode.Search;
            _pendingSearchQuery = query;
            _pendingSearchCount = count;
            HideLocalizedTitles();
            if (titleText != null)
                titleText.gameObject.SetActive(true);
            RefreshSearchTitle();
            RefreshTitleColors();
        }

        public void SetDetailTitle()
        {
            _mode = TitleMode.Detail;
            HideLocalizedTitles();
            if (titleText != null)
                titleText.gameObject.SetActive(true);
            if (detailTitle != null && titleText != null)
                titleText.text = detailTitle.GetLocalizedString();
            RefreshTitleColors();
        }

        public void SetDetailSectionTitle(string title)
        {
            _mode = TitleMode.Detail;
            HideLocalizedTitles();
            if (titleText != null)
            {
                titleText.gameObject.SetActive(true);
                titleText.text = title;
            }

            RefreshTitleColors();
        }

        void RefreshCurrent()
        {
            switch (_mode)
            {
                case TitleMode.Home:
                    SetHomeTitle();
                    break;
                case TitleMode.Index:
                    SetIndexTitle();
                    break;
                case TitleMode.Settings:
                    SetSettingsTitle();
                    break;
                case TitleMode.Favorites:
                    SetFavoritesTitle();
                    break;
                case TitleMode.Quiz:
                    SetQuizTitle();
                    break;
                case TitleMode.About:
                    SetAboutTitle();
                    break;
                case TitleMode.Filter:
                    if (_pendingKind.HasValue && _pendingKey != null)
                        SetFilterTitle(_pendingKind.Value, _pendingKey);
                    break;
                case TitleMode.Search:
                    if (_pendingSearchQuery != null)
                        SetSearchTitle(_pendingSearchQuery, _pendingSearchCount);
                    break;
                case TitleMode.Detail:
                    SetDetailTitle();
                    break;
            }
        }

        void RefreshFilterTitle()
        {
            if (titleText == null || _pendingKey == null || !_pendingKind.HasValue)
                return;

            var english = LocaleHelper.IsEnglish;
            var label = _pendingKind.Value switch
            {
                FilterKind.Century => Taxonomy.Centuries[_pendingKey].Get(english),
                FilterKind.Country => Taxonomy.Countries[_pendingKey].Get(english),
                FilterKind.Branch => Taxonomy.Branches[_pendingKey].Get(english),
                _ => _pendingKey
            };

            titleText.text = label;
        }

        void RefreshSearchTitle()
        {
            if (titleText == null || _pendingSearchQuery == null)
                return;

            var titleFmt = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "search_results_title");
            var countFmt = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "search_results_count");
            titleText.text = $"{string.Format(titleFmt, _pendingSearchQuery)} · {string.Format(countFmt, _pendingSearchCount)}";
        }
    }
}
