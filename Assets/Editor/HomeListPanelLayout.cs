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
            var so = new SerializedObject(tmp);
            var baseProp = so.FindProperty("m_fontSizeBase");
            if (baseProp != null)
                baseProp.floatValue = fontSize;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(go);
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
        }
    }
}
