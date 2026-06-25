using System.Collections.Generic;
using System.Text;
using PeopleOfMath.Data;

namespace PeopleOfMath.Sharing
{
    public static class MathematicianShareText
    {
        public static string BuildListShare(MathematicianData data, bool english)
        {
            if (data == null)
                return "";

            var name = data.GetFullName(english);
            var url = data.GetWikipediaUrl(english);
            if (string.IsNullOrWhiteSpace(url))
                return "";

            return string.IsNullOrWhiteSpace(name) ? url.Trim() : $"{name.Trim()}\n{url.Trim()}";
        }

        public static string BuildProfileShare(MathematicianData data, bool english)
        {
            if (data == null)
                return "";

            var blocks = new List<string>();

            AppendBlock(blocks, GetIdentityTitle(english), BuildIdentityBody(data, english));
            AppendBlock(blocks, GetCountriesTitle(english), data.GetCountriesDisplay(english));
            AppendBlock(blocks, GetCenturiesTitle(english), data.GetCenturiesDisplay(english));
            AppendBlock(blocks, GetFieldsTitle(english), data.GetBranchesDisplay(english));
            AppendBlock(blocks, GetShortBioTitle(english), data.GetShortBio(english));
            AppendBlock(blocks, GetAchievementsTitle(english), data.GetAchievements(english));
            AppendBlock(blocks, GetPersonalLifeTitle(english), data.GetPersonalLife(english));
            AppendBlock(blocks, GetInterestingFactsTitle(english), data.GetInterestingFacts(english));

            var wikiUrl = data.GetWikipediaUrl(english);
            if (!string.IsNullOrWhiteSpace(wikiUrl))
                AppendBlock(blocks, GetWikipediaTitle(english), wikiUrl.Trim());

            return string.Join("\n\n", blocks);
        }

        static string BuildIdentityBody(MathematicianData data, bool english)
        {
            var name = data.GetFullName(english)?.Trim();
            var dates = data.GetLifeDatesLabel(english)?.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return dates ?? "";

            return string.IsNullOrWhiteSpace(dates) ? name : $"{name}\n{dates}";
        }

        static void AppendBlock(List<string> blocks, string title, string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return;

            blocks.Add($"{title}\n{body.Trim()}");
        }

        static string GetIdentityTitle(bool english) => english ? "Name and dates" : "Имя и даты";

        static string GetCountriesTitle(bool english) => english ? "Countries" : "Страны";

        static string GetCenturiesTitle(bool english) => english ? "Centuries" : "Века";

        static string GetFieldsTitle(bool english) => english ? "Fields" : "Разделы";

        static string GetShortBioTitle(bool english) => english ? "Short bio" : "Краткая биография";

        static string GetAchievementsTitle(bool english) =>
            english ? "Achievements and contributions" : "Достижения и вклад";

        static string GetPersonalLifeTitle(bool english) => english ? "Personal life" : "Личная жизнь";

        static string GetInterestingFactsTitle(bool english) =>
            english ? "Interesting facts" : "Интересные факты";

        static string GetWikipediaTitle(bool english) => english ? "Wikipedia" : "Википедия";
    }
}
