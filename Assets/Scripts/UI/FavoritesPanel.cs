using System.Collections;
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
    public class FavoritesPanel : MonoBehaviour
    {
        const string ListItemResourceName = "MathematicianListItem";

        [SerializeField] NavigationController navigation;
        [SerializeField] MathematicianRepository repository;
        [SerializeField] Transform listContent;
        [SerializeField] MathematicianListItem itemPrefab;
        [SerializeField] GameObject emptyState;

        bool _animateOnNextRefresh;
        Coroutine _revealRoutine;
        ScrollRect _listScroll;

        void Awake()
        {
            if (itemPrefab == null)
                itemPrefab = Resources.Load<MathematicianListItem>(ListItemResourceName);

            if (listContent != null)
                _listScroll = listContent.GetComponentInParent<ScrollRect>();
        }

        void OnEnable()
        {
            FavoritesHelper.FavoritesChanged += OnFavoritesChanged;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            FontSizeHelper.FontSizeChanged += OnFontSizeChanged;
            ThemeHelper.ThemeChanged += OnThemeChanged;
            Refresh();
        }

        void OnDisable()
        {
            FavoritesHelper.FavoritesChanged -= OnFavoritesChanged;
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            FontSizeHelper.FontSizeChanged -= OnFontSizeChanged;
            ThemeHelper.ThemeChanged -= OnThemeChanged;

            if (_revealRoutine != null)
            {
                StopCoroutine(_revealRoutine);
                _revealRoutine = null;
            }
        }

        public void PrepareAnimatedOpen() => _animateOnNextRefresh = true;

        public void RevealListItemsStaggered()
        {
            if (_revealRoutine != null)
            {
                StopCoroutine(_revealRoutine);
                _revealRoutine = null;
            }

            _revealRoutine = StartCoroutine(RevealListItemsStaggeredRoutine());
        }

        IEnumerator RevealListItemsStaggeredRoutine()
        {
            ResetListScrollToTop();
            Canvas.ForceUpdateCanvases();
            yield return null;

            ResetListScrollToTop();
            var reveal = UiListItemReveal.RevealStaggered(this, CollectRevealTargets());
            if (reveal != null)
                yield return reveal;
            _revealRoutine = null;
        }

        void ResetListScrollToTop()
        {
            if (_listScroll == null && listContent != null)
                _listScroll = listContent.GetComponentInParent<ScrollRect>();

            if (_listScroll == null)
                return;

            _listScroll.StopMovement();
            _listScroll.verticalNormalizedPosition = 1f;
        }

        void OnFavoritesChanged() => Refresh();

        void OnLocaleChanged(UnityEngine.Localization.Locale _) => Refresh();

        void OnFontSizeChanged() => Refresh();

        void OnThemeChanged() => RefreshTheme();

        void RefreshTheme()
        {
            if (listContent == null)
                return;

            foreach (Transform child in listContent)
                child.GetComponent<UiThemedCard>()?.Apply();
        }

        void Refresh()
        {
            var animateItems = _animateOnNextRefresh;
            _animateOnNextRefresh = false;

            foreach (Transform child in listContent)
                Destroy(child.gameObject);

            if (repository == null)
                return;

            var favorites = new List<MathematicianData>();
            foreach (var id in FavoritesHelper.GetOrderedIds())
            {
                var data = repository.GetById(id);
                if (data != null)
                    favorites.Add(data);
            }

            emptyState?.SetActive(favorites.Count == 0);
            UpdateEmptyStateMessage();

            if (itemPrefab == null)
            {
                Debug.LogError(
                    "FavoritesPanel: MathematicianListItem prefab is not assigned. " +
                    "Run PeopleOfMath → Regenerate Main Scene or add Assets/Resources/MathematicianListItem.prefab.");
                return;
            }

            foreach (var data in favorites)
            {
                var item = Instantiate(itemPrefab, listContent);
                item.Bind(data, id => navigation.ShowDetail(id));

                if (animateItems)
                    UiListItemReveal.HideImmediate(item.transform);
            }

            if (animateItems && emptyState != null && emptyState.activeSelf)
                UiListItemReveal.HideImmediate(emptyState.transform);

            if (animateItems)
                ResetListScrollToTop();

            GetComponent<FontSizeScope>()?.Apply();
        }

        IEnumerable<Transform> CollectRevealTargets()
        {
            if (listContent != null)
            {
                foreach (Transform child in listContent)
                    yield return child;
            }

            if (emptyState != null && emptyState.activeSelf)
                yield return emptyState.transform;
        }

        void UpdateEmptyStateMessage()
        {
            if (emptyState == null)
                return;

            var text = emptyState.GetComponent<TMP_Text>();
            if (text == null)
                return;

            text.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "empty_favorites");
        }
    }
}
