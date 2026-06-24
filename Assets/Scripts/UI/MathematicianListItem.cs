using System;
using System.Collections;
using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class MathematicianListItem : MonoBehaviour
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text datesText;
        [SerializeField] TMP_Text bioText;
        [SerializeField] Image portraitImage;
        [SerializeField] Button button;

        string _id;
        Action<string> _onSelected;
        Coroutine _layoutRefreshRoutine;

        void Awake() => ConfigureBioText();

        void OnValidate() => ConfigureBioText();

        static void ConfigureBioText(TMP_Text text)
        {
            if (text == null)
                return;

            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        void ConfigureBioText() => ConfigureBioText(bioText);

        public void Bind(MathematicianData data, Action<string> onSelected)
        {
            _id = data.id;
            _onSelected = onSelected;
            var english = LocaleHelper.IsEnglish;
            nameText.text = data.GetFullName(english);
            datesText.text = data.GetLifeDatesLabel(english);
            bioText.text = data.GetShortBio(english);
            BindPortrait(data);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onSelected?.Invoke(_id));
            ScheduleLayoutRefresh();
        }

        public void RefreshLayout()
        {
            if (nameText == null || datesText == null || bioText == null)
                return;

            var multiplier = FontSizeHelper.Multiplier;
            var textWidth = ResolveTextColumnWidth(nameText.rectTransform);

            nameText.textWrappingMode = TextWrappingModes.Normal;
            nameText.overflowMode = TextOverflowModes.Overflow;
            datesText.textWrappingMode = TextWrappingModes.Normal;
            datesText.overflowMode = TextOverflowModes.Overflow;
            ConfigureBioText(bioText);

            var nameHeight = MeasureTextHeight(nameText, textWidth, nameText.fontSize * 1.1f);
            var datesHeight = MeasureTextHeight(datesText, textWidth, datesText.fontSize * 1.1f);
            var bioHeight = ListItemLayoutMetrics.BioBaseHeight * multiplier;

            var y = ListItemLayoutMetrics.TopPadding;
            PositionTextBlock(nameText.rectTransform, y, nameHeight);
            y += nameHeight + ListItemLayoutMetrics.VerticalGap;
            PositionTextBlock(datesText.rectTransform, y, datesHeight);
            y += datesHeight + ListItemLayoutMetrics.VerticalGap;
            PositionTextBlock(bioText.rectTransform, y, bioHeight);

            var contentHeight = y + bioHeight + ListItemLayoutMetrics.TopPadding;
            var rowHeight = Mathf.Max(ListItemLayoutMetrics.RowMinHeight * multiplier, contentHeight);

            var rootRt = transform as RectTransform;
            if (rootRt != null)
                rootRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowHeight);

            var le = GetComponent<LayoutElement>();
            if (le != null)
                le.preferredHeight = rowHeight;
        }

        void ScheduleLayoutRefresh()
        {
            RefreshLayout();
            if (!isActiveAndEnabled)
                return;

            if (_layoutRefreshRoutine != null)
                StopCoroutine(_layoutRefreshRoutine);
            _layoutRefreshRoutine = StartCoroutine(RefreshLayoutDeferred());
        }

        IEnumerator RefreshLayoutDeferred()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            RefreshLayout();
            _layoutRefreshRoutine = null;
        }

        static void PositionTextBlock(RectTransform rt, float topOffset, float height)
        {
            rt.anchoredPosition = new Vector2(ListItemLayoutMetrics.TextColumnLeft, -topOffset);
            rt.sizeDelta = new Vector2(-ListItemLayoutMetrics.TextWidthInset, height);
        }

        static float MeasureTextHeight(TMP_Text text, float width, float minHeight)
        {
            text.ForceMeshUpdate();
            var preferred = text.GetPreferredValues(width, Mathf.Infinity).y;
            return Mathf.Max(minHeight, preferred);
        }

        static float ResolveTextColumnWidth(RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();

            var parent = rect.parent as RectTransform;
            if (parent != null)
            {
                var parentWidth = parent.rect.width;
                if (parentWidth > 1f)
                    return parentWidth - ListItemLayoutMetrics.TextWidthInset;
            }

            return 900f - ListItemLayoutMetrics.TextWidthInset;
        }

        void BindPortrait(MathematicianData data)
        {
            if (portraitImage == null)
                return;

            var sprite = PortraitResolver.GetPrimaryPortrait(data);
            portraitImage.sprite = sprite;
            portraitImage.preserveAspect = true;
            portraitImage.color = sprite != null ? Color.white : UiTheme.PortraitPlaceholder;
            portraitImage.raycastTarget = false;
        }
    }
}
