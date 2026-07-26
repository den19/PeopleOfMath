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
    public class HomePanel : MonoBehaviour
    {
        const string CategoryTileResourceName = "CategoryTile";

        [SerializeField] NavigationController navigation;
        [SerializeField] MathematicianRepository repository;
        [SerializeField] SearchBar searchBar;
        [SerializeField] Transform centuryContainer;
        [SerializeField] Transform countryContainer;
        [SerializeField] Transform branchContainer;
        [SerializeField] Button categoryTilePrefab;
        [SerializeField] Button filterButtonPrefab;
        [SerializeField] Button quizButton;

        readonly List<Button> _spawned = new();
        bool _needsRebuild = true;
        string _boundLocaleCode;

        Button TilePrefab => categoryTilePrefab != null ? categoryTilePrefab : filterButtonPrefab;

        void Awake()
        {
            if (categoryTilePrefab == null)
                categoryTilePrefab = Resources.Load<Button>(CategoryTileResourceName);

            if (quizButton != null)
            {
                quizButton.onClick.RemoveAllListeners();
                quizButton.onClick.AddListener(() => navigation?.ShowQuiz());
            }

            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        void OnEnable()
        {
            ThemeHelper.ThemeChanged += OnThemeChanged;
            if (_needsRebuild || _spawned.Count == 0 || LocaleCodeChanged())
                Rebuild();
        }

        void OnDisable()
        {
            ThemeHelper.ThemeChanged -= OnThemeChanged;
        }

        void OnLocaleChanged(UnityEngine.Localization.Locale _)
        {
            _needsRebuild = true;
            if (isActiveAndEnabled)
                Rebuild();
        }

        bool LocaleCodeChanged()
        {
            var code = LocalizationSettings.SelectedLocale?.Identifier.Code ?? "";
            return code != _boundLocaleCode;
        }

        void OnThemeChanged() => ApplyFilterStyles();

        void Rebuild()
        {
            ClearSpawned();
            _boundLocaleCode = LocalizationSettings.SelectedLocale?.Identifier.Code ?? "";
            SpawnGroup(centuryContainer, FilterKind.Century, Taxonomy.AllCenturyKeys, Taxonomy.Centuries);
            SpawnGroup(countryContainer, FilterKind.Country, Taxonomy.AllCountryKeys, Taxonomy.Countries);
            SpawnGroup(
                branchContainer,
                FilterKind.Branch,
                Taxonomy.AllBranchKeys,
                Taxonomy.Branches);
            ApplyFilterStyles();
            StyleQuizButton();
            _needsRebuild = false;
        }

        void ClearSpawned()
        {
            foreach (var b in _spawned)
            {
                if (b != null)
                    Destroy(b.gameObject);
            }
            _spawned.Clear();
        }

        void SpawnGroup(
            Transform parent,
            FilterKind kind,
            IReadOnlyList<string> keys,
            Dictionary<string, Taxonomy.LabelPair> labels)
        {
            var prefab = TilePrefab;
            if (parent == null || prefab == null)
                return;

            var english = LocaleHelper.IsEnglish;
            var source = repository != null ? repository.All : null;
            var counts = FilterService.CountAll(source, kind);

            foreach (var key in keys)
            {
                if (!labels.ContainsKey(key))
                    continue;

                counts.TryGetValue(key, out var count);
                if (source != null && count == 0)
                    continue;

                var label = labels[key].Get(english);
                var btn = Instantiate(prefab, parent);
                BindTile(btn, kind, key, label, count);

                var themedCard = btn.GetComponent<UiThemedCard>();
                if (themedCard != null)
                    themedCard.Configure(UiCardVariant.Filter, kind);

                var capturedKey = key;
                btn.onClick.AddListener(() => navigation.ShowList(kind, capturedKey));
                _spawned.Add(btn);
            }

            EnsureAdaptiveGrid(parent);

            if (parent is RectTransform parentRt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
                parent.GetComponent<AdaptiveBrowseGrid>()?.Apply();
            }
        }

        static void EnsureAdaptiveGrid(Transform parent)
        {
            if (parent == null || parent.GetComponent<GridLayoutGroup>() == null)
                return;

            if (parent.GetComponent<AdaptiveBrowseGrid>() == null)
                parent.gameObject.AddComponent<AdaptiveBrowseGrid>();
        }

        static void BindTile(Button btn, FilterKind kind, string key, string label, int count)
        {
            var title = btn.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (title != null)
            {
                title.text = label;
                title.ForceMeshUpdate();
            }

            var countText = btn.transform.Find("Count")?.GetComponent<TMP_Text>();
            if (countText != null)
            {
                countText.text = $"({count})";
                countText.ForceMeshUpdate();
            }

            var glyph = btn.transform.Find("Media/Glyph")?.GetComponent<TMP_Text>();
            if (glyph != null)
            {
                glyph.text = ResolveGlyph(kind, key, label);
                glyph.ForceMeshUpdate();
            }
        }

        static string ResolveGlyph(FilterKind kind, string key, string label)
        {
            if (kind == FilterKind.Century && TryCenturyRoman(key, out var roman))
                return roman;

            if (!string.IsNullOrWhiteSpace(label))
            {
                foreach (var ch in label)
                {
                    if (char.IsLetterOrDigit(ch))
                        return char.ToUpperInvariant(ch).ToString();
                }
            }

            return kind switch
            {
                FilterKind.Century => "∞",
                FilterKind.Country => "◎",
                FilterKind.Branch => "∑",
                _ => "·"
            };
        }

        static bool TryCenturyRoman(string key, out string roman)
        {
            roman = null;
            if (string.IsNullOrEmpty(key))
                return false;

            var normalized = key.Trim().ToLowerInvariant();
            var numberPart = normalized.EndsWith("bc") ? normalized[..^2] : normalized;
            if (!int.TryParse(numberPart, out var century) || century <= 0 || century > 3999)
                return false;

            roman = ToRoman(century);
            return !string.IsNullOrEmpty(roman);
        }

        static string ToRoman(int value)
        {
            if (value <= 0)
                return null;

            // Enough for century keys (1–21, BC variants).
            (int arabic, string glyph)[] map =
            {
                (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
                (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
                (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
            };

            var result = new System.Text.StringBuilder();
            var remaining = value;
            foreach (var (arabic, glyph) in map)
            {
                while (remaining >= arabic)
                {
                    result.Append(glyph);
                    remaining -= arabic;
                }
            }

            return result.ToString();
        }

        void ApplyFilterStyles()
        {
            foreach (var button in _spawned)
            {
                if (button == null)
                    continue;

                button.GetComponent<UiThemedCard>()?.Apply();
            }

            StyleQuizButton();
        }

        void StyleQuizButton()
        {
            if (quizButton == null)
                return;

            UiButtonStyler.Apply(quizButton, UiButtonStyle.Primary);
            var indicator = quizButton.transform.Find("TabIndicator")?.GetComponent<Image>();
            if (indicator != null && ThemeHelper.IsGlassmorphism)
                indicator.color = UiTheme.AccentWarm;
        }
    }
}
