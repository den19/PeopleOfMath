using PeopleOfMath.Core;
using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace PeopleOfMath.UI
{
    public class ListPanel : MonoBehaviour
    {
        const string ListItemResourceName = "MathematicianListItem";

        [SerializeField] NavigationController navigation;
        [SerializeField] MathematicianRepository repository;
        [SerializeField] Transform listContent;
        [SerializeField] MathematicianListItem itemPrefab;
        [SerializeField] GameObject emptyState;

        FilterKind _kind;
        string _key;

        void Awake()
        {
            if (itemPrefab == null)
                itemPrefab = Resources.Load<MathematicianListItem>(ListItemResourceName);
        }

        public void BindFilter(FilterKind kind, string key)
        {
            _kind = kind;
            _key = key;
            Refresh();
        }

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            FontSizeHelper.FontSizeChanged += OnFontSizeChanged;
            Refresh();
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            FontSizeHelper.FontSizeChanged -= OnFontSizeChanged;
        }

        void OnLocaleChanged(UnityEngine.Localization.Locale _) => Refresh();

        void OnFontSizeChanged() => Refresh();

        void Refresh()
        {
            foreach (Transform child in listContent)
                Destroy(child.gameObject);

            if (repository == null || string.IsNullOrEmpty(_key))
                return;

            var english = LocaleHelper.IsEnglish;
            var results = FilterService.Filter(repository.All, _kind, _key, english);
            emptyState?.SetActive(results.Count == 0);

            if (itemPrefab == null)
            {
                Debug.LogError(
                    "ListPanel: MathematicianListItem prefab is not assigned. " +
                    "Run PeopleOfMath → Regenerate Main Scene or add Assets/Resources/MathematicianListItem.prefab.");
                return;
            }

            foreach (var data in results)
            {
                var item = Instantiate(itemPrefab, listContent);
                item.Bind(data, id => navigation.ShowDetail(id));
            }

            GetComponent<FontSizeScope>()?.Apply();
        }
    }
}
