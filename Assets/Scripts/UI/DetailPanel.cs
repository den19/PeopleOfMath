using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace PeopleOfMath.UI
{
    public class DetailPanel : MonoBehaviour
    {
        [SerializeField] MathematicianRepository repository;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text datesText;
        [SerializeField] TMP_Text countriesText;
        [SerializeField] TMP_Text centuriesText;
        [SerializeField] TMP_Text branchesText;
        [SerializeField] TMP_Text achievementsText;
        [SerializeField] TMP_Text personalLifeText;
        [SerializeField] TMP_Text achievementsLabel;
        [SerializeField] TMP_Text personalLifeLabel;
        [SerializeField] TMP_Text countriesLabel;
        [SerializeField] TMP_Text centuriesLabel;
        [SerializeField] TMP_Text branchesLabel;

        string _currentId;

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
            Refresh();
        }

        void Refresh()
        {
            var data = repository?.GetById(_currentId);
            if (data == null)
                return;

            var english = LocaleHelper.IsEnglish;
            nameText.text = data.GetFullName(english);
            datesText.text = data.GetLifeDatesLabel(english);
            countriesText.text = data.GetCountriesDisplay(english);
            centuriesText.text = data.GetCenturiesDisplay(english);
            branchesText.text = data.GetBranchesDisplay(english);
            achievementsText.text = data.GetAchievements(english);
            personalLifeText.text = data.GetPersonalLife(english);

            if (countriesLabel != null)
                countriesLabel.text = english ? "Countries" : "Страны";
            if (centuriesLabel != null)
                centuriesLabel.text = english ? "Centuries" : "Века";
            if (branchesLabel != null)
                branchesLabel.text = english ? "Fields" : "Разделы";
            if (achievementsLabel != null)
                achievementsLabel.text = english ? "Achievements and contributions" : "Достижения и вклад";
            if (personalLifeLabel != null)
                personalLifeLabel.text = english ? "Personal life" : "Личная жизнь";
        }
    }
}
