using PeopleOfMath.Localization;
using UnityEngine;

namespace PeopleOfMath.UI
{
    public static class ExternalLinkOpener
    {
        public static void Open(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (!HasNetworkConnection())
            {
                UiToastView.Show(UiStrings.Get("link_offline"));
                return;
            }

            Application.OpenURL(url.Trim());
        }

        public static bool HasNetworkConnection() =>
            Application.internetReachability != NetworkReachability.NotReachable;
    }
}
