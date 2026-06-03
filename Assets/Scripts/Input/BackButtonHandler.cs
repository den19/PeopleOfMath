using PeopleOfMath.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PeopleOfMath.Input
{
    public class BackButtonHandler : MonoBehaviour
    {
        [SerializeField] NavigationController navigation;

        void Update()
        {
            if (navigation == null)
                return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                navigation.HandleBack();
        }
    }
}
