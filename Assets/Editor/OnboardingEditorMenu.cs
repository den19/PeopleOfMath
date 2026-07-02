using PeopleOfMath.UI;
using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class OnboardingEditorMenu
    {
        [MenuItem("PeopleOfMath/Reset Onboarding")]
        public static void ResetOnboarding()
        {
            OnboardingOverlay.ResetProgress();
            Debug.Log("Onboarding reset. The overlay will appear on the next Play.");
        }
    }
}
