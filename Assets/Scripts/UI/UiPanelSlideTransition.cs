using System;
using System.Collections;
using UnityEngine;

namespace PeopleOfMath.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UiPanelSlideTransition : MonoBehaviour
    {
        const float DefaultOpenDuration = 0.28f;

        [SerializeField] RectTransform slideRoot;
        [SerializeField] float openDuration = DefaultOpenDuration;

        CanvasGroup _canvasGroup;
        Coroutine _routine;
        float _closedOffsetY;
        Vector2 _openAnchoredPosition;
        Action _onComplete;

        public bool IsAnimating { get; private set; }

        void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            EnsureSlideRoot();
            CacheLayout();
            SnapClosed();
        }

        void EnsureSlideRoot()
        {
            if (slideRoot != null)
                return;

            slideRoot = transform.Find("SlideBackdrop") as RectTransform
                ?? transform.Find("SlideRoot") as RectTransform;
        }

        void CacheLayout()
        {
            if (slideRoot == null)
                return;

            var parent = slideRoot.parent as RectTransform;
            _closedOffsetY = parent != null ? -parent.rect.height : -800f;
            _openAnchoredPosition = slideRoot.anchoredPosition;
        }

        public void SnapClosed()
        {
            StopRoutine();
            IsAnimating = false;

            if (slideRoot != null)
            {
                CacheLayout();
                slideRoot.anchoredPosition = new Vector2(_openAnchoredPosition.x, _closedOffsetY);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        public void SnapOpen()
        {
            StopRoutine();
            IsAnimating = false;

            if (slideRoot != null)
                slideRoot.anchoredPosition = _openAnchoredPosition;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
            }
        }

        public void PlayOpen(Action onComplete = null)
        {
            EnsureSlideRoot();
            CacheLayout();
            StopRoutine();
            _onComplete = onComplete;
            _routine = StartCoroutine(OpenRoutine());
        }

        IEnumerator OpenRoutine()
        {
            IsAnimating = true;

            if (_canvasGroup != null)
                _canvasGroup.blocksRaycasts = false;

            if (slideRoot != null)
                slideRoot.anchoredPosition = new Vector2(_openAnchoredPosition.x, _closedOffsetY);

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            var duration = Mathf.Max(0.01f, openDuration);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EaseOutCubic(t);

                if (slideRoot != null)
                {
                    var y = Mathf.Lerp(_closedOffsetY, _openAnchoredPosition.y, eased);
                    slideRoot.anchoredPosition = new Vector2(_openAnchoredPosition.x, y);
                }

                if (_canvasGroup != null)
                    _canvasGroup.alpha = eased;

                yield return null;
            }

            SnapOpen();
            _onComplete?.Invoke();
        }

        static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        void StopRoutine()
        {
            if (_routine == null)
                return;

            StopCoroutine(_routine);
            _routine = null;
        }
    }
}
