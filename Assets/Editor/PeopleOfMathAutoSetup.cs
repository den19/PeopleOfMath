using System.IO;
using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    [InitializeOnLoad]
    static class PeopleOfMathAutoSetup
    {
        internal const string ScenePath = "Assets/Scenes/Main.unity";
        internal const string SessionKey = "PeopleOfMath_AutoSetupDone";

        public static void ResetSession() => SessionState.EraseBool(SessionKey);

        static PeopleOfMathAutoSetup()
        {
            EditorApplication.delayCall += TryRun;
        }

        static void TryRun()
        {
            if (File.Exists(ScenePath))
                return;
            if (SessionState.GetBool(SessionKey, false))
                return;
            if (EditorApplication.isPlaying)
            {
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryRun;
                return;
            }

            try
            {
                PeopleOfMathProjectSetup.Run();
                if (File.Exists(ScenePath))
                    SessionState.SetBool(SessionKey, true);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"PeopleOfMath auto setup failed: {ex}");
            }
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.delayCall += TryRun;
        }
    }
}
