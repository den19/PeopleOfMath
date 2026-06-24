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
        [SerializeField] LocalizeStringEvent settingsTitleEvent;
        [SerializeField] LocalizedString detailTitle;

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        void OnLocaleChanged(Locale _) => RefreshCurrent();

        FilterKind? _pendingKind;
        string _pendingKey;
        string _pendingSearchQuery;
        int _pendingSearchCount;
        TitleMode _mode = TitleMode.Home;

        enum TitleMode
        {
            Home,
            Settings,
            Filter,
            Search,
            Detail
        }

        public void SetHomeTitle()
        {
            _mode = TitleMode.Home;
            homeTitleEvent?.gameObject.SetActive(true);
            settingsTitleEvent?.gameObject.SetActive(false);
            if (titleText != null)
                titleText.gameObject.SetActive(false);
            if (homeTitleEvent != null)
            {
                homeTitleEvent.enabled = true;
                homeTitleEvent.RefreshString();
            }
        }

        public void SetSettingsTitle()
        {
            _mode = TitleMode.Settings;
            homeTitleEvent?.gameObject.SetActive(false);
            settingsTitleEvent?.gameObject.SetActive(true);
            if (titleText != null)
                titleText.gameObject.SetActive(false);
            settingsTitleEvent?.RefreshString();
        }

        public void SetFilterTitle(FilterKind kind, string key)
        {
            _mode = TitleMode.Filter;
            _pendingKind = kind;
            _pendingKey = key;
            homeTitleEvent?.gameObject.SetActive(false);
            settingsTitleEvent?.gameObject.SetActive(false);
            if (titleText != null)
                titleText.gameObject.SetActive(true);
            RefreshFilterTitle();
        }

        public void SetSearchTitle(string query, int count)
        {
            _mode = TitleMode.Search;
            _pendingSearchQuery = query;
            _pendingSearchCount = count;
            homeTitleEvent?.gameObject.SetActive(false);
            settingsTitleEvent?.gameObject.SetActive(false);
            if (titleText != null)
                titleText.gameObject.SetActive(true);
            RefreshSearchTitle();
        }

        public void SetDetailTitle()
        {
            _mode = TitleMode.Detail;
            homeTitleEvent?.gameObject.SetActive(false);
            settingsTitleEvent?.gameObject.SetActive(false);
            if (titleText != null)
                titleText.gameObject.SetActive(true);
            if (detailTitle != null && titleText != null)
                titleText.text = detailTitle.GetLocalizedString();
        }

        public void SetDetailSectionTitle(string title)
        {
            _mode = TitleMode.Detail;
            homeTitleEvent?.gameObject.SetActive(false);
            settingsTitleEvent?.gameObject.SetActive(false);
            if (titleText != null)
            {
                titleText.gameObject.SetActive(true);
                titleText.text = title;
            }
        }

        void RefreshCurrent()
        {
            switch (_mode)
            {
                case TitleMode.Home:
                    SetHomeTitle();
                    break;
                case TitleMode.Settings:
                    SetSettingsTitle();
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
