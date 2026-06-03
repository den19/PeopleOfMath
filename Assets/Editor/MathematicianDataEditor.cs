using PeopleOfMath.Data;
using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    [CustomEditor(typeof(MathematicianData))]
    public class MathematicianDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var data = (MathematicianData)target;
            var count = data.GetValidPortraits().Count;
            if (count < 2)
                EditorGUILayout.HelpBox(
                    $"Рекомендуется минимум 2 портрета (сейчас {count}). Запустите PeopleOfMath → Import Portraits (Wikimedia).",
                    MessageType.Warning);
        }
    }
}
