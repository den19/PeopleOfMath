using PeopleOfMath.Data;
using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class MathematicianTextFixup
    {
        const string DataFolder = "Assets/Data/Mathematicians";

        [MenuItem("PeopleOfMath/Fix Unicode Text In Assets")]
        public static void FixAllAssets()
        {
            var guids = AssetDatabase.FindAssets("t:MathematicianData", new[] { DataFolder });
            var fixedCount = 0;

            foreach (var guid in guids)
            {
                var data = AssetDatabase.LoadAssetAtPath<MathematicianData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (data == null)
                    continue;

                if (FixField(ref data.fullNameRu) |
                    FixField(ref data.shortBioRu) |
                    FixField(ref data.achievementsRu) |
                    FixField(ref data.personalLifeRu) |
                    FixField(ref data.interestingFactsRu))
                {
                    EditorUtility.SetDirty(data);
                    fixedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Unicode text fixed on {fixedCount} mathematician assets.");
        }

        static bool FixField(ref string field)
        {
            if (string.IsNullOrEmpty(field) || !field.Contains('u'))
                return false;

            var normalized = UnicodeText.Normalize(field);
            if (normalized == field)
                return false;

            field = normalized;
            return true;
        }
    }
}
