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
