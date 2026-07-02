using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using UnityEngine;

namespace PeopleOfMath.UI
{
    public class PortraitDetailSection : MathematicianDetailSection
    {
        [SerializeField] PortraitGalleryView gallery;

        public override void Bind(MathematicianData data, bool english) =>
            gallery?.Bind(data);

        public override string GetSectionTitle(bool english) =>
            UiStrings.Get("section_portraits");
    }
}
