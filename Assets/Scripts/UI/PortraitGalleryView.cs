using System;
using System.Collections;
using System.Collections.Generic;
using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class PortraitGalleryView : MonoBehaviour
    {
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] RectTransform pageContainer;
        [SerializeField] Image pageTemplate;
        [SerializeField] TMP_Text captionText;
        [SerializeField] Transform dotsRoot;
        [SerializeField] Image dotTemplate;
        [SerializeField] GalleryScrollSnap snap;
        [SerializeField] Sprite placeholderSprite;

        readonly List<Image> _pages = new();
        readonly List<Image> _dots = new();
        IReadOnlyList<PortraitEntry> _entries = new List<PortraitEntry>();
        string _mathematicianId;
        int _layoutPageCount;
        bool _relayoutScheduled;

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            ScheduleRelayout();
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            if (snap != null)
                snap.PageChanged -= OnPageChanged;
        }

        void OnRectTransformDimensionsChange() => ScheduleRelayout();

        void OnLocaleChanged(UnityEngine.Localization.Locale _) => RefreshCaption();

        public void Bind(MathematicianData data)
        {
            if (data == null)
            {
                BindEntries(null, null);
                return;
            }

            BindEntries(data.id, data.portraits);
        }

        public void Bind(IReadOnlyList<PortraitEntry> portraits) => BindEntries(null, portraits);

        void BindEntries(string mathematicianId, IReadOnlyList<PortraitEntry> portraits)
        {
            _mathematicianId = mathematicianId;
            _entries = portraits ?? new List<PortraitEntry>();
            var valid = PortraitResolver.ResolveGalleryPortraits(mathematicianId, _entries);
            if (valid != _entries)
                _entries = valid;

            BuildPages(valid);
            snap?.Configure(valid.Count);
            BuildDots(valid.Count);
            if (valid.Count > 0)
            {
                snap.PageChanged -= OnPageChanged;
                snap.PageChanged += OnPageChanged;
                OnPageChanged(0);
            }
            else
                SetCaption(LocaleHelper.IsEnglish
                    ? "No images available"
                    : "Изображения недоступны");

            ScheduleRelayout();
        }

        void BuildPages(List<PortraitEntry> valid)
        {
            foreach (var p in _pages)
            {
                if (p != null)
                    Destroy(p.gameObject);
            }
            _pages.Clear();

            if (pageTemplate != null)
                pageTemplate.gameObject.SetActive(false);

            _layoutPageCount = Mathf.Max(1, valid.Count);

            if (valid.Count == 0)
            {
                var empty = CreatePage();
                empty.sprite = placeholderSprite;
                empty.color = UiTheme.PortraitPlaceholder;
                _pages.Add(empty);
            }
            else
            {
                foreach (var entry in valid)
                {
                    var img = CreatePage();
                    img.sprite = entry.sprite;
                    img.preserveAspect = !ShouldStretchPortraitToWidth();
                    img.color = Color.white;
                    _pages.Add(img);
                }
            }

            LayoutPages(_layoutPageCount);
            if (pageContainer != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(pageContainer);
            if (scrollRect != null)
                Canvas.ForceUpdateCanvases();
        }

        void LayoutPages(int count)
        {
            if (pageContainer == null || scrollRect == null)
                return;

            var width = scrollRect.viewport != null
                ? scrollRect.viewport.rect.width
                : ((RectTransform)scrollRect.transform).rect.width;
            if (width < 1f)
                width = 400f;

            pageContainer.sizeDelta = new Vector2(width * Mathf.Max(1, count), 0f);
            for (var i = 0; i < _pages.Count; i++)
            {
                var rt = _pages[i].rectTransform;
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 0.5f);
                rt.sizeDelta = new Vector2(width, 0f);
                rt.anchoredPosition = new Vector2(i * width, 0);
                _pages[i].type = Image.Type.Simple;
            }
        }

        void ScheduleRelayout()
        {
            if (_pages.Count == 0 || _relayoutScheduled || !isActiveAndEnabled)
                return;

            _relayoutScheduled = true;
            StartCoroutine(RelayoutNextFrame());
        }

        IEnumerator RelayoutNextFrame()
        {
            yield return null;
            _relayoutScheduled = false;

            if (!isActiveAndEnabled || _pages.Count == 0)
                yield break;

            Canvas.ForceUpdateCanvases();
            LayoutPages(_layoutPageCount);
            if (pageContainer != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(pageContainer);
        }

        Image CreatePage()
        {
            Image img;
            if (pageTemplate != null)
            {
                img = Instantiate(pageTemplate, pageContainer);
                img.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("Page", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(pageContainer, false);
                img = go.GetComponent<Image>();
            }
            return img;
        }

        void BuildDots(int count)
        {
            foreach (var d in _dots)
            {
                if (d != null)
                    Destroy(d.gameObject);
            }
            _dots.Clear();

            if (dotTemplate != null)
                dotTemplate.gameObject.SetActive(false);

            var max = Mathf.Min(4, count);
            for (var i = 0; i < max; i++)
            {
                Image dot;
                if (dotTemplate != null)
                {
                    dot = Instantiate(dotTemplate, dotsRoot);
                    dot.gameObject.SetActive(true);
                }
                else
                    continue;
                _dots.Add(dot);
                SetDotActive(i, i == 0);
            }
        }

        void OnPageChanged(int index)
        {
            for (var i = 0; i < _dots.Count; i++)
                SetDotActive(i, i == index);
            RefreshCaption(index);
        }

        void SetDotActive(int index, bool active)
        {
            if (index < 0 || index >= _dots.Count)
                return;
            _dots[index].color = active
                ? UiTheme.GalleryDotActive
                : UiTheme.GalleryDotInactive;
        }

        public void RefreshTheme()
        {
            for (var i = 0; i < _dots.Count; i++)
                SetDotActive(i, snap != null && i == snap.CurrentIndex);

            foreach (var page in _pages)
            {
                if (page == null)
                    continue;
                if (page.sprite == null || page.sprite == placeholderSprite)
                    page.color = UiTheme.PortraitPlaceholder;
            }

            if (captionText != null)
                captionText.color = UiTheme.TextSecondary;
        }

        void RefreshCaption() => RefreshCaption(snap != null ? snap.CurrentIndex : 0);

        void RefreshCaption(int index)
        {
            if (_entries == null || index < 0)
                return;

            var valid = PortraitResolver.CollectValidPortraits(_entries);

            if (index >= valid.Count)
            {
                SetCaption("");
                return;
            }

            var entry = valid[index];
            var english = LocaleHelper.IsEnglish;
            var license = entry.licenseShort ?? "";
            var attr = english
                ? (string.IsNullOrWhiteSpace(entry.attributionEn) ? entry.attributionRu : entry.attributionEn)
                : entry.attributionRu;
            var sourceLabel = english ? "Source" : "Источник";
            var licenseLabel = english ? "License" : "Лицензия";

            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(license))
                lines.Add($"{licenseLabel}: {license}");
            if (!string.IsNullOrWhiteSpace(attr))
                lines.Add(attr);
            if (!string.IsNullOrWhiteSpace(entry.sourceUrl))
                lines.Add($"{sourceLabel}: {entry.sourceUrl}");

            SetCaption(lines.Count > 0 ? string.Join("\n", lines) : "");
        }

        void SetCaption(string text)
        {
            if (captionText != null)
                captionText.text = text;
        }

        static bool ShouldStretchPortraitToWidth(string mathematicianId) =>
            string.Equals(mathematicianId, "selberg", StringComparison.OrdinalIgnoreCase);

        bool ShouldStretchPortraitToWidth() => ShouldStretchPortraitToWidth(_mathematicianId);
    }
}
