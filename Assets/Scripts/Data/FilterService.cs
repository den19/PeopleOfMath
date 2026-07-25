using System.Collections.Generic;
using System.Linq;

namespace PeopleOfMath.Data
{
    public static class FilterService
    {
        public static List<MathematicianData> Filter(
            IEnumerable<MathematicianData> source,
            FilterKind kind,
            string key,
            bool english)
        {
            return source
                .Where(m => Matches(m, kind, key))
                .OrderBy(m => m.GetFullName(english))
                .ToList();
        }

        public static int Count(
            IEnumerable<MathematicianData> source,
            FilterKind kind,
            string key)
        {
            if (source == null)
                return 0;

            var total = 0;
            foreach (var m in source)
            {
                if (Matches(m, kind, key))
                    total++;
            }

            return total;
        }

        static bool Matches(MathematicianData data, FilterKind kind, string key)
        {
            return kind switch
            {
                FilterKind.Century => data.centuryKeys.Contains(key),
                FilterKind.Country => data.countryKeys.Contains(key),
                FilterKind.Branch => data.branchKeys.Contains(key),
                _ => false
            };
        }
    }
}
