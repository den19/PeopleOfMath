using PeopleOfMath.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PeopleOfMath.Editor
{
    public static class GlassShaderBuildSetup
    {
        const string FrostedGlassGuid = "19442cf39d81bdd43973eb683c6be642";
        const string BackdropBlurGuid = "e5148efb3bf068045be30606ba2b8c82";

        [InitializeOnLoadMethod]
        static void EnsureOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (!Application.isBatchMode)
                    EnsureGlassShadersIncluded(logChanges: false);
            };
        }

        [MenuItem("PeopleOfMath/Ensure Glass Shaders In Build")]
        public static void EnsureGlassShadersIncludedMenu()
        {
            EnsureGlassShadersIncluded(logChanges: true);
        }

        public static void EnsureGlassShadersIncluded(bool logChanges = true)
        {
            var graphicsSettings = GraphicsSettings.GetGraphicsSettings();
            if (graphicsSettings == null)
            {
                Debug.LogWarning("GraphicsSettings asset was not found; glass shaders were not registered.");
                return;
            }

            var serialized = new SerializedObject(graphicsSettings);
            var shadersProp = serialized.FindProperty("m_AlwaysIncludedShaders");
            if (shadersProp == null)
            {
                Debug.LogWarning("m_AlwaysIncludedShaders was not found; glass shaders were not registered.");
                return;
            }

            var added = 0;
            added += EnsureShaderReference(shadersProp, FrostedGlassGuid);
            added += EnsureShaderReference(shadersProp, BackdropBlurGuid);

            if (added > 0)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                if (logChanges)
                    Debug.Log($"Added {added} glass shader(s) to Always Included Shaders for APK builds.");
            }
            else if (logChanges)
            {
                Debug.Log("Glass shaders are already included in the player build.");
            }

            EnsureResourceMaterials();
        }

        static int EnsureShaderReference(SerializedProperty shadersProp, string guid)
        {
            for (var i = 0; i < shadersProp.arraySize; i++)
            {
                var element = shadersProp.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null)
                    continue;

                var path = AssetDatabase.GetAssetPath(element.objectReferenceValue);
                if (path != null && AssetDatabase.AssetPathToGUID(path) == guid)
                    return 0;
            }

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));
            if (shader == null)
            {
                Debug.LogWarning($"Glass shader with guid {guid} was not found.");
                return 0;
            }

            shadersProp.InsertArrayElementAtIndex(shadersProp.arraySize);
            shadersProp.GetArrayElementAtIndex(shadersProp.arraySize - 1).objectReferenceValue = shader;
            return 1;
        }

        static void EnsureResourceMaterials()
        {
            EnsureMaterial("Assets/Resources/UI/UiFrostedGlass.mat", GlassThemeAssets.FrostedGlassShaderName);
            EnsureMaterial("Assets/Resources/UI/UiBackdropBlur.mat", GlassThemeAssets.BackdropBlurShaderName);
        }

        static void EnsureMaterial(string path, string shaderName)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                return;

            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"Shader {shaderName} was not found while validating {path}.");
                return;
            }

            if (material.shader == shader)
                return;

            material.shader = shader;
            EditorUtility.SetDirty(material);
        }
    }
}
