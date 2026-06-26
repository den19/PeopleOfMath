using System.Collections.Generic;
using PeopleOfMath.Core;
using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using PeopleOfMath.Quiz;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class QuizPanel : MonoBehaviour
    {
        enum QuizViewState
        {
            Menu,
            Playing,
            Feedback,
            Results
        }

        [SerializeField] NavigationController navigation;
        [SerializeField] MathematicianRepository repository;

        [SerializeField] GameObject menuView;
        [SerializeField] GameObject playingView;
        [SerializeField] GameObject feedbackView;
        [SerializeField] GameObject resultsView;

        [SerializeField] Button modePortraitButton;
        [SerializeField] Button modeFactButton;
        [SerializeField] Button modeMixedButton;
        [SerializeField] Button startButton;
        [SerializeField] TMP_Text bestScoreLabel;
        [SerializeField] TMP_Text emptyStateLabel;

        [SerializeField] TMP_Text progressLabel;
        [SerializeField] Image promptPortrait;
        [SerializeField] TMP_Text promptText;
        [SerializeField] Transform answersContainer;
        [SerializeField] QuizAnswerButton answerButtonPrefab;

        [SerializeField] TMP_Text feedbackTitleLabel;
        [SerializeField] TMP_Text feedbackAnswerLabel;
        [SerializeField] Button nextButton;
        [SerializeField] Button viewProfileButton;

        [SerializeField] TMP_Text resultsTitleLabel;
        [SerializeField] TMP_Text resultsScoreLabel;
        [SerializeField] TMP_Text resultsBestLabel;
        [SerializeField] TMP_Text resultsNewRecordLabel;
        [SerializeField] Button playAgainButton;
        [SerializeField] Button resultsHomeButton;

        readonly List<QuizAnswerButton> _spawnedAnswers = new();

        QuizViewState _viewState = QuizViewState.Menu;
        QuizMode _selectedMode = QuizMode.Mixed;
        List<QuizQuestion> _questions;
        int _questionIndex;
        int _score;
        string _lastCorrectId;
        string _selectedAnswerId;
        bool _isNewRecord;
        bool _resultsRecorded;

        public bool IsInActiveRound =>
            _viewState is QuizViewState.Playing or QuizViewState.Feedback;

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            ThemeHelper.ThemeChanged += OnThemeChanged;
            FontSizeHelper.FontSizeChanged += OnFontSizeChanged;
            WireButtons();
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            ThemeHelper.ThemeChanged -= OnThemeChanged;
            FontSizeHelper.FontSizeChanged -= OnFontSizeChanged;
        }

        void WireButtons()
        {
            Wire(modePortraitButton, () => SelectMode(QuizMode.Portrait));
            Wire(modeFactButton, () => SelectMode(QuizMode.Fact));
            Wire(modeMixedButton, () => SelectMode(QuizMode.Mixed));
            Wire(startButton, StartRound);
            Wire(nextButton, OnNextClicked);
            Wire(viewProfileButton, OnViewProfileClicked);
            Wire(playAgainButton, StartRound);
            Wire(resultsHomeButton, () => navigation?.ShowHome());
        }

        static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        void OnLocaleChanged(UnityEngine.Localization.Locale _) => RefreshVisibleState();

        void OnThemeChanged() => RefreshModeButtons();

        void OnFontSizeChanged() => RefreshVisibleState();

        public void ShowMenu()
        {
            _viewState = QuizViewState.Menu;
            _questions = null;
            _questionIndex = 0;
            _score = 0;
            _isNewRecord = false;
            _resultsRecorded = false;
            SetViewActive(menuView);
            RefreshMenu();
        }

        public bool TryHandleBack()
        {
            switch (_viewState)
            {
                case QuizViewState.Playing:
                case QuizViewState.Feedback:
                    ShowMenu();
                    return true;
                case QuizViewState.Results:
                    ShowMenu();
                    return true;
                default:
                    return false;
            }
        }

        void SelectMode(QuizMode mode)
        {
            _selectedMode = mode;
            RefreshMenu();
        }

        void StartRound()
        {
            if (repository == null)
                return;

            _questions = QuizService.GenerateRound(repository.All, _selectedMode);
            if (_questions == null || _questions.Count == 0)
            {
                ShowMenu();
                return;
            }

            _questionIndex = 0;
            _score = 0;
            _isNewRecord = false;
            _resultsRecorded = false;
            ShowQuestion();
        }

        void ShowQuestion()
        {
            _viewState = QuizViewState.Playing;
            SetViewActive(playingView);
            if (feedbackView != null)
                feedbackView.SetActive(false);
            ClearAnswers();

            var question = _questions[_questionIndex];
            _lastCorrectId = question.CorrectId;

            if (progressLabel != null)
            {
                var fmt = L("quiz_progress");
                progressLabel.text = string.Format(fmt, _questionIndex + 1, _questions.Count);
            }

            var isPortrait = question.Kind == QuizPromptKind.Portrait;
            if (promptPortrait != null)
            {
                promptPortrait.gameObject.SetActive(isPortrait);
                promptPortrait.sprite = question.Portrait;
                promptPortrait.preserveAspect = true;
                promptPortrait.color = question.Portrait != null
                    ? Color.white
                    : UiTheme.PortraitPlaceholder;
            }

            if (promptText != null)
            {
                promptText.gameObject.SetActive(!isPortrait);
                promptText.text = isPortrait ? "" : question.PromptText ?? "";
            }

            SpawnAnswers(question);
        }

        void SpawnAnswers(QuizQuestion question)
        {
            if (answersContainer == null || answerButtonPrefab == null || repository == null)
                return;

            var english = LocaleHelper.IsEnglish;
            foreach (var optionId in question.OptionIds)
            {
                var data = repository.GetById(optionId);
                if (data == null)
                    continue;

                var button = Instantiate(answerButtonPrefab, answersContainer);
                button.Bind(data.id, data.GetFullName(english), OnAnswerSelected);
                _spawnedAnswers.Add(button);
            }
        }

        void OnAnswerSelected(string optionId)
        {
            if (_viewState != QuizViewState.Playing)
                return;

            _selectedAnswerId = optionId;
            var correct = optionId == _lastCorrectId;
            if (correct)
                _score++;

            foreach (var answer in _spawnedAnswers)
                answer.SetInteractable(false);

            HighlightAnswers(optionId);
            ShowFeedback(correct);
        }

        void HighlightAnswers(string selectedId)
        {
            foreach (var answer in _spawnedAnswers)
            {
                if (answer == null)
                    continue;

                var id = GetAnswerId(answer);
                if (id == _lastCorrectId)
                    answer.SetState(QuizAnswerButtonState.Correct);
                else if (id == selectedId)
                    answer.SetState(QuizAnswerButtonState.Wrong);
            }
        }

        static string GetAnswerId(QuizAnswerButton button) => button?.OptionId;

        void ShowFeedback(bool correct)
        {
            _viewState = QuizViewState.Feedback;
            if (feedbackView != null)
                feedbackView.SetActive(true);

            if (feedbackTitleLabel != null)
                feedbackTitleLabel.text = L(correct ? "quiz_correct" : "quiz_wrong");

            if (feedbackAnswerLabel != null && repository != null)
            {
                var data = repository.GetById(_lastCorrectId);
                var name = data?.GetFullName(LocaleHelper.IsEnglish) ?? _lastCorrectId;
                feedbackAnswerLabel.text = string.Format(L("quiz_correct_answer"), name);
            }
        }

        void OnNextClicked()
        {
            if (feedbackView != null)
                feedbackView.SetActive(false);

            _questionIndex++;
            if (_questionIndex >= _questions.Count)
                ShowResults();
            else
                ShowQuestion();
        }

        void OnViewProfileClicked()
        {
            if (navigation == null || string.IsNullOrEmpty(_lastCorrectId))
                return;

            navigation.ShowDetail(_lastCorrectId);
        }

        void ShowResults()
        {
            _viewState = QuizViewState.Results;
            SetViewActive(resultsView);

            if (!_resultsRecorded)
            {
                QuizStatsHelper.RecordGamePlayed();
                _isNewRecord = QuizStatsHelper.TryUpdateBestScore(_selectedMode, _score);
                _resultsRecorded = true;
            }

            if (resultsTitleLabel != null)
                resultsTitleLabel.text = L("quiz_results_title");

            if (resultsScoreLabel != null)
                resultsScoreLabel.text = string.Format(L("quiz_results_score"), _score, _questions.Count);

            if (resultsBestLabel != null)
                resultsBestLabel.text = string.Format(L("quiz_best_score"), QuizStatsHelper.GetBestScore(_selectedMode));

            if (resultsNewRecordLabel != null)
            {
                resultsNewRecordLabel.gameObject.SetActive(_isNewRecord);
                if (_isNewRecord)
                    resultsNewRecordLabel.text = L("quiz_new_record");
            }
        }

        void RefreshMenu()
        {
            RefreshModeButtons();
            RefreshBestScore();
            RefreshStartAvailability();
        }

        void RefreshModeButtons()
        {
            ApplyModeStyle(modePortraitButton, _selectedMode == QuizMode.Portrait);
            ApplyModeStyle(modeFactButton, _selectedMode == QuizMode.Fact);
            ApplyModeStyle(modeMixedButton, _selectedMode == QuizMode.Mixed);

            if (modePortraitButton != null)
                SetButtonLabel(modePortraitButton, L("quiz_mode_portrait"));
            if (modeFactButton != null)
                SetButtonLabel(modeFactButton, L("quiz_mode_fact"));
            if (modeMixedButton != null)
                SetButtonLabel(modeMixedButton, L("quiz_mode_mixed"));
            if (startButton != null)
                SetButtonLabel(startButton, L("quiz_start"));
            if (playAgainButton != null)
                SetButtonLabel(playAgainButton, L("quiz_play_again"));
            if (nextButton != null)
                SetButtonLabel(nextButton, L("quiz_next"));
            if (viewProfileButton != null)
                SetButtonLabel(viewProfileButton, L("quiz_view_profile"));
        }

        static void ApplyModeStyle(Button button, bool selected)
        {
            if (button == null)
                return;

            UiButtonStyler.Apply(button, selected ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
        }

        static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
                return;

            var tmp = button.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
                tmp.text = text;
        }

        void RefreshBestScore()
        {
            if (bestScoreLabel == null)
                return;

            bestScoreLabel.text = string.Format(L("quiz_best_score"), QuizStatsHelper.GetBestScore(_selectedMode));
        }

        void RefreshStartAvailability()
        {
            var pool = repository != null
                ? QuizService.GetEligiblePool(_selectedMode, repository.All)
                : null;
            var hasEnough = pool != null && pool.Count >= QuizService.OptionCount;

            if (startButton != null)
                startButton.interactable = hasEnough;

            if (emptyStateLabel != null)
            {
                emptyStateLabel.gameObject.SetActive(!hasEnough);
                if (!hasEnough)
                    emptyStateLabel.text = L("quiz_not_enough_data");
            }
        }

        void RefreshVisibleState()
        {
            switch (_viewState)
            {
                case QuizViewState.Menu:
                    RefreshMenu();
                    break;
                case QuizViewState.Playing:
                    ShowQuestion();
                    break;
                case QuizViewState.Feedback:
                    ShowFeedback(_selectedAnswerId == _lastCorrectId);
                    break;
                case QuizViewState.Results:
                    ShowResults();
                    break;
            }
        }

        void SetViewActive(GameObject activeView)
        {
            if (menuView != null)
                menuView.SetActive(activeView == menuView);
            if (playingView != null)
                playingView.SetActive(activeView == playingView);
            if (feedbackView != null)
                feedbackView.SetActive(activeView == feedbackView);
            if (resultsView != null)
                resultsView.SetActive(activeView == resultsView);
        }

        void ClearAnswers()
        {
            foreach (var answer in _spawnedAnswers)
            {
                if (answer != null)
                    Destroy(answer.gameObject);
            }

            _spawnedAnswers.Clear();
        }

        static string L(string key) =>
            LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
    }

}
