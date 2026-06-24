using TMPro;
using UnityEngine;

namespace PeopleOfMath.UI
{
    public class FontSizeScope : MonoBehaviour
    {
        void OnEnable()
        {
            FontSizeHelper.FontSizeChanged += Apply;
            Apply();
        }

        void OnDisable()
        {
            FontSizeHelper.FontSizeChanged -= Apply;
        }

        public void Apply()
        {
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                FontSizeHelper.ApplyTo(text);

            foreach (var layout in GetComponentsInChildren<TmpLayoutHeight>(true))
                layout.RefreshHeight();

            foreach (var item in GetComponentsInChildren<MathematicianListItem>(true))
                item.RefreshLayout();
        }
    }
}
