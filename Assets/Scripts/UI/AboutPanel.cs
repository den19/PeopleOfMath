using System.Collections;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class AboutPanel : MonoBehaviour
    {
        const string RateUrl = "https://www.rustore.ru/catalog/app/com.peopleofmath.app";
        const string MoreAppsUrl = "https://www.rustore.ru/catalog/developer/emahzj";
        const string ContactMailto = "mailto:den.kolesov@gmail.com";
        const float NetworkPollInterval = 0.75f;

        [SerializeField] TMP_Text versionText;
        [SerializeField] Button rateButton;
        [SerializeField] Button moreAppsButton;
        [SerializeField] Button emailButton;
        [SerializeField] GameObject rateSection;
        [SerializeField] GameObject moreAppsSection;

        Coroutine _networkPoll;
        Coroutine _layoutRebuild;
        bool? _lastReachable;
        RectTransform _scrollContent;

        void Awake()
        {
            BindButton(rateButton, OnRateClicked);
            BindButton(moreAppsButton, OnMoreAppsClicked);
            BindButton(emailButton, OnEmailClicked);
            CacheScrollContent();
        }

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            RefreshVersion();
            RefreshTheme();
            ApplyNetworkVisibility(force: true);
            if (_networkPoll != null)
                StopCoroutine(_networkPoll);
            _networkPoll = StartCoroutine(PollNetwork());
            ScheduleLayoutRebuild();
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            if (_layoutRebuild != null)
            {
                StopCoroutine(_layoutRebuild);
                _layoutRebuild = null;
            }

            if (_networkPoll == null)
                return;

            StopCoroutine(_networkPoll);
            _networkPoll = null;
        }

        public void RefreshTheme()
        {
            if (rateButton != null)
                UiButtonStyler.ApplyAccentWarm(rateButton);
            if (moreAppsButton != null)
                UiButtonStyler.Apply(moreAppsButton, UiButtonStyle.Secondary);
            if (emailButton != null)
                UiButtonStyler.Apply(emailButton, UiButtonStyle.Secondary);

            if (isActiveAndEnabled)
                ScheduleLayoutRebuild();
        }

        void OnLocaleChanged(Locale _)
        {
            RefreshVersion();
            ScheduleLayoutRebuild();
        }

        void RefreshVersion()
        {
            if (versionText != null)
                versionText.text = UiStrings.Format("about_version", Application.version);
        }

        void ScheduleLayoutRebuild()
        {
            if (!isActiveAndEnabled)
                return;

            if (_layoutRebuild != null)
                StopCoroutine(_layoutRebuild);
            _layoutRebuild = StartCoroutine(RebuildLayoutDeferred());
        }

        IEnumerator RebuildLayoutDeferred()
        {
            // First frame: rect widths may still be zero after SetActive(true).
            RebuildLayoutNow();
            yield return null;
            Canvas.ForceUpdateCanvases();
            RebuildLayoutNow();
            yield return null;
            RebuildLayoutNow();
            _layoutRebuild = null;
        }

        void RebuildLayoutNow()
        {
            CacheScrollContent();

            var fontScope = GetComponent<FontSizeScope>();
            if (fontScope != null)
                fontScope.Apply();
            else
            {
                foreach (var layout in GetComponentsInChildren<TmpLayoutHeight>(true))
                    layout.RefreshHeight();
            }

            Canvas.ForceUpdateCanvases();
            if (_scrollContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollContent);
        }

        void CacheScrollContent()
        {
            if (_scrollContent != null)
                return;

            var scroll = GetComponentInChildren<ScrollRect>(true);
            if (scroll != null)
                _scrollContent = scroll.content;
        }

        IEnumerator PollNetwork()
        {
            var wait = new WaitForSecondsRealtime(NetworkPollInterval);
            while (enabled && gameObject.activeInHierarchy)
            {
                ApplyNetworkVisibility(force: false);
                yield return wait;
            }
        }

        void ApplyNetworkVisibility(bool force)
        {
            var reachable = ExternalLinkOpener.HasNetworkConnection();
            if (!force && _lastReachable == reachable)
                return;

            _lastReachable = reachable;
            if (rateSection != null)
                rateSection.SetActive(reachable);
            if (moreAppsSection != null)
                moreAppsSection.SetActive(reachable);

            if (isActiveAndEnabled && !force)
                ScheduleLayoutRebuild();
        }

        public void OnRateClicked() => ExternalLinkOpener.Open(RateUrl);

        public void OnMoreAppsClicked() => ExternalLinkOpener.Open(MoreAppsUrl);

        public void OnEmailClicked()
        {
            if (string.IsNullOrWhiteSpace(ContactMailto))
                return;

            Application.OpenURL(ContactMailto);
        }

        static void BindButton(Button button, UnityAction handler)
        {
            if (button == null || handler == null)
                return;

            button.onClick.RemoveListener(handler);
            button.onClick.AddListener(handler);
        }
    }
}
