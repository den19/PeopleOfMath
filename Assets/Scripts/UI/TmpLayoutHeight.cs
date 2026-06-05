using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    [RequireComponent(typeof(LayoutElement))]
    public class TmpLayoutHeight : MonoBehaviour
    {
        [SerializeField] float padding = 10f;
        [SerializeField] float minHeight = 48f;

        TextMeshProUGUI _text;
        LayoutElement _layout;
        RectTransform _rect;

        void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _layout = GetComponent<LayoutElement>();
            _rect = transform as RectTransform;
        }

        void OnEnable() => RefreshHeight();

        public void RefreshHeight()
        {
            if (_text == null)
                _text = GetComponent<TextMeshProUGUI>();
            if (_layout == null)
                _layout = GetComponent<LayoutElement>();
            if (_rect == null)
                _rect = transform as RectTransform;
            if (_text == null || _layout == null || _rect == null)
                return;

            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.overflowMode = TextOverflowModes.Overflow;

            var width = ResolveTextWidth(_rect);
            _text.ForceMeshUpdate();
            var preferred = _text.GetPreferredValues(width, Mathf.Infinity).y;
            var height = Mathf.Max(minHeight, preferred + padding);

            _layout.preferredHeight = height;
            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            RebuildLayoutChain(_rect);
        }

        static float ResolveTextWidth(RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();

            var parent = rect.parent as RectTransform;
            if (parent != null)
            {
                var preferred = LayoutUtility.GetPreferredWidth(parent);
                if (preferred > 1f)
                    return preferred;

                var rectWidth = parent.rect.width;
                if (rectWidth > 1f)
                    return rectWidth;
            }

            return rect.rect.width > 1f ? rect.rect.width : 900f;
        }

        static void RebuildLayoutChain(RectTransform start)
        {
            var current = start.parent as RectTransform;
            while (current != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(current);
                if (current.GetComponent<ContentSizeFitter>() != null)
                    break;
                current = current.parent as RectTransform;
            }
        }
    }
}
