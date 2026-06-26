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
        Settings,
        Favorites,
        Quiz
    }

    public class NavigationController : MonoBehaviour
    {
        [SerializeField] HomePanel homePanel;
        [SerializeField] IndexPanel indexPanel;
        [SerializeField] ListPanel listPanel;
        [SerializeField] DetailPanel detailPanel;
        [SerializeField] SettingsPanel settingsPanel;
        [SerializeField] FavoritesPanel favoritesPanel;
        [SerializeField] QuizPanel quizPanel;
        [SerializeField] UiPanelSlideTransition favoritesTransition;
        [SerializeField] GameObject headerBackButton;
        [SerializeField] HeaderTitleBinder headerTitle;
        [SerializeField] Button browseTab;
        [SerializeField] Button indexTab;
        [SerializeField] Button settingsTab;
        [SerializeField] Button favoritesButton;
        [SerializeField] Button quizTab;

        AppScreen _screen = AppScreen.Home;
        FilterKind _filterKind;
        string _filterKey;
        string _searchQuery;
        bool _listFromSearch;
        bool _listFromDetail;
        bool _detailFromIndex;
        bool _detailFromFavorites;
        bool _detailFromQuiz;
        string _selectedMathematicianId;
        int _lastBackFrame = -1;

        bool _detailReturnFromHome;
        bool _detailReturnFromIndex;
        bool _detailReturnFromFavorites;
        bool _detailReturnFromQuiz;
        bool _detailReturnFromSearch;
        string _detailReturnSearchQuery;
        bool _detailReturnFromFilterList;
        FilterKind _detailReturnFilterKind;
        string _detailReturnFilterKey;

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

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnBackButtonClicked);
        }

        void HideAllPanels()
        {
            homePanel?.gameObject.SetActive(false);
            indexPanel?.gameObject.SetActive(false);
            listPanel?.gameObject.SetActive(false);
            detailPanel?.gameObject.SetActive(false);
            settingsPanel?.gameObject.SetActive(false);
            favoritesPanel?.gameObject.SetActive(false);
            quizPanel?.gameObject.SetActive(false);
        }

        void HideAllPanelsExceptFavorites()
        {
            homePanel?.gameObject.SetActive(false);
            indexPanel?.gameObject.SetActive(false);
            listPanel?.gameObject.SetActive(false);
            detailPanel?.gameObject.SetActive(false);
            settingsPanel?.gameObject.SetActive(false);
            quizPanel?.gameObject.SetActive(false);
        }

        UiPanelSlideTransition GetFavoritesTransition()
        {
            if (favoritesTransition != null)
                return favoritesTransition;

            if (favoritesPanel == null)
                return null;

            favoritesTransition = favoritesPanel.GetComponent<UiPanelSlideTransition>();
            return favoritesTransition;
        }

        bool IsFavoritesAnimating()
        {
            var transition = GetFavoritesTransition();
            return transition != null && transition.IsAnimating;
        }

        public void ShowHome()
        {
            _listFromSearch = false;
            _listFromDetail = false;
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
            _listFromDetail = false;
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
            _listFromDetail = false;
            _screen = AppScreen.List;
            HideAllPanels();
            var count = listPanel.BindSearch(_searchQuery);
            listPanel.gameObject.SetActive(true);
            headerBackButton.SetActive(true);
            headerTitle?.SetSearchTitle(_searchQuery, count);
            RefreshTabStyles();
        }

        public void ShowList(FilterKind kind, string key, bool fromDetail = false)
        {
            _listFromSearch = false;
            _listFromDetail = fromDetail;
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

        public void ShowListFromDetail(FilterKind kind, string key, string mathematicianId)
        {
            _selectedMathematicianId = mathematicianId;
            ShowList(kind, key, fromDetail: true);
        }

        public void ShowDetail(string mathematicianId, bool restoreReturnContext = false)
        {
            if (restoreReturnContext)
            {
                _detailFromIndex = _detailReturnFromIndex;
                _detailFromFavorites = _detailReturnFromFavorites;
                _detailFromQuiz = _detailReturnFromQuiz;
                _listFromSearch = _detailReturnFromSearch;
                _searchQuery = _detailReturnSearchQuery;
                if (_detailReturnFromFilterList)
                {
                    _filterKind = _detailReturnFilterKind;
                    _filterKey = _detailReturnFilterKey;
                }
            }
            else
            {
                _detailReturnFromHome = _screen == AppScreen.Home;
                _detailReturnFromIndex = _screen == AppScreen.Index;
                _detailReturnFromFavorites = _screen == AppScreen.Favorites;
                _detailReturnFromQuiz = _screen == AppScreen.Quiz;
                _detailReturnFromSearch = _screen == AppScreen.List && _listFromSearch;
                _detailReturnSearchQuery = _searchQuery;
                _detailReturnFromFilterList = _screen == AppScreen.List && !_listFromSearch;
                _detailReturnFilterKind = _filterKind;
                _detailReturnFilterKey = _filterKey;

                _detailFromIndex = _detailReturnFromIndex;
                _detailFromFavorites = _detailReturnFromFavorites;
                _detailFromQuiz = _detailReturnFromQuiz;
            }

            _listFromDetail = false;
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

        public void ShowFavorites()
        {
            if (_screen == AppScreen.Favorites || IsFavoritesAnimating())
                return;

            _screen = AppScreen.Favorites;
            HideAllPanelsExceptFavorites();
            headerBackButton.SetActive(true);
            headerTitle?.SetFavoritesTitle();
            RefreshTabStyles();

            favoritesPanel.PrepareAnimatedOpen();
            favoritesPanel.gameObject.SetActive(true);

            var transition = GetFavoritesTransition();
            if (transition == null)
            {
                favoritesPanel.RevealListItemsStaggered();
                return;
            }

            transition.SnapClosed();
            transition.PlayOpen(() => favoritesPanel.RevealListItemsStaggered());
        }

        public void ShowQuiz()
        {
            _listFromSearch = false;
            _listFromDetail = false;
            _screen = AppScreen.Quiz;
            HideAllPanels();
            quizPanel.gameObject.SetActive(true);
            quizPanel.ShowMenu();
            headerBackButton.SetActive(true);
            headerTitle?.SetQuizTitle();
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
            var favoritesActive = _screen == AppScreen.Favorites
                || (_screen == AppScreen.Detail && _detailFromFavorites);
            var quizActive = _screen == AppScreen.Quiz
                || (_screen == AppScreen.Detail && _detailFromQuiz);
            UiButtonStyler.Apply(browseTab, browseActive ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            UiButtonStyler.Apply(indexTab, indexActive ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            UiButtonStyler.Apply(settingsTab, settingsActive ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            if (favoritesButton != null)
                UiButtonStyler.Apply(favoritesButton, favoritesActive ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            if (quizTab != null)
                UiButtonStyler.Apply(quizTab, quizActive ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        public void HandleBack()
        {
            if (IsFavoritesAnimating())
                return;

            switch (_screen)
            {
                case AppScreen.List:
                    if (_listFromDetail)
                        ShowDetail(_selectedMathematicianId, restoreReturnContext: true);
                    else
                        ShowHome();
                    break;
                case AppScreen.Detail:
                    if (detailPanel != null && detailPanel.TryGoBack())
                        break;
                    if (_detailFromIndex)
                        ShowIndex();
                    else if (_detailFromFavorites)
                        ShowFavorites();
                    else if (_detailFromQuiz)
                        ShowQuiz();
                    else if (_listFromSearch)
                        ShowSearch(_searchQuery);
                    else if (_detailReturnFromFilterList)
                        ShowList(_detailReturnFilterKind, _detailReturnFilterKey);
                    else if (_detailReturnFromHome)
                        ShowHome();
                    else
                        ShowList(_filterKind, _filterKey);
                    break;
                case AppScreen.Index:
                case AppScreen.Settings:
                case AppScreen.Favorites:
                    ShowHome();
                    break;
                case AppScreen.Quiz:
                    if (quizPanel != null && quizPanel.TryHandleBack())
                        break;
                    ShowHome();
                    break;
            }
        }

        public void OnBackButtonClicked()
        {
            if (_lastBackFrame == Time.frameCount)
                return;

            _lastBackFrame = Time.frameCount;
            HandleBack();
        }

        public void OnBrowseTabClicked() => ShowHome();

        public void OnIndexTabClicked() => ShowIndex();

        public void OnSettingsTabClicked() => ShowSettings();

        public void OnFavoritesButtonClicked()
        {
            if (IsFavoritesAnimating())
                return;

            ShowFavorites();
        }

        public void OnQuizTabClicked() => ShowQuiz();
    }
}
