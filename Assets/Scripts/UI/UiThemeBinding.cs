using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    [DisallowMultipleComponent]
    public class UiThemeBinding : MonoBehaviour
    {
        [SerializeField] UiThemeToken token;

        public UiThemeToken Token => token;

        Image _image;
        TMP_Text _text;

        void Awake()
        {
            _image = GetComponent<Image>();
            _text = GetComponent<TMP_Text>();
        }

        public void Apply()
        {
            var color = UiTheme.GetToken(token);
            if (_image == null)
                _image = GetComponent<Image>();
            if (_text == null)
                _text = GetComponent<TMP_Text>();

            if (_image != null)
                _image.color = color;
            if (_text != null)
                _text.color = color;
        }
    }
}
