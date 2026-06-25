using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class DetailSectionSwipeNavigator : MonoBehaviour
    {
        [SerializeField] DetailPanel detailPanel;
        [SerializeField] float swipeThreshold = 60f;
        [SerializeField] float horizontalDominanceRatio = 1.5f;

        Vector2 _pressPosition;
        bool _tracking;
        GalleryScrollSnap _galleryAtPress;

        void Update()
        {
            if (detailPanel == null || !detailPanel.isActiveAndEnabled)
            {
                _tracking = false;
                return;
            }

            var pointer = Pointer.current;
            if (pointer == null)
                return;

            if (pointer.press.wasPressedThisFrame)
                BeginTracking(pointer.position.ReadValue());

            if (_tracking && pointer.press.wasReleasedThisFrame)
                EndTracking(pointer.position.ReadValue());
        }

        void BeginTracking(Vector2 screenPosition)
        {
            _pressPosition = screenPosition;
            _tracking = true;
            _galleryAtPress = null;

            if (EventSystem.current == null)
                return;

            var eventData = new PointerEventData(EventSystem.current) { position = screenPosition };
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var hit in results)
            {
                var snap = hit.gameObject.GetComponentInParent<GalleryScrollSnap>();
                if (snap == null)
                    continue;

                var scroll = snap.GetComponent<ScrollRect>();
                if (scroll == null || !scroll.horizontal || scroll.vertical)
                    continue;

                _galleryAtPress = snap;
                break;
            }
        }

        void EndTracking(Vector2 releasePosition)
        {
            if (!_tracking)
                return;

            _tracking = false;

            var delta = releasePosition - _pressPosition;
            if (Mathf.Abs(delta.x) < swipeThreshold)
                return;

            if (Mathf.Abs(delta.x) < Mathf.Abs(delta.y) * horizontalDominanceRatio)
                return;

            if (delta.x < 0)
                TryGoNext();
            else
                TryGoPrevious();
        }

        void TryGoNext()
        {
            if (_galleryAtPress != null)
            {
                var lastIndex = Mathf.Max(0, _galleryAtPress.PageCount - 1);
                if (_galleryAtPress.CurrentIndex < lastIndex)
                    return;
            }

            detailPanel.GoNext();
        }

        void TryGoPrevious()
        {
            if (_galleryAtPress != null && _galleryAtPress.CurrentIndex > 0)
                return;

            detailPanel.GoPrevious();
        }
    }
}
