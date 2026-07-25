using UnityEngine;

namespace PeopleOfMath.UI
{
    static class UiSprites
    {
        const string Folder = "UI";

        static Sprite _roundedRect;
        static Sprite _buttonGradient;
        static Sprite _shareIcon;
        static Sprite _heartOutline;
        static Sprite _heartFilled;
        static Sprite _tabBrowse;
        static Sprite _tabIndex;
        static Sprite _tabQuiz;
        static Sprite _tabSettings;
        static Sprite _tabAbout;

        public static Sprite RoundedRect => _roundedRect ??= Resources.Load<Sprite>($"{Folder}/RoundedRect");

        public static Sprite ButtonGradient => _buttonGradient ??= Resources.Load<Sprite>($"{Folder}/ButtonGradient");

        public static Sprite ShareIcon => _shareIcon ??= Resources.Load<Sprite>($"{Folder}/ShareIcon");

        public static Sprite HeartOutline => _heartOutline ??= Resources.Load<Sprite>($"{Folder}/HeartOutline");

        public static Sprite HeartFilled => _heartFilled ??= Resources.Load<Sprite>($"{Folder}/HeartFilled");

        public static Sprite TabBrowse => _tabBrowse ??= Resources.Load<Sprite>($"{Folder}/TabBrowse");

        public static Sprite TabIndex => _tabIndex ??= Resources.Load<Sprite>($"{Folder}/TabIndex");

        public static Sprite TabQuiz => _tabQuiz ??= Resources.Load<Sprite>($"{Folder}/TabQuiz");

        public static Sprite TabSettings => _tabSettings ??= Resources.Load<Sprite>($"{Folder}/TabSettings");

        public static Sprite TabAbout => _tabAbout ??= Resources.Load<Sprite>($"{Folder}/TabAbout");

        public static Sprite GetTabIcon(NavTabId tab) => tab switch
        {
            NavTabId.Browse => TabBrowse,
            NavTabId.Index => TabIndex,
            NavTabId.Favorites => HeartOutline,
            NavTabId.Quiz => TabQuiz,
            NavTabId.Settings => TabSettings,
            NavTabId.About => TabAbout,
            _ => TabBrowse
        };
    }
}
