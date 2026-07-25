using System;
using System.Collections.Generic;

namespace PeopleOfMath.Data
{
    public static class SearchService
    {
        enum MatchRank
        {
            None = 0,
            Branch = 1,
            Achievements = 2,
            ShortBio = 3,
            Name = 4
        }

        public static List<MathematicianData> Search(
            IEnumerable<MathematicianData> source,
            string query,
            bool english)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<MathematicianData>();

            var q = query.Trim();
            var comparison = StringComparison.CurrentCultureIgnoreCase;
            var matches = new List<(MathematicianData data, MatchRank rank)>();

            foreach (var m in source)
            {
                var rank = GetMatchRank(m, q, comparison);
                if (rank != MatchRank.None)
                    matches.Add((m, rank));
            }

            matches.Sort((a, b) =>
            {
                var byRank = ((int)b.rank).CompareTo((int)a.rank);
                if (byRank != 0)
                    return byRank;

                return string.Compare(
                    a.data.GetFullName(english),
                    b.data.GetFullName(english),
                    StringComparison.CurrentCultureIgnoreCase);
            });

            var results = new List<MathematicianData>(matches.Count);
            for (var i = 0; i < matches.Count; i++)
                results.Add(matches[i].data);
            return results;
        }

        static MatchRank GetMatchRank(MathematicianData data, string query, StringComparison comparison)
        {
            if (MatchesName(data, query, comparison))
                return MatchRank.Name;
            if (MatchesField(data.shortBioRu, data.shortBioEn, query, comparison))
                return MatchRank.ShortBio;
            if (MatchesField(data.achievementsRu, data.achievementsEn, query, comparison))
                return MatchRank.Achievements;
            if (MatchesBranches(data, query, comparison))
                return MatchRank.Branch;
            return MatchRank.None;
        }

        static bool MatchesName(MathematicianData data, string query, StringComparison comparison) =>
            MatchesText(data.fullNameRu, query, comparison) ||
            MatchesText(data.fullNameEn, query, comparison);

        static bool MatchesField(string ru, string en, string query, StringComparison comparison) =>
            MatchesText(ru, query, comparison) || MatchesText(en, query, comparison);

        static bool MatchesText(string text, string query, StringComparison comparison) =>
            !string.IsNullOrEmpty(text) && text.Contains(query, comparison);

        static bool MatchesBranches(MathematicianData data, string query, StringComparison comparison)
        {
            if (data.branchKeys == null)
                return false;

            foreach (var key in data.branchKeys)
            {
                if (MatchesText(key, query, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (!Taxonomy.Branches.TryGetValue(key, out var labels))
                    continue;

                if (MatchesText(labels.Ru, query, comparison) || MatchesText(labels.En, query, comparison))
                    return true;
            }

            return false;
        }
    }
}
