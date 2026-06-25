using UnityEngine;
using UnityEngine.Localization.Settings;

namespace PeopleOfMath.Sharing
{
    public static class NativeShare
    {
        public static void ShareText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var chooserTitle = LocalizationSettings.StringDatabase.GetLocalizedString(
                "UI", "share_chooser_title");

#if UNITY_ANDROID && !UNITY_EDITOR
            ShareAndroid(text, chooserTitle);
#else
            GUIUtility.systemCopyBuffer = text;
            Debug.Log($"[Share] Copied to clipboard:\n{text}");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static void ShareAndroid(string text, string chooserTitle)
        {
            try
            {
                using var intentClass = new AndroidJavaClass("android.content.Intent");
                using var intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                intent.Call<AndroidJavaObject>("setType", "text/plain");
                intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);

                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, chooserTitle);
                activity.Call("startActivity", chooser);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }
#endif
    }
}
