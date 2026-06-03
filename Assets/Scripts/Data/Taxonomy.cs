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
            ["uk"] = new("Великобритания", "United Kingdom"),
            ["switzerland"] = new("Швейцария", "Switzerland"),
            ["russia"] = new("Россия", "Russia"),
            ["germany"] = new("Германия", "Germany"),
            ["france"] = new("Франция", "France"),
            ["italy"] = new("Италия", "Italy"),
            ["usa"] = new("США", "United States"),
            ["poland"] = new("Польша", "Poland"),
            ["india"] = new("Индия", "India"),
            ["china"] = new("Китай", "China"),
            ["austria"] = new("Австрия", "Austria"),
            ["netherlands"] = new("Нидерланды", "Netherlands"),
            ["norway"] = new("Норвегия", "Norway"),
            ["sweden"] = new("Швеция", "Sweden"),
            ["hungary"] = new("Венгрия", "Hungary"),
            ["czech"] = new("Чехия", "Czech Republic"),
            ["belgium"] = new("Бельгия", "Belgium"),
            ["scotland"] = new("Шотландия", "Scotland"),
            ["ireland"] = new("Ирландия", "Ireland"),
            ["iran"] = new("Иран", "Iran"),
            ["egypt"] = new("Египет", "Egypt"),
            ["japan"] = new("Япония", "Japan"),
            ["brazil"] = new("Бразилия", "Brazil"),
            ["canada"] = new("Канада", "Canada"),
            ["spain"] = new("Испания", "Spain"),
            ["portugal"] = new("Португалия", "Portugal"),
            ["ukraine"] = new("Украина", "Ukraine"),
            ["israel"] = new("Израиль", "Israel"),
            ["turkey"] = new("Турция", "Turkey"),
        };

        public static readonly Dictionary<string, LabelPair> Centuries = new()
        {
            ["6bc"] = new("VI в. до н.э.", "6th century BC"),
            ["5bc"] = new("V в. до н.э.", "5th century BC"),
            ["4bc"] = new("IV в. до н.э.", "4th century BC"),
            ["3bc"] = new("III в. до н.э.", "3rd century BC"),
            ["2bc"] = new("II в. до н.э.", "2nd century BC"),
            ["1bc"] = new("I в. до н.э.", "1st century BC"),
            ["1"] = new("I век", "1st century"),
            ["2"] = new("II век", "2nd century"),
            ["3"] = new("III век", "3rd century"),
            ["4"] = new("IV век", "4th century"),
            ["5"] = new("V век", "5th century"),
            ["6"] = new("VI век", "6th century"),
            ["7"] = new("VII век", "7th century"),
            ["8"] = new("VIII век", "8th century"),
            ["9"] = new("IX век", "9th century"),
            ["10"] = new("X век", "10th century"),
            ["11"] = new("XI век", "11th century"),
            ["12"] = new("XII век", "12th century"),
            ["13"] = new("XIII век", "13th century"),
            ["14"] = new("XIV век", "14th century"),
            ["15"] = new("XV век", "15th century"),
            ["16"] = new("XVI век", "16th century"),
            ["17"] = new("XVII век", "17th century"),
            ["18"] = new("XVIII век", "18th century"),
            ["19"] = new("XIX век", "19th century"),
            ["20"] = new("XX век", "20th century"),
            ["21"] = new("XXI век", "21st century"),
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
            ["algebra"] = new("Алгебра", "Algebra"),
            ["probability"] = new("Теория вероятностей", "Probability"),
            ["statistics"] = new("Статистика", "Statistics"),
            ["combinatorics"] = new("Комбинаторика", "Combinatorics"),
            ["applied_math"] = new("Прикладная математика", "Applied mathematics"),
            ["set_theory"] = new("Теория множеств", "Set theory"),
            ["mathematical_physics"] = new("Математическая физика", "Mathematical physics"),
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

        public static IReadOnlyList<string> AllCountryKeys => Countries.Keys.OrderBy(k => k).ToList();
        public static IReadOnlyList<string> AllCenturyKeys => Centuries.Keys.ToList();
        public static IReadOnlyList<string> AllBranchKeys => Branches.Keys.OrderBy(k => k).ToList();
    }
}
