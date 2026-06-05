using System.Collections.Generic;
using System.IO;
using System.Linq;
using PeopleOfMath.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PeopleOfMath.Editor
{
    public static class MathematicianRepositoryRefresh
    {
        const string DataFolder = "Assets/Data/Mathematicians";
        const string CatalogPath = "Assets/Resources/MathematicianCatalog.asset";

        [MenuItem("PeopleOfMath/Refresh Repository List")]
        public static void RefreshRepositoryMenu() => RefreshAllInOpenScene();

        public static List<MathematicianData> LoadAllFromFolder(string folder = DataFolder)
        {
            var guids = AssetDatabase.FindAssets("t:MathematicianData", new[] { folder });
            var list = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<MathematicianData>)
                .Where(m => m != null)
                .OrderBy(m => m.fullNameRu ?? m.id)
                .ToList();
            return list;
        }

        public static void RefreshAllInOpenScene()
        {
            var list = LoadAllFromFolder();
            var repo = Object.FindAnyObjectByType<MathematicianRepository>();
            if (repo == null)
            {
                Debug.LogWarning("MathematicianRepository not found in open scene.");
                UpdateResourcesCatalog(list);
                return;
            }

            AssignToRepository(repo, list);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"Repository refreshed with {list.Count} mathematicians.");
        }

        public static void AssignToRepository(MathematicianRepository repository, List<MathematicianData> list)
        {
            if (repository == null)
                return;

            var filtered = list.Where(m => m != null).ToList();
            var so = new SerializedObject(repository);
            var prop = so.FindProperty("mathematicians");
            if (prop == null)
            {
                Debug.LogError("MathematicianRepository: missing mathematicians serialized property.");
                UpdateResourcesCatalog(filtered);
                return;
            }

            prop.arraySize = filtered.Count;
            for (var i = 0; i < filtered.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = filtered[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            repository.SetMathematicians(filtered);
            EditorUtility.SetDirty(repository);
            if (repository.gameObject != null)
                EditorUtility.SetDirty(repository.gameObject);

            UpdateResourcesCatalog(filtered);
        }

        public static void UpdateResourcesCatalog(List<MathematicianData> list)
        {
            var resourcesFolder = "Assets/Resources";
            if (!Directory.Exists(resourcesFolder))
                Directory.CreateDirectory(resourcesFolder);

            var catalog = AssetDatabase.LoadAssetAtPath<MathematicianCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MathematicianCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.mathematicians = list.Where(m => m != null).ToList();
            EditorUtility.SetDirty(catalog);
        }
    }
}
