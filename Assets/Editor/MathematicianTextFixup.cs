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

                if (FixUnicodeField(ref data.fullNameRu) |
                    FixUnicodeField(ref data.shortBioRu) |
                    FixUnicodeField(ref data.achievementsRu) |
                    FixUnicodeField(ref data.personalLifeRu) |
                    FixUnicodeField(ref data.interestingFactsRu))
                {
                    EditorUtility.SetDirty(data);
                    fixedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Unicode text fixed on {fixedCount} mathematician assets.");
        }

        [MenuItem("PeopleOfMath/Proofread Mathematician Cards")]
        public static void ProofreadAllAssets()
        {
            var guids = AssetDatabase.FindAssets("t:MathematicianData", new[] { DataFolder });
            var fixedCount = 0;

            foreach (var guid in guids)
            {
                var data = AssetDatabase.LoadAssetAtPath<MathematicianData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (data == null)
                    continue;

                var changed =
                    EditorialText.TryClean(ref data.fullNameRu) |
                    EditorialText.TryClean(ref data.fullNameEn) |
                    EditorialText.TryClean(ref data.shortBioRu) |
                    EditorialText.TryClean(ref data.shortBioEn) |
                    EditorialText.TryClean(ref data.achievementsRu) |
                    EditorialText.TryClean(ref data.achievementsEn) |
                    EditorialText.TryClean(ref data.personalLifeRu) |
                    EditorialText.TryClean(ref data.personalLifeEn) |
                    EditorialText.TryClean(ref data.interestingFactsRu) |
                    EditorialText.TryClean(ref data.interestingFactsEn);

                if (!changed)
                    continue;

                EditorUtility.SetDirty(data);
                fixedCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Editorial proofread cleaned {fixedCount} mathematician cards.");
        }

        static bool FixUnicodeField(ref string field)
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
