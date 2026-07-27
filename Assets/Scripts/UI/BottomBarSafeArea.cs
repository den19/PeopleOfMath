using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    /// <summary>
    /// Lifts BottomBar tab captions above the device bottom safe area (rounded
    /// corners / home indicator) without changing ContentArea width — horizontal
    /// insets on ContentArea would risk breaking the 2-column AdaptiveBrowseGrid.
    /// Horizontal padding is applied only to the BottomBar HLG so outer tab
    /// captions clear physical corner radii that often are not in Screen.safeArea.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class BottomBarSafeArea : MonoBehaviour
    {
        /// <summary>Single-row tab bar height on the reference 1080 canvas (no safe inset).</summary>
        public const float BottomBarHeight = 148f;

        /// <summary>Top (and base bottom before inset) padding on the BottomBar HLG.</summary>
        public const int BaseLayoutPadding = 4;

        /// <summary>
        /// Left/right HLG padding on the BottomBar only — clears physical corner
        /// radii without narrowing ContentArea.
        /// </summary>
        public const int HorizontalLayoutPadding = 18;

        /// <summary>
        /// Floor inset when Screen.safeArea underreports (or omits) corner radius.
        /// </summary>
        public const float MinBottomCornerInset = 20f;

        [SerializeField] RectTransform contentArea;
        [SerializeField] HorizontalLayoutGroup layoutGroup;

        RectTransform _barRt;
        Canvas _rootCanvas;
        float _appliedInset = -1f;

        public void Configure(RectTransform content, HorizontalLayoutGroup layout = null)
        {
            contentArea = content;
            if (layout != null)
                layoutGroup = layout;
        }

        void Awake()
        {
            CacheRefs();
            Apply();
        }

        void OnEnable() => Apply();

        void Update()
        {
            // Orientation / multi-window / cutout changes update safeArea without
            // always firing a dimensions callback on this rect.
            var inset = ResolveBottomInsetCanvas();
            if (!Mathf.Approximately(inset, _appliedInset))
                Apply(inset);
        }

        void CacheRefs()
        {
            if (_barRt == null)
                _barRt = transform as RectTransform;
            if (layoutGroup == null)
                layoutGroup = GetComponent<HorizontalLayoutGroup>();
            if (contentArea == null)
            {
                var canvas = transform.parent;
                var found = canvas != null ? canvas.Find("ContentArea") : null;
                if (found != null)
                    contentArea = found as RectTransform;
            }

            if (_rootCanvas == null)
            {
                var c = GetComponentInParent<Canvas>();
                _rootCanvas = c != null ? c.rootCanvas : null;
            }
        }

        public void Apply() => Apply(ResolveBottomInsetCanvas());

        void Apply(float inset)
        {
            CacheRefs();
            if (_barRt == null)
                return;

            inset = Mathf.Max(0f, inset);
            _appliedInset = inset;

            var totalHeight = BottomBarHeight + inset;
            _barRt.anchorMin = new Vector2(0f, 0f);
            _barRt.anchorMax = new Vector2(1f, 0f);
            _barRt.pivot = new Vector2(0.5f, 0.5f);
            _barRt.anchoredPosition = new Vector2(0f, totalHeight * 0.5f);
            _barRt.sizeDelta = new Vector2(0f, totalHeight);

            if (layoutGroup != null)
            {
                var bottomPad = BaseLayoutPadding + Mathf.RoundToInt(inset);
                layoutGroup.padding = new RectOffset(
                    HorizontalLayoutPadding,
                    HorizontalLayoutPadding,
                    BaseLayoutPadding,
                    bottomPad);
            }

            // Vertical only — never touch offsetMin.x (tiles / AdaptiveBrowseGrid).
            if (contentArea != null)
            {
                var min = contentArea.offsetMin;
                contentArea.offsetMin = new Vector2(min.x, totalHeight);
            }
        }

        float ResolveBottomInsetCanvas()
        {
            var safe = Screen.safeArea;
            var screenInset = Mathf.Max(0f, safe.y);
            var canvasInset = 0f;

            if (screenInset > 0.01f)
            {
                if (_rootCanvas == null)
                {
                    var c = GetComponentInParent<Canvas>();
                    _rootCanvas = c != null ? c.rootCanvas : null;
                }

                if (_rootCanvas != null && _rootCanvas.scaleFactor > 0.01f)
                    canvasInset = screenInset / _rootCanvas.scaleFactor;
                else if (Screen.height > 0 && _barRt != null)
                {
                    var canvasRt = _rootCanvas != null
                        ? _rootCanvas.transform as RectTransform
                        : _barRt.parent as RectTransform;
                    if (canvasRt != null && canvasRt.rect.height > 1f)
                        canvasInset = screenInset * (canvasRt.rect.height / Screen.height);
                    else
                        canvasInset = screenInset;
                }
                else
                    canvasInset = screenInset;
            }

            return Mathf.Max(canvasInset, MinBottomCornerInset);
        }
    }
}
