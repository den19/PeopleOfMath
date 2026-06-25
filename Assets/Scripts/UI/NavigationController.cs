using PeopleOfMath.Data;
using PeopleOfMath.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PeopleOfMath.Core
{
    public enum AppScreen
    {
        Home,
        Index,
        List,
        Detail,
        Settings
    }

    public class NavigationController : MonoBehaviour
    {
        [SerializeField] HomePanel homePanel;
        [SerializeField] IndexPanel indexPanel;
        [SerializeField] ListPanel listPanel;
        [SerializeField] DetailPanel detailPanel;
        [SerializeField] SettingsPanel settingsPanel;
        [SerializeField] GameObject headerBackButton;
        [SerializeField] HeaderTitleBinder headerTitle;
        [SerializeField] Button browseTab;
        [SerializeField] Button indexTab;
        [SerializeField] Button settingsTab;

        AppScreen _screen = AppScreen.Home;
        FilterKind _filterKind;
        string _filterKey;
        string _searchQuery;
        bool _listFromSearch;
        bool _detailFromIndex;
        string _selectedMathematicianId;

        public AppScreen CurrentScreen => _screen;

        void Awake()
        {
            HideAllPanels();
            WireHeaderBackButton();
        }

        void WireHeaderBackButton()
        {
            if (headerBackButton == null)
                return;

            var button = headerBackButton.GetComponent<Button>();
            if (button == null)
                return;

            button.onClick.RemoveListener(OnBackButtonClicked);
            button.onClick.AddListener(OnBackButtonClicked);
        }

        void HideAllPanels()
        {
            homePanel?.gameObject.SetActive(false);
            indexPanel?.gameObject.SetActive(false);
            listPanel?.gameObject.SetActive(false);
            detailPanel?.gameObject.SetActive(false);
            settingsPanel?.gameObject.SetActive(false);
        }

        public void ShowHome()
        {
            _listFromSearch = false;
            _screen = AppScreen.Home;
            HideAllPanels();
            homePanel.gameObject.SetActive(true);
            headerBackButton.SetActive(false);
            headerTitle?.SetHomeTitle();
            RefreshTabStyles();
        }

        public void ShowIndex()
        {
            _listFromSearch = false;
            _screen = AppScreen.Index;
            HideAllPanels();
            indexPanel.gameObject.SetActive(true);
            headerBackButton.SetActive(false);
            headerTitle?.SetIndexTitle();
            RefreshTabStyles();
        }

        public void ShowSearch(string query)
        {
            _searchQuery = query?.Trim() ?? "";
            if (string.IsNullOrEmpty(_searchQuery))
            {
                ShowHome();
                return;
            }

            _listFromSearch = true;
            _screen = AppScreen.List;
            HideAllPanels();
            var count = listPanel.BindSearch(_searchQuery);
            listPanel.gameObject.SetActive(true);
            headerBackButton.SetActive(true);
            headerTitle?.SetSearchTitle(_searchQuery, count);
            RefreshTabStyles();
        }

        public void ShowList(FilterKind kind, string key)
        {
            _listFromSearch = false;
            _filterKind = kind;
            _filterKey = key;
            _screen = AppScreen.List;
            HideAllPanels();
            listPanel.BindFilter(kind, key);
            listPanel.gameObject.SetActive(true);
            headerBackButton.SetActive(true);
            headerTitle?.SetFilterTitle(kind, key);
            RefreshTabStyles();
        }

        public void ShowDetail(string mathematicianId)
        {
            _detailFromIndex = _screen == AppScreen.Index;
            _selectedMathematicianId = mathematicianId;
            _screen = AppScreen.Detail;
            HideAllPanels();
            detailPanel.gameObject.SetActive(true);
            detailPanel.Bind(mathematicianId);
            headerBackButton.SetActive(true);
            RefreshTabStyles();
        }

        public void ShowSettings()
        {
            _screen = AppScreen.Settings;
            HideAllPanels();
            settingsPanel.gameObject.SetActive(true);
            headerBackButton.SetActive(false);
            headerTitle?.SetSettingsTitle();
            RefreshTabStyles();
        }

        public void RefreshTabStyles()
        {
            var browseActive = _screen == AppScreen.Home
                || _screen == AppScreen.List
                || (_screen == AppScreen.Detail && !_detailFromIndex);
            var indexActive = _screen == AppScreen.Index
                || (_screen == AppScreen.Detail && _detailFromIndex);
            var settingsActive = _screen == AppScreen.Settings;
            UiButtonStyler.Apply(browseTab, browseActive ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            UiButtonStyler.Apply(indexTab, indexActive ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            UiButtonStyler.Apply(settingsTab, settingsActive ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        public void HandleBack()
        {
            switch (_screen)
            {
                case AppScreen.List:
                    ShowHome();
                    break;
                case AppScreen.Detail:
                    if (detailPanel != null && detailPanel.TryGoBack())
                        break;
                    if (_detailFromIndex)
                        ShowIndex();
                    else if (_listFromSearch)
                        ShowSearch(_searchQuery);
                    else
                        ShowList(_filterKind, _filterKey);
                    break;
                case AppScreen.Index:
                case AppScreen.Settings:
                    ShowHome();
                    break;
            }
        }

        public void OnBackButtonClicked() => HandleBack();

        public void OnBrowseTabClicked() => ShowHome();

        public void OnIndexTabClicked() => ShowIndex();

        public void OnSettingsTabClicked() => ShowSettings();
    }
}
