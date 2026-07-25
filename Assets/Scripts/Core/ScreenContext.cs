using PeopleOfMath.Data;

namespace PeopleOfMath.Core
{
    public enum AppScreen
    {
        Home,
        Index,
        List,
        Detail,
        Settings,
        Favorites,
        Quiz,
        About
    }

    public enum DetailOrigin
    {
        None,
        Home,
        Index,
        Favorites,
        Quiz,
        Search,
        FilterList
    }

    public struct ScreenContext
    {
        public AppScreen Screen;
        public string MathematicianId;
        public FilterKind FilterKind;
        public string FilterKey;
        public string SearchQuery;
        public bool ListFromDetail;
        public bool ListFromSearch;

        public static ScreenContext Home() => new() { Screen = AppScreen.Home };

        public static ScreenContext Index() => new() { Screen = AppScreen.Index };

        public static ScreenContext Settings() => new() { Screen = AppScreen.Settings };

        public static ScreenContext Quiz() => new() { Screen = AppScreen.Quiz };

        public static ScreenContext Favorites() => new() { Screen = AppScreen.Favorites };

        public static ScreenContext About() => new() { Screen = AppScreen.About };

        public static ScreenContext ListFilter(FilterKind kind, string key, bool fromDetail = false, string mathematicianId = null) =>
            new()
            {
                Screen = AppScreen.List,
                FilterKind = kind,
                FilterKey = key,
                ListFromDetail = fromDetail,
                MathematicianId = mathematicianId
            };

        public static ScreenContext ListSearch(string query) =>
            new()
            {
                Screen = AppScreen.List,
                SearchQuery = query,
                ListFromSearch = true
            };

        public static ScreenContext Detail(string mathematicianId) =>
            new()
            {
                Screen = AppScreen.Detail,
                MathematicianId = mathematicianId
            };
    }
}
