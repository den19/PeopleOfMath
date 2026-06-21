using PeopleOfMath.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.Editor
{
    public static class UiScaleApplicator
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        static readonly string[] PrefabPaths =
        {
            "Assets/Prefabs/UI/FilterButton.prefab",
            "Assets/Prefabs/UI/MathematicianListItem.prefab",
            "Assets/Resources/MathematicianListItem.prefab"
        };

        [MenuItem("PeopleOfMath/Apply UI Scale (fonts +20%, filter width +50%)")]
        public static void ApplyFromMenu() => Apply();

        [MenuItem("PeopleOfMath/Fix Detail Text Layout")]
        public static void FixDetailLayoutFromMenu()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            FixDetailTextLayout(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("PeopleOfMath/Fix Home Title Layout")]
        public static void FixHomeTitleLayoutFromMenu()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            FixHomeTitleLayout(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static void Apply()
        {
            foreach (var path in PrefabPaths)
                ApplyToPrefab(path);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var scaled = 0;
            foreach (var tmp in Object.FindObjectsByType<TextMeshProUGUI>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tmp.gameObject.scene != scene)
                    continue;

                if (IsHeaderTitle(tmp.gameObject))
                    continue;

                ScaleTextAdditional(tmp);
                scaled++;
            }

            FixDetailTextLayout(scene);
            FixHomeTitleLayout(scene);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"UI scale applied: additional fonts x{UiLayoutMetrics.AdditionalFontScale}, " +
                $"codegen FontScale {UiLayoutMetrics.FontScale}, filter width {UiLayoutMetrics.FilterButtonWidth}. " +
                $"TMP in scene: {scaled}.");
        }

        public static void FixDetailTextLayout(UnityEngine.SceneManagement.Scene scene)
        {
            ConfigureDetailContentVlg(scene);
            foreach (var tmp in Object.FindObjectsByType<TextMeshProUGUI>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tmp.gameObject.scene != scene)
                    continue;
                var n = tmp.gameObject.name;
                if (n == "Name" && IsIdentitySectionField(tmp.transform))
                    ConfigureIdentityNameField(tmp.gameObject);
                else if (n is "Achievements" or "Personal")
                    ConfigureAutoHeightField(tmp.gameObject);
            }
        }

        static bool IsIdentitySectionField(Transform field)
        {
            var section = field.parent;
            return section != null && section.name == "DetailSection_Identity";
        }

        static void ConfigureIdentityNameField(GameObject go)
        {
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                return;

            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;

            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
                rt.localScale = Vector3.one;

            var minHeight = UiLayoutMetrics.ScaleFont(48f);
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = -1;
            le.minHeight = minHeight;

            if (rt != null)
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, minHeight);

            var fitter = go.GetComponent<ContentSizeFitter>();
            if (fitter != null)
                Object.DestroyImmediate(fitter);

            var layoutHeight = go.GetComponent<TmpLayoutHeight>() ?? go.AddComponent<TmpLayoutHeight>();
            var so = new SerializedObject(layoutHeight);
            so.FindProperty("minHeight").floatValue = minHeight;
            so.FindProperty("padding").floatValue = 10f;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(go);
        }

        public static void FixHomeTitleLayout(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var tmp in Object.FindObjectsByType<TextMeshProUGUI>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tmp.gameObject.scene != scene)
                    continue;
                if (tmp.gameObject.name != "HomeTitle")
                    continue;

                PeopleOfMathProjectSetup.ConfigureHomeTitle(tmp.gameObject);
                EditorUtility.SetDirty(tmp.gameObject);
            }
        }

        static bool IsHeaderTitle(GameObject go) =>
            go.name is "HomeTitle" or "SettingsTitle";

        static void ConfigureDetailContentVlg(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var vlg in Object.FindObjectsByType<VerticalLayoutGroup>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (vlg.gameObject.scene != scene || vlg.gameObject.name != "Content")
                    continue;
                if (vlg.transform.parent == null || vlg.transform.parent.name != "Viewport")
                    continue;
                if (vlg.transform.parent.parent == null || vlg.transform.parent.parent.name != "DetailScroll")
                    continue;

                vlg.spacing = 16;
                vlg.childForceExpandHeight = false;
                vlg.childControlHeight = true;
                EditorUtility.SetDirty(vlg);
            }
        }

        static void ConfigureAutoHeightField(GameObject go)
        {
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                return;

            tmp.textWrappingMode = TextWrappingModes.Normal;

            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = -1;
            le.minHeight = UiLayoutMetrics.ScaleFont(80);

            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, le.minHeight);

            var fitter = go.GetComponent<ContentSizeFitter>() ?? go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layoutHeight = go.GetComponent<TmpLayoutHeight>() ?? go.AddComponent<TmpLayoutHeight>();
            var so = new SerializedObject(layoutHeight);
            so.FindProperty("minHeight").floatValue = UiLayoutMetrics.ScaleFont(48);
            so.FindProperty("padding").floatValue = 10f;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(go);
        }

        static void ApplyToPrefab(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var isFilter = path.EndsWith("FilterButton.prefab", System.StringComparison.OrdinalIgnoreCase);
                if (isFilter)
                {
                    PeopleOfMathProjectSetup.ConfigureFilterButton(root);
                }
                else
                    PeopleOfMathProjectSetup.ConfigureListItem(root);

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void ScaleTextAdditional(TMP_Text tmp)
        {
            var size = Mathf.Round(tmp.fontSize * UiLayoutMetrics.AdditionalFontScale);
            tmp.fontSize = size;

            var so = new SerializedObject(tmp);
            var baseProp = so.FindProperty("m_fontSizeBase");
            if (baseProp != null)
                baseProp.floatValue = size;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
