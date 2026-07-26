using PeopleOfMath.Core;
using PeopleOfMath.Localization;
using PeopleOfMath.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PeopleOfMath.Input
{
    public class BackButtonHandler : MonoBehaviour
    {
        [SerializeField] NavigationController navigation;

        const float ExitConfirmWindowSeconds = 2f;

        int _lastBackFrame = -1;
        float _lastExitPromptTime = -10f;

        void Awake()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Keyboard.current == null)
                InputSystem.AddDevice<Keyboard>();
#endif
        }

        void Update()
        {
            if (navigation == null)
                return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                OnBackPressed();
        }

        void OnBackPressed()
        {
            if (navigation == null || _lastBackFrame == Time.frameCount)
                return;

            _lastBackFrame = Time.frameCount;

            var confirm = FindFirstObjectByType<ConfirmDialogOverlay>();
            if (confirm != null)
            {
                Destroy(confirm.gameObject);
                return;
            }

            switch (navigation.CurrentScreen)
            {
                case AppScreen.Home:
                    if (Time.unscaledTime - _lastExitPromptTime <= ExitConfirmWindowSeconds)
                    {
                        Application.Quit();
                        return;
                    }

                    _lastExitPromptTime = Time.unscaledTime;
                    UiToastView.Show(UiStrings.Get("exit_press_again"));
                    break;
                case AppScreen.Settings:
                case AppScreen.Index:
                case AppScreen.About:
                    navigation.ShowHome();
                    break;
                default:
                    navigation.HandleBack();
                    break;
            }
        }
    }
}
