using System.Collections.Generic;
using PeopleOfMath.Data;
using PeopleOfMath.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PeopleOfMath.Core
{
    public class NavigationController : MonoBehaviour
    {
        [SerializeField] HomePanel homePanel;
        [SerializeField] IndexPanel indexPanel;
        [SerializeField] ListPanel listPanel;
        [SerializeField] DetailPanel detailPanel;
        [SerializeField] SettingsPanel settingsPanel;
        [SerializeField] FavoritesPanel favoritesPanel;
        [SerializeField] CalendarPanel calendarPanel;
        [SerializeField] QuizPanel quizPanel;
        [SerializeField] AboutPanel aboutPanel;
        [SerializeField] UiPanelSlideTransition favoritesTransition;
        [SerializeField] GameObject headerBackButton;
        [SerializeField] HeaderTitleBinder headerTitle;
        [SerializeField] Button browseTab;
        [SerializeField] Button indexTab;
        [SerializeField] Button settingsTab;
        [SerializeField] Button favoritesButton;
        [SerializeField] Button quizTab;
        [SerializeField] Button aboutTab;

        readonly List<ScreenContext> _stack = new();
        int _lastBackFrame = -1;

        public AppScreen CurrentScreen => _stack.Count > 0 ? _stack[^1].Screen : AppScreen.Home;

        public ScreenContext CurrentContext => _stack.Count > 0 ? _stack[^1] : ScreenContext.Home();

        void Awake()
        {
            _stack.Clear();
            _stack.Add(ScreenContext.Home());
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
            calendarPanel?.gameObject.SetActive(false);
            quizPanel?.gameObject.SetActive(false);
            aboutPanel?.gameObject.SetActive(false);
        }

        void HideAllPanelsExceptFavorites()
        {
            homePanel?.gameObject.SetActive(false);
            indexPanel?.gameObject.SetActive(false);
            listPanel?.gameObject.SetActive(false);
            detailPanel?.gameObject.SetActive(false);
            settingsPanel?.gameObject.SetActive(false);
            calendarPanel?.gameObject.SetActive(false);
            quizPanel?.gameObject.SetActive(false);
            aboutPanel?.gameObject.SetActive(false);
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

        void SetRoot(ScreenContext context)
        {
            _stack.Clear();
            _stack.Add(context);
            Present(context);
        }

        void Push(ScreenContext context)
        {
            _stack.Add(context);
            Present(context);
        }

        void Pop()
        {
            if (_stack.Count <= 1)
                return;

            _stack.RemoveAt(_stack.Count - 1);
            Present(_stack[^1], restoring: true);
        }

        void Present(ScreenContext context, bool restoring = false)
        {
            HideAllPanels();

            switch (context.Screen)
            {
                case AppScreen.Home:
                    if (homePanel != null)
                        homePanel.gameObject.SetActive(true);
                    if (headerBackButton != null)
                        headerBackButton.SetActive(false);
                    headerTitle?.SetHomeTitle();
                    break;
                case AppScreen.Index:
                    if (indexPanel != null)
                        indexPanel.gameObject.SetActive(true);
                    if (headerBackButton != null)
                        headerBackButton.SetActive(false);
                    headerTitle?.SetIndexTitle();
                    break;
                case AppScreen.List:
                    if (listPanel != null)
                    {
                        if (context.ListFromSearch)
                            listPanel.BindSearch(context.SearchQuery);
                        else
                            listPanel.BindFilter(context.FilterKind, context.FilterKey);

                        listPanel.gameObject.SetActive(true);
                        if (context.ListFromSearch)
                            headerTitle?.SetSearchTitle(context.SearchQuery, listPanel.LastResultCount);
                        else
                            headerTitle?.SetFilterTitle(context.FilterKind, context.FilterKey);
                    }

                    if (headerBackButton != null)
                        headerBackButton.SetActive(true);
                    break;
                case AppScreen.Detail:
                    if (detailPanel != null)
                    {
                        detailPanel.gameObject.SetActive(true);
                        detailPanel.Bind(context.MathematicianId);
                    }

                    if (headerBackButton != null)
                        headerBackButton.SetActive(true);
                    break;
                case AppScreen.Settings:
                    if (settingsPanel != null)
                        settingsPanel.gameObject.SetActive(true);
                    if (headerBackButton != null)
                        headerBackButton.SetActive(false);
                    headerTitle?.SetSettingsTitle();
                    break;
                case AppScreen.Favorites:
                    PresentFavorites(restoring);
                    break;
                case AppScreen.Calendar:
                    if (calendarPanel != null)
                    {
                        if (!restoring)
                            calendarPanel.ResetToToday();
                        calendarPanel.gameObject.SetActive(true);
                    }

                    if (headerBackButton != null)
                        headerBackButton.SetActive(true);
                    headerTitle?.SetCalendarTitle();
                    break;
                case AppScreen.Quiz:
                    if (quizPanel != null)
                    {
                        quizPanel.gameObject.SetActive(true);
                        if (!restoring || !quizPanel.IsInActiveRound)
                            quizPanel.ShowMenu();
                    }

                    if (headerBackButton != null)
                        headerBackButton.SetActive(true);
                    headerTitle?.SetQuizTitle();
                    break;
                case AppScreen.About:
                    if (aboutPanel != null)
                        aboutPanel.gameObject.SetActive(true);
                    if (headerBackButton != null)
                        headerBackButton.SetActive(false);
                    headerTitle?.SetAboutTitle();
                    break;
            }

            RefreshTabStyles();
        }

        void PresentFavorites(bool restoring)
        {
            if (favoritesPanel == null)
                return;

            HideAllPanelsExceptFavorites();
            if (headerBackButton != null)
                headerBackButton.SetActive(true);
            headerTitle?.SetFavoritesTitle();

            if (!restoring)
            {
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
                return;
            }

            favoritesPanel.gameObject.SetActive(true);
            favoritesPanel.RevealListItemsStaggered();
        }

        public void ShowHome() => SetRoot(ScreenContext.Home());

        public void ShowIndex() => SetRoot(ScreenContext.Index());

        public void ShowSearch(string query)
        {
            query = query?.Trim() ?? "";
            if (string.IsNullOrEmpty(query))
            {
                ShowHome();
                return;
            }

            if (_stack.Count > 0 && _stack[^1].Screen == AppScreen.List && _stack[^1].ListFromSearch)
            {
                _stack[^1] = ScreenContext.ListSearch(query);
                // Keep the list panel active — avoid HideAllPanels + full re-present.
                if (listPanel != null && listPanel.gameObject.activeInHierarchy)
                {
                    var count = listPanel.BindSearch(query);
                    headerTitle?.SetSearchTitle(query, count);
                    RefreshTabStyles();
                    return;
                }

                Present(_stack[^1]);
                return;
            }

            Push(ScreenContext.ListSearch(query));
        }

        public void ShowList(FilterKind kind, string key, bool fromDetail = false)
        {
            if (fromDetail)
            {
                var parent = _stack.Count > 0 ? _stack[^1] : ScreenContext.Home();
                Push(ScreenContext.ListFilter(kind, key, fromDetail: true, mathematicianId: parent.MathematicianId));
                return;
            }

            Push(ScreenContext.ListFilter(kind, key));
        }

        public void ShowListFromDetail(FilterKind kind, string key, string mathematicianId) =>
            Push(ScreenContext.ListFilter(kind, key, fromDetail: true, mathematicianId: mathematicianId));

        public void ShowDetail(string mathematicianId, bool restoreReturnContext = false)
        {
            if (restoreReturnContext)
            {
                Pop();
                return;
            }

            Push(ScreenContext.Detail(mathematicianId));
        }

        public void ShowSettings() => SetRoot(ScreenContext.Settings());

        public void ShowFavorites()
        {
            if (CurrentScreen == AppScreen.Favorites || IsFavoritesAnimating())
                return;

            Push(ScreenContext.Favorites());
        }

        public void ShowCalendar()
        {
            if (CurrentScreen == AppScreen.Calendar)
                return;

            Push(ScreenContext.Calendar());
        }

        public void ShowQuiz() => SetRoot(ScreenContext.Quiz());

        public void ShowAbout() => SetRoot(ScreenContext.About());

        DetailOrigin GetDetailOrigin()
        {
            if (_stack.Count < 2 || _stack[^1].Screen != AppScreen.Detail)
                return DetailOrigin.None;

            var previous = _stack[^2];
            return previous.Screen switch
            {
                AppScreen.Index => DetailOrigin.Index,
                AppScreen.Favorites => DetailOrigin.Favorites,
                AppScreen.Quiz => DetailOrigin.Quiz,
                AppScreen.Calendar => DetailOrigin.Calendar,
                AppScreen.Home => DetailOrigin.Home,
                AppScreen.List when previous.ListFromSearch => DetailOrigin.Search,
                AppScreen.List => DetailOrigin.FilterList,
                _ => DetailOrigin.Home
            };
        }

        public void RefreshTabStyles()
        {
            var ctx = CurrentContext;
            var detailOrigin = GetDetailOrigin();

            var browseActive = ctx.Screen == AppScreen.Home
                || ctx.Screen == AppScreen.List
                || ctx.Screen == AppScreen.Calendar
                || (ctx.Screen == AppScreen.Detail && detailOrigin is DetailOrigin.Home or DetailOrigin.Search or DetailOrigin.FilterList or DetailOrigin.Calendar);
            var indexActive = ctx.Screen == AppScreen.Index
                || (ctx.Screen == AppScreen.Detail && detailOrigin == DetailOrigin.Index);
            var settingsActive = ctx.Screen == AppScreen.Settings;
            var favoritesActive = ctx.Screen == AppScreen.Favorites
                || (ctx.Screen == AppScreen.Detail && detailOrigin == DetailOrigin.Favorites);
            var quizActive = ctx.Screen == AppScreen.Quiz
                || (ctx.Screen == AppScreen.Detail && detailOrigin == DetailOrigin.Quiz);
            var aboutActive = ctx.Screen == AppScreen.About;

            ApplyTabStyle(browseTab, browseActive);
            ApplyTabStyle(indexTab, indexActive);
            ApplyTabStyle(settingsTab, settingsActive);
            ApplyTabStyle(favoritesButton, favoritesActive);
            ApplyTabStyle(quizTab, quizActive);
            ApplyTabStyle(aboutTab, aboutActive);

            EventSystem.current?.SetSelectedGameObject(null);
        }

        static void ApplyTabStyle(Button button, bool active)
        {
            if (button == null)
                return;

            var tabView = button.GetComponent<NavTabView>();
            if (tabView != null)
            {
                tabView.Apply(active);
                return;
            }

            // Legacy tabs without NavTabView.
            UiButtonStyler.Apply(button, active ? UiButtonStyle.Primary : UiButtonStyle.Secondary, showTabIndicator: active);
        }

        public void HandleBack()
        {
            if (IsFavoritesAnimating())
                return;

            var ctx = CurrentContext;
            switch (ctx.Screen)
            {
                case AppScreen.List:
                    Pop();
                    break;
                case AppScreen.Detail:
                    if (detailPanel != null && detailPanel.TryGoBack())
                        break;
                    Pop();
                    break;
                case AppScreen.Favorites:
                    Pop();
                    break;
                case AppScreen.Calendar:
                    Pop();
                    break;
                case AppScreen.Index:
                case AppScreen.Settings:
                case AppScreen.About:
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
            ShowFavorites();
        }

        public void OnQuizTabClicked() => ShowQuiz();

        public void OnAboutTabClicked() => ShowAbout();
    }
}
