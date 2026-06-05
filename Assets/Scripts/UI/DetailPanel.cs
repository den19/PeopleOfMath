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

        int _sectionIndex;
        string _currentId;

        public bool CanGoNext =>
            sections != null && _sectionIndex < sections.Length - 1;

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
            _sectionIndex = 0;
            ShowSection(0);
            Refresh();
        }

        public bool TryGoBack()
        {
            if (_sectionIndex > 0)
            {
                ShowSection(_sectionIndex - 1);
                return true;
            }

            return false;
        }

        public void GoNext()
        {
            if (CanGoNext)
                ShowSection(_sectionIndex + 1);
        }

        void Refresh()
        {
            var data = repository?.GetById(_currentId);
            if (data == null)
                return;

            var english = LocaleHelper.IsEnglish;
            if (sections == null)
                return;

            foreach (var section in sections)
            {
                if (section != null)
                    section.Bind(data, english);
            }

            var current = sections != null && sections.Length > 0
                ? sections[Mathf.Clamp(_sectionIndex, 0, sections.Length - 1)]
                : null;
            if (current != null && headerTitle != null)
                headerTitle.SetDetailSectionTitle(current.GetSectionTitle(english));

            UpdateNavState();
        }

        void ShowSection(int index)
        {
            if (sections == null || sections.Length == 0)
                return;

            _sectionIndex = Mathf.Clamp(index, 0, sections.Length - 1);
            for (var i = 0; i < sections.Length; i++)
            {
                if (sections[i] != null)
                    sections[i].gameObject.SetActive(i == _sectionIndex);
            }

            var english = LocaleHelper.IsEnglish;
            var current = sections != null && sections.Length > 0
                ? sections[Mathf.Clamp(_sectionIndex, 0, sections.Length - 1)]
                : null;
            if (current != null && headerTitle != null)
                headerTitle.SetDetailSectionTitle(current.GetSectionTitle(english));

            UpdateNavState();
        }

        void UpdateNavState()
        {
            if (pageIndicator != null && sections != null && sections.Length > 0)
                pageIndicator.text = $"{_sectionIndex + 1} / {sections.Length}";

            if (sectionNextButton != null)
                sectionNextButton.gameObject.SetActive(CanGoNext);
        }
    }
}
