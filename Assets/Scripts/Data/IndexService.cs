using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PeopleOfMath.Data
{
    public static class IndexService
    {
        public const char OtherLetter = '#';

        static readonly char[] LatinAlphabet =
        {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
            'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'
        };

        static readonly char[] CyrillicAlphabet =
        {
            'А', 'Б', 'В', 'Г', 'Д', 'Е', 'Ё', 'Ж', 'З', 'И', 'Й', 'К', 'Л', 'М',
            'Н', 'О', 'П', 'Р', 'С', 'Т', 'У', 'Ф', 'Х', 'Ц', 'Ч', 'Ш', 'Щ', 'Ъ',
            'Ы', 'Ь', 'Э', 'Ю', 'Я'
        };

        public static IReadOnlyList<char> GetAlphabet(bool english) =>
            english ? LatinAlphabet : CyrillicAlphabet;

        public static bool IsStripLetter(char letter, bool english) =>
            letter == OtherLetter || GetAlphabet(english).Contains(letter);

        public static char GetSortLetter(string name, bool english)
        {
            if (string.IsNullOrWhiteSpace(name))
                return OtherLetter;

            var culture = CultureInfo.CurrentCulture;
            var trimmed = name.TrimStart();
            if (trimmed.Length == 0)
                return OtherLetter;

            var first = char.ToUpper(trimmed[0], culture);
            if (!char.IsLetter(first))
                return OtherLetter;

            var alphabet = GetAlphabet(english);
            if (alphabet.Contains(first))
                return first;

            return OtherLetter;
        }

        public static HashSet<char> GetUsedLetters(IEnumerable<MathematicianData> source, bool english)
        {
            var used = new HashSet<char>();
            foreach (var data in source)
            {
                var letter = GetSortLetter(data.GetFullName(english), english);
                used.Add(letter);
            }

            return used;
        }

        public static List<MathematicianData> FilterByLetter(
            IEnumerable<MathematicianData> source,
            char letter,
            bool english)
        {
            var comparer = StringComparer.CurrentCultureIgnoreCase;
            return source
                .Where(m => GetSortLetter(m.GetFullName(english), english) == letter)
                .OrderBy(m => m.GetFullName(english), comparer)
                .ToList();
        }
    }
}
