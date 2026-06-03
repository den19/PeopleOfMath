using System.Collections;
using PeopleOfMath.Localization;
using UnityEngine;

namespace PeopleOfMath.Core
{
    public class AppBootstrap : MonoBehaviour
    {
        [SerializeField] NavigationController navigation;

        static bool _initialized;

        void Awake()
        {
            if (_initialized)
                return;
            _initialized = true;
            StartCoroutine(Bootstrap());
        }

        IEnumerator Bootstrap()
        {
            yield return LocaleHelper.InitializeLocale();
            navigation?.ShowHome();
        }
    }
}
