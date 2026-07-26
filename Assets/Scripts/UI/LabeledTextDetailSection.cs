using System.Collections.Generic;
using PeopleOfMath.Core;
using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public enum LabeledDetailSectionKind
    {
        Countries,
        Centuries,
        Fields
    }

    public class LabeledTextDetailSection : MathematicianDetailSection
    {
        [SerializeField] LabeledDetailSectionKind sectionKind;
        [SerializeField] NavigationController navigation;
        [SerializeField] TMP_Text labelText;
        [SerializeField] Transform tagContainer;
        [SerializeField] Button tagButtonPrefab;

        readonly List<Button> _spawnedTags = new();

        public FilterKind FilterKind => GetFilterKind();

        void OnEnable()
        {
            ThemeHelper.ThemeChanged += ApplySectionLabelColor;
            ApplySectionLabelColor();
        }

        void OnDisable()
        {
            ThemeHelper.ThemeChanged -= ApplySectionLabelColor;
        }

        void ApplySectionLabelColor()
        {
            if (labelText != null)
                labelText.color = UiTheme.GetFilterAccent(GetFilterKind());
        }

        public override void Bind(MathematicianData data, bool english)
        {
            if (data == null)
                return;

            if (labelText != null)
            {
                labelText.text = GetSectionTitle(english);
                labelText.color = UiTheme.GetFilterAccent(GetFilterKind());
            }

            ClearSpawnedTags();
            if (tagContainer == null || tagButtonPrefab == null)
                return;

            var nav = ResolveNavigation();
            if (nav == null)
                return;

            var filterKind = GetFilterKind();
            var keys = GetKeys(data);
            var labels = GetLabelMap();
            foreach (var key in keys)
            {
                if (!labels.ContainsKey(key))
                    continue;

                var btn = Instantiate(tagButtonPrefab, tagContainer);
                var text = btn.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    text.text = labels[key].Get(english);
                    text.ForceMeshUpdate();
                }

                var themedCard = btn.GetComponent<UiThemedCard>();
                if (themedCard != null)
                    themedCard.Configure(UiCardVariant.Filter, filterKind);

                var capturedKey = key;
                btn.onClick.AddListener(() => nav.ShowListFromDetail(filterKind, capturedKey, data.id));
                _spawnedTags.Add(btn);
            }
        }

        public override string GetSectionTitle(bool english) => sectionKind switch
        {
            LabeledDetailSectionKind.Countries => UiStrings.Get("section_countries"),
            LabeledDetailSectionKind.Centuries => UiStrings.Get("section_centuries"),
            LabeledDetailSectionKind.Fields => UiStrings.Get("section_fields"),
            _ => ""
        };

        public override bool HasContent(MathematicianData data, bool english)
        {
            if (data == null)
                return false;

            var labels = GetLabelMap();
            foreach (var key in GetKeys(data))
            {
                if (labels.ContainsKey(key))
                    return true;
            }

            return false;
        }

        void ClearSpawnedTags()
        {
            foreach (var btn in _spawnedTags)
            {
                if (btn != null)
                    Destroy(btn.gameObject);
            }

            _spawnedTags.Clear();
        }

        NavigationController ResolveNavigation()
        {
            if (navigation != null)
                return navigation;

            navigation = FindFirstObjectByType<NavigationController>();
            return navigation;
        }

        FilterKind GetFilterKind() => sectionKind switch
        {
            LabeledDetailSectionKind.Countries => FilterKind.Country,
            LabeledDetailSectionKind.Centuries => FilterKind.Century,
            LabeledDetailSectionKind.Fields => FilterKind.Branch,
            _ => FilterKind.Country
        };

        IEnumerable<string> GetKeys(MathematicianData data) => sectionKind switch
        {
            LabeledDetailSectionKind.Countries => data.countryKeys,
            LabeledDetailSectionKind.Centuries => data.centuryKeys,
            LabeledDetailSectionKind.Fields => data.branchKeys,
            _ => data.countryKeys
        };

        Dictionary<string, Taxonomy.LabelPair> GetLabelMap() => sectionKind switch
        {
            LabeledDetailSectionKind.Countries => Taxonomy.Countries,
            LabeledDetailSectionKind.Centuries => Taxonomy.Centuries,
            LabeledDetailSectionKind.Fields => Taxonomy.Branches,
            _ => Taxonomy.Countries
        };
    }
}
