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
        [SerializeField] NavigationController navigation;
        [SerializeField] SearchBar searchBar;
        [SerializeField] Transform centuryContainer;
        [SerializeField] Transform countryContainer;
        [SerializeField] Transform branchContainer;
        [SerializeField] Button filterButtonPrefab;

        readonly List<Button> _spawned = new();

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            Rebuild();
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        void OnLocaleChanged(UnityEngine.Localization.Locale _) => Rebuild();

        void Rebuild()
        {
            ClearSpawned();
            SpawnGroup(centuryContainer, FilterKind.Century, Taxonomy.AllCenturyKeys, Taxonomy.Centuries);
            SpawnGroup(countryContainer, FilterKind.Country, Taxonomy.AllCountryKeys, Taxonomy.Countries);
            SpawnGroup(
                branchContainer,
                FilterKind.Branch,
                Taxonomy.AllBranchKeys,
                Taxonomy.Branches);
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
            if (parent == null || filterButtonPrefab == null)
                return;

            var english = LocaleHelper.IsEnglish;
            foreach (var key in keys)
            {
                if (!labels.ContainsKey(key))
                    continue;

                var btn = Instantiate(filterButtonPrefab, parent);
                var text = btn.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    text.text = labels[key].Get(english);
                    text.ForceMeshUpdate();
                }

                var capturedKey = key;
                btn.onClick.AddListener(() => navigation.ShowList(kind, capturedKey));
                _spawned.Add(btn);
            }
        }
    }
}
