using System.Collections;
using PeopleOfMath.Localization;
using PeopleOfMath.UI;
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
            FontSizeHelper.Initialize();
            ThemeHelper.Initialize();
            navigation?.ShowHome();
        }
    }
}
