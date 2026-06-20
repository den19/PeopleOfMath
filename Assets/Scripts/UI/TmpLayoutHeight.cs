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
        [SerializeField] float maxHeight;

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
            if (maxHeight > 0f)
                height = Mathf.Min(height, maxHeight);

            _layout.preferredHeight = height;
            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            RebuildLayoutChain(_rect);
        }

        static float ResolveTextWidth(RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();

            var selfWidth = rect.rect.width;
            if (selfWidth > 1f)
                return selfWidth;

            var parent = rect.parent as RectTransform;
            if (parent != null)
            {
                var parentWidth = parent.rect.width;
                if (parentWidth > 1f)
                {
                    if (Mathf.Approximately(rect.anchorMin.x, 0f) && Mathf.Approximately(rect.anchorMax.x, 1f))
                        return parentWidth + rect.sizeDelta.x;

                    return parentWidth;
                }
            }

            return 900f;
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
