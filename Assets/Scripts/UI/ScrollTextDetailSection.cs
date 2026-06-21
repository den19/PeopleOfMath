using System.Collections;
using PeopleOfMath.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public enum ScrollDetailSectionKind
    {
        Achievements,
        PersonalLife,
        ShortBio
    }

    public class ScrollTextDetailSection : MathematicianDetailSection
    {
        [SerializeField] ScrollDetailSectionKind sectionKind;
        [SerializeField] TMP_Text labelText;
        [SerializeField] TMP_Text bodyText;

        Coroutine _heightRefreshRoutine;
        bool _pendingHeightRefresh;

        void Awake()
        {
            EnsureAutoHeight(bodyText);
        }

        void OnEnable()
        {
            if (_pendingHeightRefresh)
                ScheduleBodyHeightRefresh();
        }

        public override void Bind(MathematicianData data, bool english)
        {
            if (data == null)
                return;

            if (labelText != null)
                labelText.text = GetSectionTitle(english);
            if (bodyText != null)
            {
                bodyText.text = sectionKind switch
                {
                    ScrollDetailSectionKind.Achievements => data.GetAchievements(english),
                    ScrollDetailSectionKind.PersonalLife => data.GetPersonalLife(english),
                    ScrollDetailSectionKind.ShortBio => data.GetShortBio(english),
                    _ => ""
                };
            }

            ScheduleBodyHeightRefresh();
        }

        public override string GetSectionTitle(bool english) => sectionKind switch
        {
            ScrollDetailSectionKind.Achievements => english
                ? "Achievements and contributions"
                : "Достижения и вклад",
            ScrollDetailSectionKind.PersonalLife => english ? "Personal life" : "Личная жизнь",
            ScrollDetailSectionKind.ShortBio => english ? "Short bio" : "Краткая биография",
            _ => ""
        };

        void ScheduleBodyHeightRefresh()
        {
            if (!isActiveAndEnabled)
            {
                _pendingHeightRefresh = true;
                RefreshBodyHeight();
                return;
            }

            _pendingHeightRefresh = false;
            if (_heightRefreshRoutine != null)
                StopCoroutine(_heightRefreshRoutine);
            _heightRefreshRoutine = StartCoroutine(RefreshBodyHeightsDeferred());
        }

        IEnumerator RefreshBodyHeightsDeferred()
        {
            RefreshBodyHeight();
            yield return null;
            Canvas.ForceUpdateCanvases();
            RefreshBodyHeight();
            yield return null;
            RefreshBodyHeight();
            _heightRefreshRoutine = null;
        }

        void RefreshBodyHeight()
        {
            EnsureAutoHeight(bodyText);
            bodyText?.GetComponent<TmpLayoutHeight>()?.RefreshHeight();
        }

        static void EnsureAutoHeight(TMP_Text text)
        {
            if (text == null)
                return;

            var go = text.gameObject;
            var rt = go.GetComponent<RectTransform>();
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;

            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = -1;
            le.minHeight = 69f;

            var fitter = go.GetComponent<ContentSizeFitter>() ?? go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (go.GetComponent<TmpLayoutHeight>() == null)
                go.AddComponent<TmpLayoutHeight>();

            if (rt != null)
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, le.minHeight);
        }
    }
}
