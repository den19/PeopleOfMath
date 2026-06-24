using UnityEngine;

namespace PeopleOfMath.UI
{
    static class UiSprites
    {
        const string Folder = "UI";

        static Sprite _roundedRect;
        static Sprite _buttonGradient;

        public static Sprite RoundedRect => _roundedRect ??= Resources.Load<Sprite>($"{Folder}/RoundedRect");

        public static Sprite ButtonGradient => _buttonGradient ??= Resources.Load<Sprite>($"{Folder}/ButtonGradient");
    }
}
