using System;
using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class MathematicianListItem : MonoBehaviour
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text datesText;
        [SerializeField] TMP_Text bioText;
        [SerializeField] Button button;

        string _id;
        Action<string> _onSelected;

        public void Bind(MathematicianData data, Action<string> onSelected)
        {
            _id = data.id;
            _onSelected = onSelected;
            var english = LocaleHelper.IsEnglish;
            nameText.text = data.GetFullName(english);
            datesText.text = data.GetLifeDatesLabel(english);
            bioText.text = data.GetShortBio(english);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onSelected?.Invoke(_id));
        }
    }
}
