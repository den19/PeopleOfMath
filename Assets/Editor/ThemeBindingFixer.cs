using PeopleOfMath.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PeopleOfMath.Editor
{
    public static class ThemeBindingFixer
    {
        const string MainScenePath = "Assets/Scenes/Main.unity";

        static readonly string[] CardPrefabPaths =
        {
            "Assets/Prefabs/UI/MathematicianListItem.prefab",
            "Assets/Resources/MathematicianListItem.prefab",
            "Assets/Prefabs/UI/FilterButton.prefab",
            "Assets/Prefabs/UI/LetterButton.prefab"
        };

        [MenuItem("PeopleOfMath/Fix Theme Bindings")]
        public static void FixThemeBindings()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.isLoaded || scene.path != MainScenePath)
            {
                scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("ThemeBindingFixer: Canvas not found in Main scene.");
                return;
            }

            var fixedCount = 0;
            foreach (var binding in canvas.GetComponentsInChildren<UiThemeBinding>(true))
            {
                if (binding.GetComponent<TMP_Text>() != null && binding.GetComponentInParent<UiThemedCard>() != null)
                {
                    Object.DestroyImmediate(binding);
                    fixedCount++;
                    continue;
                }

                if (!ThemeBindingResolver.TryResolveToken(binding.gameObject, out var token))
                    continue;

                if (binding.Token == token)
                    continue;

                var so = new SerializedObject(binding);
                so.FindProperty("token").enumValueIndex = (int)token;
                so.ApplyModifiedPropertiesWithoutUndo();
                fixedCount++;
            }

            var prefabFixedCount = 0;
            foreach (var path in CardPrefabPaths)
                prefabFixedCount += PatchCardPrefab(path);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"ThemeBindingFixer: updated {fixedCount} binding(s) in {MainScenePath}, " +
                $"patched {prefabFixedCount} prefab change(s).");
        }

        static int PatchCardPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                return 0;

            var root = PrefabUtility.LoadPrefabContents(path);
            var changes = 0;

            EnsureGlassSurfaceOnCardFill(root);
            changes++;

            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.GetComponentInParent<UiThemedCard>() == null)
                    continue;

                var binding = text.GetComponent<UiThemeBinding>();
                if (binding == null)
                    continue;

                Object.DestroyImmediate(binding);
                changes++;
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            return changes;
        }

        static void EnsureGlassSurfaceOnCardFill(GameObject root)
        {
            var fill = root.transform.Find("Fill");
            if (fill == null)
                return;

            EnsureGlassSurface(fill.gameObject, UiThemeToken.CardFill);
        }

        static void EnsureGlassSurface(GameObject go, UiThemeToken tintToken)
        {
            if (go.GetComponent<Image>() == null)
                return;

            var surface = go.GetComponent<UiGlassSurface>() ?? go.AddComponent<UiGlassSurface>();
            var so = new SerializedObject(surface);
            so.FindProperty("targetImage").objectReferenceValue = go.GetComponent<Image>();
            so.FindProperty("useFrostedMaterial").boolValue = true;
            so.FindProperty("tintToken").enumValueIndex = (int)tintToken;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static class ThemeBindingResolver
    {
        internal static bool TryResolveToken(GameObject go, out UiThemeToken token)
        {
            var name = go.name;

            if (name == "DecorGlow")
            {
                token = UiThemeToken.Glow;
                return true;
            }

            if (name == "TopGlow")
            {
                token = UiThemeToken.NavBarAccent;
                return true;
            }

            if (name is "Header" or "BottomBar")
            {
                token = UiThemeToken.NavBar;
                return true;
            }

            if (name == "ContentArea")
            {
                token = UiThemeToken.Background;
                return true;
            }

            if (name == "DotTemplate")
            {
                token = UiThemeToken.GalleryDotInactive;
                return true;
            }

            if (name.EndsWith("Panel") && go.GetComponent<Image>() != null)
            {
                token = UiThemeToken.Background;
                return true;
            }

            var scroll = go.GetComponentInParent<ScrollRect>();
            if (scroll != null)
            {
                if (scroll.viewport != null && scroll.viewport.gameObject == go)
                {
                    token = UiThemeToken.ViewportMask;
                    return true;
                }

                if (scroll.gameObject == go && go.GetComponent<Image>() != null)
                {
                    token = UiThemeToken.ScrollBackground;
                    return true;
                }

                if (scroll.content != null && scroll.content.gameObject == go)
                {
                    token = UiThemeToken.Background;
                    return true;
                }
            }

            if (go.GetComponent<TMP_Text>() != null)
                return TryResolveTextToken(go, out token);

            token = default;
            return false;
        }

        static bool TryResolveTextToken(GameObject go, out UiThemeToken token)
        {
            var name = go.name;
            var onCard = go.GetComponentInParent<UiThemedCard>() != null;

            if (name is "Status" or "FontStatus" or "ThemeStatus" or "Caption" or "Empty")
            {
                token = UiThemeToken.TextSecondary;
                return true;
            }

            if (name is "Name" or "Label")
            {
                token = onCard ? UiThemeToken.CardTextPrimary : UiThemeToken.TextPrimary;
                return true;
            }

            if (name is "Dates" or "Bio" or "Body")
            {
                token = onCard ? UiThemeToken.CardTextSecondary : UiThemeToken.TextSecondary;
                return true;
            }

            if (name is "LangLabel" or "FontSizeLabel" or "ThemeLabel" or "ResetLabel"
                || name.StartsWith("section_")
                || name is "HomeTitle" or "IndexTitle" or "SettingsTitle"
                    or "FavoritesTitle" or "PlainTitle")
            {
                token = UiThemeToken.TextPrimary;
                return true;
            }

            token = default;
            return false;
        }
    }
}
