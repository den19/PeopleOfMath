using PeopleOfMath.Data;
using TMPro;
using UnityEngine;

namespace PeopleOfMath.UI
{
    public class IdentityDetailSection : MathematicianDetailSection
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text datesText;

        public override void Bind(MathematicianData data, bool english)
        {
            if (data == null)
                return;

            if (nameText != null)
                nameText.text = data.GetFullName(english);
            if (datesText != null)
                datesText.text = data.GetLifeDatesLabel(english);
        }

        public override string GetSectionTitle(bool english) =>
            english ? "Name and dates" : "Имя и даты";
    }
}
