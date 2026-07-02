using System;
using PeopleOfMath.Core;
using PeopleOfMath.Localization;
using PeopleOfMath.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

namespace PeopleOfMath.Input
{
    public class BackButtonHandler : MonoBehaviour
    {
        [SerializeField] NavigationController navigation;

        const float ExitConfirmWindowSeconds = 2f;

        int _lastBackFrame = -1;
        float _lastExitPromptTime = -10f;
        IDisposable _buttonPressSubscription;

        void Awake()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Keyboard.current == null)
                InputSystem.AddDevice<Keyboard>();
#endif
        }

        void OnEnable() =>
            _buttonPressSubscription = InputSystem.onAnyButtonPress.Call(OnButtonPressed);

        void OnDisable()
        {
            _buttonPressSubscription?.Dispose();
            _buttonPressSubscription = null;
        }

        void Update()
        {
            if (navigation == null)
                return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                OnBackPressed();
        }

        void OnButtonPressed(InputControl control)
        {
            if (control is KeyControl { keyCode: Key.Escape })
                OnBackPressed();
        }

        void OnBackPressed()
        {
            if (navigation == null || _lastBackFrame == Time.frameCount)
                return;

            _lastBackFrame = Time.frameCount;

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
                    navigation.ShowHome();
                    break;
                default:
                    navigation.HandleBack();
                    break;
            }
        }
    }
}
