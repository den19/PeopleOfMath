using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    [DisallowMultipleComponent]
    public class UiButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const string FillChildName = "Fill";
        const float PressScale = 0.97f;
        const float AnimDuration = 0.08f;

        Vector3 _normalScale = Vector3.one;
        Image _fillImage;
        Color _normalFillColor = Color.white;
        bool _pressed;
        Coroutine _scaleRoutine;

        void Awake()
        {
            _normalScale = transform.localScale;
            var fill = transform.Find(FillChildName);
            if (fill != null)
            {
                _fillImage = fill.GetComponent<Image>();
                if (_fillImage != null)
                    _normalFillColor = _fillImage.color;
            }
        }

        void OnDisable()
        {
            _pressed = false;
            transform.localScale = _normalScale;
            RestoreFillColor();
        }

        public void OnPointerDown(PointerEventData eventData) => SetPressed(true);

        public void OnPointerUp(PointerEventData eventData) => SetPressed(false);

        public void OnPointerExit(PointerEventData eventData) => SetPressed(false);

        void SetPressed(bool pressed)
        {
            if (_pressed == pressed)
                return;

            _pressed = pressed;
            AnimateScale(pressed ? PressScale : 1f);

            if (_fillImage == null)
                return;

            if (pressed)
            {
                var pressedColor = Color.Lerp(_normalFillColor, UiTheme.PrimaryPressed, 0.35f);
                pressedColor.a = _normalFillColor.a;
                _fillImage.color = pressedColor;
            }
            else
            {
                RestoreFillColor();
            }
        }

        void RestoreFillColor()
        {
            if (_fillImage != null)
                _fillImage.color = _normalFillColor;
        }

        public void RefreshNormalFillColor()
        {
            if (_fillImage != null)
                _normalFillColor = _fillImage.color;
        }

        void AnimateScale(float targetMultiplier)
        {
            if (_scaleRoutine != null)
                StopCoroutine(_scaleRoutine);

            _scaleRoutine = StartCoroutine(AnimateScaleRoutine(targetMultiplier));
        }

        IEnumerator AnimateScaleRoutine(float targetMultiplier)
        {
            var start = transform.localScale;
            var end = _normalScale * targetMultiplier;
            var elapsed = 0f;

            while (elapsed < AnimDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / AnimDuration);
                transform.localScale = Vector3.Lerp(start, end, t);
                yield return null;
            }

            transform.localScale = end;
            _scaleRoutine = null;
        }
    }
}
