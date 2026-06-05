using PeopleOfMath.Data;
using TMPro;
using UnityEngine;

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
        [SerializeField] TMP_Text labelText;
        [SerializeField] TMP_Text bodyText;

        public override void Bind(MathematicianData data, bool english)
        {
            if (data == null)
                return;

            if (labelText != null)
                labelText.text = GetSectionTitle(english);
            if (bodyText != null)
            {
                bodyText.text = sectionKind switch
                {
                    LabeledDetailSectionKind.Countries => data.GetCountriesDisplay(english),
                    LabeledDetailSectionKind.Centuries => data.GetCenturiesDisplay(english),
                    LabeledDetailSectionKind.Fields => data.GetBranchesDisplay(english),
                    _ => ""
                };
            }
        }

        public override string GetSectionTitle(bool english) => sectionKind switch
        {
            LabeledDetailSectionKind.Countries => english ? "Countries" : "Страны",
            LabeledDetailSectionKind.Centuries => english ? "Centuries" : "Века",
            LabeledDetailSectionKind.Fields => english ? "Fields" : "Разделы",
            _ => ""
        };
    }
}
