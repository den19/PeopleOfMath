using System.Collections.Generic;
using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class DetailPanel : MonoBehaviour
    {
        [SerializeField] MathematicianRepository repository;
        [SerializeField] MathematicianDetailSection[] sections;
        [SerializeField] HeaderTitleBinder headerTitle;
        [SerializeField] Button sectionNextButton;
        [SerializeField] TMP_Text pageIndicator;

        readonly List<int> _visibleIndices = new();
        int _visibleSectionIndex;
        string _currentId;

        public bool CanGoNext =>
            _visibleIndices.Count > 0 && _visibleSectionIndex < _visibleIndices.Count - 1;

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        void OnLocaleChanged(UnityEngine.Localization.Locale _) => Refresh();

        public void Bind(string mathematicianId)
        {
            _currentId = mathematicianId;
            _visibleSectionIndex = 0;
            Refresh();
            ShowVisibleSection(_visibleSectionIndex);
        }

        public bool TryGoBack()
        {
            if (_visibleSectionIndex > 0)
            {
                ShowVisibleSection(_visibleSectionIndex - 1);
                return true;
            }

            return false;
        }

        public void GoNext()
        {
            if (CanGoNext)
                ShowVisibleSection(_visibleSectionIndex + 1);
        }

        void Refresh()
        {
            var data = repository?.GetById(_currentId);
            if (data == null)
                return;

            var english = LocaleHelper.IsEnglish;
            if (sections == null)
                return;

            RebuildVisibleIndices(data, english);

            foreach (var section in sections)
            {
                if (section != null)
                    section.Bind(data, english);
            }

            if (_visibleIndices.Count == 0)
            {
                HideAllSections();
                UpdateNavState();
                return;
            }

            _visibleSectionIndex = Mathf.Clamp(_visibleSectionIndex, 0, _visibleIndices.Count - 1);
            ShowVisibleSection(_visibleSectionIndex);
        }

        void RebuildVisibleIndices(MathematicianData data, bool english)
        {
            _visibleIndices.Clear();
            if (sections == null)
                return;

            for (var i = 0; i < sections.Length; i++)
            {
                var section = sections[i];
                if (section != null && section.HasContent(data, english))
                    _visibleIndices.Add(i);
            }
        }

        void ShowVisibleSection(int visibleIndex)
        {
            if (sections == null || sections.Length == 0 || _visibleIndices.Count == 0)
            {
                HideAllSections();
                UpdateNavState();
                return;
            }

            _visibleSectionIndex = Mathf.Clamp(visibleIndex, 0, _visibleIndices.Count - 1);
            var sectionIndex = _visibleIndices[_visibleSectionIndex];

            for (var i = 0; i < sections.Length; i++)
            {
                if (sections[i] != null)
                    sections[i].gameObject.SetActive(i == sectionIndex);
            }

            var english = LocaleHelper.IsEnglish;
            var current = sections[sectionIndex];
            if (current != null && headerTitle != null)
                headerTitle.SetDetailSectionTitle(current.GetSectionTitle(english));

            UpdateNavState();
        }

        void HideAllSections()
        {
            if (sections == null)
                return;

            foreach (var section in sections)
            {
                if (section != null)
                    section.gameObject.SetActive(false);
            }
        }

        void UpdateNavState()
        {
            if (pageIndicator != null)
            {
                pageIndicator.text = _visibleIndices.Count > 0
                    ? $"{_visibleSectionIndex + 1} / {_visibleIndices.Count}"
                    : "";
            }

            if (sectionNextButton != null)
                sectionNextButton.gameObject.SetActive(CanGoNext);
        }
    }
}
