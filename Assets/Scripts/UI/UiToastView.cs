using System.Collections;
using PeopleOfMath.Core;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class UiToastView : MonoBehaviour
    {
        const float VisibleSeconds = 2.2f;

        static readonly WaitForSecondsRealtime HideDelay = new(VisibleSeconds);
        static UiToastView _instance;

        CanvasGroup _canvasGroup;
        TMP_Text _label;
        Coroutine _hideRoutine;

        public static void Show(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            EnsureInstance();
            if (_instance == null)
                return;

            _instance.Present(message);
        }

        static void EnsureInstance()
        {
            if (_instance != null)
                return;

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;

            var go = new GameObject("UiToast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(canvas.transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 180f);
            rt.sizeDelta = new Vector2(900f, 96f);

            var bg = go.GetComponent<Image>();
            bg.sprite = UiSprites.RoundedRect;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0f, 0f, 0f, 0.82f);
            bg.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(24f, 12f);
            labelRt.offsetMax = new Vector2(-24f, -12f);

            var label = labelGo.GetComponent<TMP_Text>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 28f;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;

            _instance = go.AddComponent<UiToastView>();
            _instance._canvasGroup = go.GetComponent<CanvasGroup>();
            _instance._label = label;
            _instance._canvasGroup.alpha = 0f;
            go.SetActive(false);
        }

        void Present(string message)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _label.text = message;
            _canvasGroup.alpha = 1f;

            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        IEnumerator HideAfterDelay()
        {
            yield return HideDelay;
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            _hideRoutine = null;
        }
    }
}
