using PeopleOfMath.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.Editor
{
    public static class HomeListPanelLayout
    {
        public static void ConfigureBrowseScrollContent(VerticalLayoutGroup vlg)
        {
            vlg.padding = new RectOffset(
                UiLayoutMetrics.BrowseScrollPaddingLeft,
                UiLayoutMetrics.BrowseScrollPaddingRight,
                UiLayoutMetrics.BrowseScrollPaddingTop,
                UiLayoutMetrics.BrowseScrollPaddingBottom);
            vlg.spacing = UiLayoutMetrics.BrowseScrollSpacing;
            EditorUtility.SetDirty(vlg);
        }

        public static void ConfigureBrowseGroup(VerticalLayoutGroup vlg)
        {
            vlg.spacing = UiLayoutMetrics.GroupSpacing;
            vlg.padding = new RectOffset(0, 0, UiLayoutMetrics.GroupPaddingTop, UiLayoutMetrics.GroupPaddingBottom);
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            EditorUtility.SetDirty(vlg);
        }

        public static void ConfigureSectionLabel(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, UiLayoutMetrics.SectionLabelHeight);

            var le = go.GetComponent<LayoutElement>();
            if (le != null)
                le.preferredHeight = UiLayoutMetrics.SectionLabelHeight;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                return;

            var fontSize = UiLayoutMetrics.SectionLabelFontSize;
            tmp.fontSize = fontSize;
            tmp.color = UiTheme.TextSecondary;
            var so = new SerializedObject(tmp);
            var baseProp = so.FindProperty("m_fontSizeBase");
            if (baseProp != null)
                baseProp.floatValue = fontSize;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(go);
        }

        public static void ConfigureEmptyState(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = UiLayoutMetrics.EmptyStatePosition;
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, UiLayoutMetrics.EmptyStateLineHeight);
            }

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                return;

            var fontSize = UiLayoutMetrics.EmptyStateFontSize;
            tmp.fontSize = fontSize;
            tmp.color = UiTheme.TextSecondary;
            var so = new SerializedObject(tmp);
            var baseProp = so.FindProperty("m_fontSizeBase");
            if (baseProp != null)
                baseProp.floatValue = fontSize;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(go);
        }

        public static void ConfigureSearchBar(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, UiLayoutMetrics.SearchBarHeight);

            var le = go.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredHeight = UiLayoutMetrics.SearchBarHeight;
                le.minHeight = UiLayoutMetrics.SearchBarHeight;
            }

            PeopleOfMathProjectSetup.ConfigureSearchBar(go);
        }

        public static void ApplyToPanel(GameObject panel)
        {
            if (panel == null)
                return;

            foreach (var scroll in panel.GetComponentsInChildren<ScrollRect>(true))
            {
                var content = scroll.content;
                if (content == null)
                    continue;

                var vlg = content.GetComponent<VerticalLayoutGroup>();
                if (vlg != null)
                    ConfigureBrowseScrollContent(vlg);
            }

            foreach (var vlg in panel.GetComponentsInChildren<VerticalLayoutGroup>(true))
            {
                if (vlg.gameObject.name.EndsWith("Group"))
                    ConfigureBrowseGroup(vlg);
            }

            foreach (var tmp in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                var name = tmp.gameObject.name;
                if (name.StartsWith("section_"))
                    ConfigureSectionLabel(tmp.gameObject);
                else if (name == "Empty")
                    ConfigureEmptyState(tmp.gameObject);
            }

            var searchBar = panel.transform.Find("SearchBar");
            if (searchBar != null)
                ConfigureSearchBar(searchBar.gameObject);

            var homeScroll = panel.transform.Find("HomeScroll")?.GetComponent<ScrollRect>();
            if (searchBar != null && homeScroll != null)
                PinHomeSearchAndScroll(searchBar.gameObject, homeScroll);
        }

        static void PinHomeSearchAndScroll(GameObject searchBar, ScrollRect scroll)
        {
            var searchRt = searchBar.GetComponent<RectTransform>();
            searchRt.anchorMin = new Vector2(0, 1);
            searchRt.anchorMax = new Vector2(1, 1);
            searchRt.pivot = new Vector2(0.5f, 1);
            searchRt.anchoredPosition = new Vector2(0, -UiLayoutMetrics.SearchBarMarginTop);
            searchRt.sizeDelta = new Vector2(
                -(UiLayoutMetrics.BrowseScrollPaddingLeft + UiLayoutMetrics.BrowseScrollPaddingRight),
                UiLayoutMetrics.SearchBarHeight);
            searchRt.offsetMin = new Vector2(UiLayoutMetrics.BrowseScrollPaddingLeft, searchRt.offsetMin.y);
            searchRt.offsetMax = new Vector2(-UiLayoutMetrics.BrowseScrollPaddingRight, -UiLayoutMetrics.SearchBarMarginTop);

            var scrollRt = scroll.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = new Vector2(0, -UiLayoutMetrics.SearchBarTotalTopInset);
            EditorUtility.SetDirty(scrollRt);
            EditorUtility.SetDirty(searchRt);
        }
    }
}
