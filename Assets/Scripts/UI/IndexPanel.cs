using System.Collections.Generic;
using PeopleOfMath.Core;
using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class IndexPanel : MonoBehaviour
    {
        const string ListItemResourceName = "MathematicianListItem";

        [SerializeField] NavigationController navigation;
        [SerializeField] MathematicianRepository repository;
        [SerializeField] Transform letterContainer;
        [SerializeField] Transform listContent;
        [SerializeField] Button letterButtonPrefab;
        [SerializeField] MathematicianListItem itemPrefab;
        [SerializeField] GameObject emptyState;

        readonly List<(Button button, char letter)> _letterButtons = new();
        char? _selectedLetter;

        void Awake()
        {
            if (itemPrefab == null)
                itemPrefab = Resources.Load<MathematicianListItem>(ListItemResourceName);
        }

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            FontSizeHelper.FontSizeChanged += OnFontSizeChanged;
            ThemeHelper.ThemeChanged += OnThemeChanged;
            Rebuild();
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            FontSizeHelper.FontSizeChanged -= OnFontSizeChanged;
            ThemeHelper.ThemeChanged -= OnThemeChanged;
        }

        void OnLocaleChanged(UnityEngine.Localization.Locale _) => Rebuild();

        void OnFontSizeChanged() => RefreshList();

        void OnThemeChanged() => RefreshTheme();

        void RefreshTheme()
        {
            RefreshLetterStyles();
            if (listContent == null)
                return;

            foreach (Transform child in listContent)
                child.GetComponent<UiThemedCard>()?.Apply();
        }

        void Rebuild()
        {
            if (repository == null || letterContainer == null)
                return;

            var english = LocaleHelper.IsEnglish;
            var usedLetters = IndexService.GetUsedLetters(repository.All, english);
            EnsureSelectedLetter(usedLetters, english);
            RebuildLetterButtons(english, usedLetters);
            EnsureLetterStripOnTop();
            RefreshList();
        }

        void EnsureLetterStripOnTop()
        {
            var letterScroll = transform.Find("LetterScroll");
            letterScroll?.SetAsLastSibling();
        }

        void EnsureSelectedLetter(HashSet<char> usedLetters, bool english)
        {
            if (_selectedLetter.HasValue)
            {
                var letter = _selectedLetter.Value;
                if (IndexService.IsStripLetter(letter, english) && usedLetters.Contains(letter))
                    return;
            }

            _selectedLetter = FindFirstUsedLetter(english, usedLetters);
        }

        static char? FindFirstUsedLetter(bool english, HashSet<char> usedLetters)
        {
            foreach (var letter in IndexService.GetAlphabet(english))
            {
                if (usedLetters.Contains(letter))
                    return letter;
            }

            return usedLetters.Contains(IndexService.OtherLetter) ? IndexService.OtherLetter : null;
        }

        void RebuildLetterButtons(bool english, HashSet<char> usedLetters)
        {
            ClearLetterButtons();

            if (letterButtonPrefab == null)
                return;

            foreach (var letter in IndexService.GetAlphabet(english))
                SpawnLetterButton(letter, usedLetters.Contains(letter));

            if (usedLetters.Contains(IndexService.OtherLetter))
                SpawnLetterButton(IndexService.OtherLetter, true);

            if (letterContainer is RectTransform letterRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(letterRt);

            var letterScroll = letterContainer.GetComponentInParent<ScrollRect>();
            if (letterScroll != null)
                letterScroll.horizontalNormalizedPosition = 0f;

            RefreshLetterStyles();
        }

        void SpawnLetterButton(char letter, bool hasEntries)
        {
            var btn = Instantiate(letterButtonPrefab, letterContainer);
            var text = btn.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = letter == IndexService.OtherLetter ? "#" : letter.ToString();
                text.ForceMeshUpdate();
            }

            btn.interactable = hasEntries;
            var captured = letter;
            btn.onClick.AddListener(() => SelectLetter(captured));
            _letterButtons.Add((btn, letter));
        }

        void ClearLetterButtons()
        {
            foreach (var (button, _) in _letterButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }

            _letterButtons.Clear();
        }

        void SelectLetter(char letter)
        {
            _selectedLetter = letter;
            RefreshLetterStyles();
            RefreshList();
        }

        void RefreshLetterStyles()
        {
            foreach (var (button, letter) in _letterButtons)
                ApplyLetterStyle(button, letter);
        }

        void ApplyLetterStyle(Button button, char letter)
        {
            if (button == null || !button.interactable)
            {
                UiButtonStyler.Apply(button, UiButtonStyle.Secondary);
                return;
            }

            var selected = _selectedLetter.HasValue && _selectedLetter.Value == letter;
            UiButtonStyler.Apply(button, selected ? UiButtonStyle.Primary : UiButtonStyle.Secondary);
        }

        void RefreshList()
        {
            foreach (Transform child in listContent)
                Destroy(child.gameObject);

            if (repository == null || !_selectedLetter.HasValue)
            {
                emptyState?.SetActive(true);
                UpdateEmptyStateMessage();
                return;
            }

            var english = LocaleHelper.IsEnglish;
            var results = IndexService.FilterByLetter(repository.All, _selectedLetter.Value, english);
            emptyState?.SetActive(results.Count == 0);
            UpdateEmptyStateMessage();

            if (itemPrefab == null)
            {
                Debug.LogError(
                    "IndexPanel: MathematicianListItem prefab is not assigned. " +
                    "Run PeopleOfMath → Regenerate Main Scene or add Assets/Resources/MathematicianListItem.prefab.");
                return;
            }

            foreach (var data in results)
            {
                var item = Instantiate(itemPrefab, listContent);
                item.Bind(data, id => navigation.ShowDetail(id));
            }

            listContent.GetComponentInParent<FontSizeScope>()?.Apply();
        }

        void UpdateEmptyStateMessage()
        {
            if (emptyState == null)
                return;

            var text = emptyState.GetComponent<TMP_Text>();
            if (text == null)
                return;

            text.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "empty_index");
        }
    }
}
