using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class HomeListPanelUiConfigurator
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        static readonly string[] PrefabPaths =
        {
            "Assets/Prefabs/UI/FilterButton.prefab",
            "Assets/Prefabs/UI/SearchBar.prefab",
            "Assets/Prefabs/UI/MathematicianListItem.prefab",
            "Assets/Resources/MathematicianListItem.prefab",
            "Assets/Prefabs/UI/Detail/DetailSection_Identity.prefab"
        };

        [MenuItem("PeopleOfMath/Apply Home & List Panel Layout (+100%)")]
        public static void ApplyFromMenu() => Apply();

        public static void Apply()
        {
            foreach (var path in PrefabPaths)
                ApplyToPrefab(path);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var home = GameObject.Find("HomePanel");
            var list = GameObject.Find("ListPanel");

            if (home != null)
                HomeListPanelLayout.ApplyToPanel(home);
            if (list != null)
                HomeListPanelLayout.ApplyToPanel(list);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Home & List panel layout applied (+100% fonts, controls, spacing).");
        }

        static void ApplyToPrefab(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (path.EndsWith("FilterButton.prefab", System.StringComparison.OrdinalIgnoreCase))
                    PeopleOfMathProjectSetup.ConfigureFilterButton(root);
                else if (path.EndsWith("SearchBar.prefab", System.StringComparison.OrdinalIgnoreCase))
                    PeopleOfMathProjectSetup.ConfigureSearchBar(root);
                else if (path.EndsWith("DetailSection_Identity.prefab", System.StringComparison.OrdinalIgnoreCase))
                    PeopleOfMathProjectSetup.ConfigureIdentitySection(root);
                else
                    PeopleOfMathProjectSetup.ConfigureListItem(root);

                PeopleOfMathProjectSetup.RemoveMissingScripts(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
