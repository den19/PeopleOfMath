using System;
using System.Collections;
using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using PeopleOfMath.Sharing;
using PeopleOfMath.Text;
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
        [SerializeField] ShareIconButton shareButton;
        [SerializeField] FavoriteIconButton favoriteButton;

        string _id;
        Action<string> _onSelected;
        MathematicianData _data;
        Coroutine _layoutRefreshRoutine;

        void Awake() => ConfigureBioText();

        void OnEnable()
        {
            FavoritesHelper.FavoritesChanged += OnFavoritesChanged;
            ApplyCardTheme();
        }

        void OnDisable()
        {
            FavoritesHelper.FavoritesChanged -= OnFavoritesChanged;
        }

        void OnFavoritesChanged()
        {
            if (string.IsNullOrEmpty(_id) || favoriteButton == null)
                return;

            favoriteButton.SetFavorite(FavoritesHelper.IsFavorite(_id));
        }

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
            if (data == null)
                return;

            _id = data.id;
            _data = data;
            _onSelected = onSelected;
            var english = LocaleHelper.IsEnglish;
            nameText.text = data.GetFullName(english);
            datesText.text = data.GetLifeDatesLabel(english);
            bioText.text = data.GetShortBio(english);
            BindPortrait(data);
            BindShareButton(data, english);
            BindFavoriteButton();

            ApplyCardTheme();

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onSelected?.Invoke(_id));
            ScheduleLayoutRefresh();
        }

        void ApplyCardTheme()
        {
            var themedCard = GetComponent<UiThemedCard>();
            if (themedCard == null)
                return;

            themedCard.Configure(UiCardVariant.ListItem);
            if (!string.IsNullOrEmpty(_id))
                themedCard.SetHighlightActive(FavoritesHelper.IsFavorite(_id));
        }

        void BindShareButton(MathematicianData data, bool english)
        {
            if (shareButton == null)
                return;

            var hasWiki = !string.IsNullOrWhiteSpace(data.GetWikipediaUrl(english));
            shareButton.SetVisible(hasWiki);
            if (!hasWiki)
                return;

            shareButton.SetClickHandler(() =>
            {
                var text = MathematicianShareText.BuildListShare(_data, LocaleHelper.IsEnglish);
                NativeShare.ShareText(text);
            });
        }

        void BindFavoriteButton()
        {
            if (favoriteButton == null)
                return;

            favoriteButton.SetVisible(true);
            favoriteButton.SetFavorite(FavoritesHelper.IsFavorite(_id));
            favoriteButton.SetClickHandler(() =>
            {
                FavoritesHelper.Toggle(_id);
                favoriteButton.SetFavorite(FavoritesHelper.IsFavorite(_id));
            });
        }

        public void RefreshLayout()
        {
            if (nameText == null || datesText == null || bioText == null)
                return;

            var multiplier = FontSizeHelper.Multiplier;
            var textWidth = ResolveTextColumnWidth(nameText.rectTransform);

            var nameHeight = ConfigureNameText(textWidth);
            datesText.textWrappingMode = TextWrappingModes.Normal;
            datesText.overflowMode = TextOverflowModes.Overflow;
            datesText.enableAutoSizing = false;
            ConfigureBioText(bioText);

            var datesHeight = MeasureTextHeight(datesText, textWidth, datesText.fontSize * 1.1f);

            var y = ListItemLayoutMetrics.TopPadding;
            PositionTextBlock(nameText.rectTransform, y, nameHeight);
            y += nameHeight + ListItemLayoutMetrics.VerticalGap;
            PositionTextBlock(datesText.rectTransform, y, datesHeight);
            y += datesHeight + ListItemLayoutMetrics.VerticalGap;

            var bioTop = y;
            var minContentHeight = bioTop
                + ListItemLayoutMetrics.BioBaseHeight * multiplier
                + ListItemLayoutMetrics.TopPadding;
            var rowHeight = Mathf.Max(
                ListItemLayoutMetrics.RowMinHeight * multiplier,
                minContentHeight,
                ListItemLayoutMetrics.LeftColumnHeight);
            var bioHeight = rowHeight - bioTop - ListItemLayoutMetrics.TopPadding;
            PositionTextBlock(bioText.rectTransform, bioTop, bioHeight);

            PositionPortrait();
            PositionActionButtons();

            var rootRt = transform as RectTransform;
            if (rootRt != null)
                rootRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowHeight);

            var le = GetComponent<LayoutElement>();
            if (le != null)
                le.preferredHeight = rowHeight;
        }

        float ConfigureNameText(float textWidth)
        {
            nameText.enableAutoSizing = false;
            FontSizeHelper.ApplyTo(nameText);

            var fontSizeMax = nameText.fontSize;
            var nameHeight = ListItemLayoutMetrics.NameBlockHeight(fontSizeMax);

            nameText.textWrappingMode = TextWrappingModes.Normal;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.fontSizeMax = fontSizeMax;
            nameText.fontSizeMin = fontSizeMax * ListItemLayoutMetrics.NameFontSizeMinMultiplier;
            nameText.enableAutoSizing = true;

            nameText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth);
            nameText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, nameHeight);
            TmpOrphanWrap.AvoidShortLastLine(nameText, textWidth);
            nameText.ForceMeshUpdate();

            return nameHeight;
        }

        void PositionPortrait()
        {
            if (portraitImage == null)
                return;

            var rt = portraitImage.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(
                ListItemLayoutMetrics.LeftPadding,
                -ListItemLayoutMetrics.TopPadding);
            rt.sizeDelta = new Vector2(
                ListItemLayoutMetrics.PortraitSize,
                ListItemLayoutMetrics.PortraitSize);
        }

        void PositionActionButtons()
        {
            PositionActionButton(
                favoriteButton != null ? favoriteButton.transform as RectTransform : null,
                ListItemLayoutMetrics.FavoriteButtonX,
                -ListItemLayoutMetrics.FavoriteButtonY);
            PositionActionButton(
                shareButton != null ? shareButton.transform as RectTransform : null,
                ListItemLayoutMetrics.ShareButtonX,
                -ListItemLayoutMetrics.ShareButtonY);
        }

        static void PositionActionButton(RectTransform rt, float x, float y)
        {
            if (rt == null)
                return;

            var size = ListItemLayoutMetrics.ActionButtonSize;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(size, size);

            var icon = rt.Find("Icon") as RectTransform;
            if (icon == null)
                return;

            icon.anchorMin = Vector2.zero;
            icon.anchorMax = Vector2.one;
            icon.pivot = new Vector2(0.5f, 0.5f);
            icon.anchoredPosition = Vector2.zero;
            icon.offsetMin = Vector2.zero;
            icon.offsetMax = Vector2.zero;
            icon.sizeDelta = Vector2.zero;
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
