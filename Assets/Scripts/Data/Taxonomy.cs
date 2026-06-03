using System.Collections.Generic;
using System.Linq;

namespace PeopleOfMath.Data
{
    public static class Taxonomy
    {
        public readonly struct LabelPair
        {
            public readonly string Ru;
            public readonly string En;

            public LabelPair(string ru, string en)
            {
                Ru = ru;
                En = en;
            }

            public string Get(bool english) => english ? En : Ru;
        }

        public static readonly Dictionary<string, LabelPair> Countries = new()
        {
            ["greece"] = new("Греция", "Greece"),
            ["syracuse"] = new("Сиракузы", "Syracuse"),
            ["england"] = new("Англия", "England"),
            ["switzerland"] = new("Швейцария", "Switzerland"),
            ["russia"] = new("Россия", "Russia"),
            ["germany"] = new("Германия", "Germany"),
            ["france"] = new("Франция", "France"),
            ["uk"] = new("Великобритания", "United Kingdom"),
        };

        public static readonly Dictionary<string, LabelPair> Centuries = new()
        {
            ["6bc"] = new("VI в. до н.э.", "6th century BC"),
            ["5bc"] = new("V в. до н.э.", "5th century BC"),
            ["3bc"] = new("III в. до н.э.", "3rd century BC"),
            ["17"] = new("XVII век", "17th century"),
            ["18"] = new("XVIII век", "18th century"),
            ["19"] = new("XIX век", "19th century"),
            ["20"] = new("XX век", "20th century"),
        };

        public static readonly Dictionary<string, LabelPair> Branches = new()
        {
            ["geometry"] = new("Геометрия", "Geometry"),
            ["number_theory"] = new("Теория чисел", "Number theory"),
            ["mechanics"] = new("Механика", "Mechanics"),
            ["analysis"] = new("Математический анализ", "Mathematical analysis"),
            ["topology"] = new("Топология", "Topology"),
            ["dynamical_systems"] = new("Динамические системы", "Dynamical systems"),
            ["logic"] = new("Логика", "Logic"),
            ["informatics"] = new("Информатика", "Computer science"),
        };

        public static string JoinLabels(
            IEnumerable<string> keys,
            Dictionary<string, LabelPair> map,
            bool english)
        {
            var parts = keys
                .Where(k => map.ContainsKey(k))
                .Select(k => map[k].Get(english))
                .ToList();
            return parts.Count == 0 ? "—" : string.Join(", ", parts);
        }

        public static IReadOnlyList<string> AllCountryKeys => Countries.Keys.ToList();
        public static IReadOnlyList<string> AllCenturyKeys => Centuries.Keys.ToList();
        public static IReadOnlyList<string> AllBranchKeys => Branches.Keys.ToList();
    }
}
