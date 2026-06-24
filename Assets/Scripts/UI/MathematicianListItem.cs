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
        [SerializeField] Image portraitImage;
        [SerializeField] Button button;

        string _id;
        Action<string> _onSelected;

        void Awake() => ConfigureBioText();

        void OnValidate() => ConfigureBioText();

        static void ConfigureBioText(TMP_Text text)
        {
            if (text == null)
                return;

            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        void ConfigureBioText() => ConfigureBioText(bioText);

        public void Bind(MathematicianData data, Action<string> onSelected)
        {
            _id = data.id;
            _onSelected = onSelected;
            var english = LocaleHelper.IsEnglish;
            nameText.text = data.GetFullName(english);
            datesText.text = data.GetLifeDatesLabel(english);
            bioText.text = data.GetShortBio(english);
            BindPortrait(data);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onSelected?.Invoke(_id));
        }

        void BindPortrait(MathematicianData data)
        {
            if (portraitImage == null)
                return;

            var sprite = PortraitResolver.GetPrimaryPortrait(data);
            portraitImage.sprite = sprite;
            portraitImage.preserveAspect = true;
            portraitImage.color = sprite != null ? Color.white : UiTheme.PortraitPlaceholder;
            portraitImage.raycastTarget = false;
        }
    }
}
