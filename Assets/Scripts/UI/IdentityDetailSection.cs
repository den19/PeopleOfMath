using System.Collections;
using PeopleOfMath.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class IdentityDetailSection : MathematicianDetailSection
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text datesText;

        Coroutine _heightRefreshRoutine;
        bool _pendingHeightRefresh;

        void Awake() => EnsureAutoHeight(nameText);

        void OnEnable()
        {
            if (_pendingHeightRefresh)
                ScheduleNameHeightRefresh();
        }

        public override void Bind(MathematicianData data, bool english)
        {
            if (data == null)
                return;

            if (nameText != null)
                nameText.text = data.GetFullName(english);
            if (datesText != null)
                datesText.text = data.GetLifeDatesLabel(english);

            ScheduleNameHeightRefresh();
        }

        public override string GetSectionTitle(bool english) =>
            english ? "Name and dates" : "Имя и даты";

        void ScheduleNameHeightRefresh()
        {
            if (!isActiveAndEnabled)
            {
                _pendingHeightRefresh = true;
                RefreshNameHeight();
                return;
            }

            _pendingHeightRefresh = false;
            if (_heightRefreshRoutine != null)
                StopCoroutine(_heightRefreshRoutine);
            _heightRefreshRoutine = StartCoroutine(RefreshNameHeightsDeferred());
        }

        IEnumerator RefreshNameHeightsDeferred()
        {
            RefreshNameHeight();
            yield return null;
            Canvas.ForceUpdateCanvases();
            RefreshNameHeight();
            yield return null;
            RefreshNameHeight();
            _heightRefreshRoutine = null;
        }

        void RefreshNameHeight()
        {
            EnsureAutoHeight(nameText);
            nameText?.GetComponent<TmpLayoutHeight>()?.RefreshHeight();

            var parent = nameText != null ? nameText.transform.parent as RectTransform : null;
            if (parent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }

        static void EnsureAutoHeight(TMP_Text text)
        {
            if (text == null)
                return;

            var go = text.gameObject;
            var rt = go.GetComponent<RectTransform>();
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;

            if (rt != null)
                rt.localScale = Vector3.one;

            var minHeight = text.fontSize * 1.1f;

            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = -1;
            le.minHeight = minHeight;

            var fitter = go.GetComponent<ContentSizeFitter>();
            if (fitter != null)
                Destroy(fitter);

            if (go.GetComponent<TmpLayoutHeight>() == null)
                go.AddComponent<TmpLayoutHeight>();

            if (rt != null)
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, minHeight);
        }
    }
}
