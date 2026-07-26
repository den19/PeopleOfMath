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

        public static Dictionary<string, int> CountAll(
            IEnumerable<MathematicianData> source,
            FilterKind kind)
        {
            var counts = new Dictionary<string, int>();
            if (source == null)
                return counts;

            foreach (var data in source)
            {
                var keys = GetKeys(data, kind);
                if (keys == null)
                    continue;

                foreach (var key in keys)
                {
                    if (string.IsNullOrEmpty(key))
                        continue;

                    counts.TryGetValue(key, out var total);
                    counts[key] = total + 1;
                }
            }

            return counts;
        }

        static bool Matches(MathematicianData data, FilterKind kind, string key)
        {
            var keys = GetKeys(data, kind);
            return keys != null && keys.Contains(key);
        }

        static IList<string> GetKeys(MathematicianData data, FilterKind kind)
        {
            if (data == null)
                return null;

            return kind switch
            {
                FilterKind.Century => data.centuryKeys,
                FilterKind.Country => data.countryKeys,
                FilterKind.Branch => data.branchKeys,
                _ => null
            };
        }
    }
}
