using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    [RequireComponent(typeof(ScrollRect))]
    public class GalleryScrollSnap : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] float snapThreshold = 50f;
        [SerializeField] float snapSpeed = 12f;

        const float SettleEpsilon = 0.001f;

        ScrollRect _scroll;
        RectTransform _content;
        int _pageCount;
        int _targetIndex;
        bool _dragging;
        bool _settled = true;

        public int CurrentIndex { get; private set; }

        public int PageCount => _pageCount;

        public event System.Action<int> PageChanged;

        void Awake() => EnsureInitialized();

        void EnsureInitialized()
        {
            if (_scroll != null)
                return;

            _scroll = GetComponent<ScrollRect>();
            if (_scroll == null)
                return;

            _content = _scroll.content;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.inertia = false;
        }

        public void Configure(int pageCount)
        {
            EnsureInitialized();
            if (_scroll == null)
                return;

            _pageCount = Mathf.Max(1, pageCount);
            CurrentIndex = Mathf.Clamp(CurrentIndex, 0, _pageCount - 1);
            _targetIndex = CurrentIndex;
            SnapImmediate(CurrentIndex);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            _settled = false;
        }

        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            _settled = false;
            if (_pageCount <= 1)
                return;

            var delta = eventData.position.x - eventData.pressPosition.x;
            if (Mathf.Abs(delta) >= snapThreshold)
            {
                if (delta < 0)
                    _targetIndex = Mathf.Min(_pageCount - 1, CurrentIndex + 1);
                else
                    _targetIndex = Mathf.Max(0, CurrentIndex - 1);
            }
            else
                _targetIndex = NearestIndex();
        }

        void Update()
        {
            if (_scroll == null)
                EnsureInitialized();
            if (_pageCount <= 1 || _content == null || _scroll == null || _dragging || _settled)
                return;

            var target = GetNormalizedPosition(_targetIndex);
            var current = _scroll.horizontalNormalizedPosition;
            if (Mathf.Abs(current - target) < SettleEpsilon)
            {
                _scroll.horizontalNormalizedPosition = target;
                if (CurrentIndex != _targetIndex)
                {
                    CurrentIndex = _targetIndex;
                    PageChanged?.Invoke(CurrentIndex);
                }

                _settled = true;
                return;
            }

            _scroll.horizontalNormalizedPosition = Mathf.Lerp(
                current,
                target,
                Time.unscaledDeltaTime * snapSpeed);
        }

        int NearestIndex()
        {
            if (_pageCount <= 1 || _scroll == null)
                return 0;
            var pos = _scroll.horizontalNormalizedPosition;
            var idx = Mathf.RoundToInt(pos * (_pageCount - 1));
            return Mathf.Clamp(idx, 0, _pageCount - 1);
        }

        float GetNormalizedPosition(int index)
        {
            if (_pageCount <= 1)
                return 0f;
            return index / (float)(_pageCount - 1);
        }

        void SnapImmediate(int index)
        {
            EnsureInitialized();
            if (_scroll == null)
                return;

            _targetIndex = index;
            CurrentIndex = index;
            _scroll.horizontalNormalizedPosition = GetNormalizedPosition(index);
            _settled = true;
            PageChanged?.Invoke(CurrentIndex);
        }
    }
}
