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
            var repo = Object.FindFirstObjectByType<MathematicianRepository>();
            if (repo == null)
            {
                Debug.LogWarning("MathematicianRepository not found in open scene.");
                return;
            }

            AssignToRepository(repo, list);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"Repository refreshed with {list.Count} mathematicians.");
        }

        public static void AssignToRepository(MathematicianRepository repository, List<MathematicianData> list)
        {
            var so = new SerializedObject(repository);
            var prop = so.FindProperty("mathematicians");
            prop.ClearArray();
            for (var i = 0; i < list.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            repository.SetMathematicians(list);
            EditorUtility.SetDirty(repository);
        }
    }
}
