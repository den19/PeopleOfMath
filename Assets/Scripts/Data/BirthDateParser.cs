using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PeopleOfMath.Data
{
    public static class BirthDateParser
    {
        public static readonly int[] AnniversaryMilestones =
        {
            10, 20, 30, 40, 50, 60, 70, 80, 90,
            100, 200, 300, 400, 500, 600, 700, 800, 900, 1000
        };

        static readonly Regex DualDayMonthYear = new(
            @"^\s*(\d{1,2})\((\d{1,2})\)\.(\d{1,2})\.(\d{3,4})\s*$",
            RegexOptions.Compiled);

        static readonly Regex DayMonthYear = new(
            @"^\s*(\d{1,2})\.(\d{1,2})\.(\d{3,4})\s*$",
            RegexOptions.Compiled);

        static readonly Regex YearOnly = new(
            @"^\s*~?\s*(\d{1,4})\s*$",
            RegexOptions.Compiled);

        static readonly Regex BceYear = new(
            @"^\s*(?:ок\.?\s*|~?\s*)?(\d{1,4})\s*до\s*н\.?\s*э\.?\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static bool TryGetMonthDays(string birthDate, List<(int month, int day)> results)
        {
            results?.Clear();
            if (results == null || string.IsNullOrWhiteSpace(birthDate))
                return false;

            var trimmed = birthDate.Trim();
            var dual = DualDayMonthYear.Match(trimmed);
            if (dual.Success)
            {
                var dayA = int.Parse(dual.Groups[1].Value);
                var dayB = int.Parse(dual.Groups[2].Value);
                var month = int.Parse(dual.Groups[3].Value);
                AddIfValid(results, month, dayA);
                AddIfValid(results, month, dayB);
                return results.Count > 0;
            }

            var plain = DayMonthYear.Match(trimmed);
            if (!plain.Success)
                return false;

            var day = int.Parse(plain.Groups[1].Value);
            var mo = int.Parse(plain.Groups[2].Value);
            AddIfValid(results, mo, day);
            return results.Count > 0;
        }

        public static bool TryGetBirthYear(string birthDate, out int year)
        {
            year = 0;
            if (string.IsNullOrWhiteSpace(birthDate))
                return false;

            var trimmed = birthDate.Trim();

            var dual = DualDayMonthYear.Match(trimmed);
            if (dual.Success)
            {
                year = int.Parse(dual.Groups[4].Value);
                return year > 0;
            }

            var plain = DayMonthYear.Match(trimmed);
            if (plain.Success)
            {
                year = int.Parse(plain.Groups[3].Value);
                return year > 0;
            }

            var bce = BceYear.Match(trimmed);
            if (bce.Success)
            {
                var n = int.Parse(bce.Groups[1].Value);
                if (n <= 0)
                    return false;
                year = -n;
                return true;
            }

            var yearOnly = YearOnly.Match(trimmed);
            if (yearOnly.Success)
            {
                year = int.Parse(yearOnly.Groups[1].Value);
                return year > 0;
            }

            return false;
        }

        public static int YearsSinceBirth(int birthYear, int currentYear) => currentYear - birthYear;

        public static bool TryGetAnniversaryMilestone(int yearsSince, out int milestone)
        {
            milestone = 0;
            if (yearsSince <= 0)
                return false;

            for (var i = AnniversaryMilestones.Length - 1; i >= 0; i--)
            {
                var m = AnniversaryMilestones[i];
                if (yearsSince % m == 0)
                {
                    milestone = m;
                    return true;
                }
            }

            return false;
        }

        public static bool BornOn(MathematicianData data, int month, int day)
        {
            if (data == null)
                return false;

            var buffer = new List<(int month, int day)>(2);
            if (!TryGetMonthDays(data.birthDate, buffer))
                return false;

            for (var i = 0; i < buffer.Count; i++)
            {
                if (buffer[i].month == month && buffer[i].day == day)
                    return true;
            }

            return false;
        }

        public static List<MathematicianData> FindBornOn(
            IEnumerable<MathematicianData> source,
            int month,
            int day)
        {
            var matches = new List<MathematicianData>();
            if (source == null)
                return matches;

            var buffer = new List<(int month, int day)>(2);
            foreach (var data in source)
            {
                if (data == null)
                    continue;

                if (!TryGetMonthDays(data.birthDate, buffer))
                    continue;

                for (var i = 0; i < buffer.Count; i++)
                {
                    if (buffer[i].month == month && buffer[i].day == day)
                    {
                        matches.Add(data);
                        break;
                    }
                }
            }

            return matches;
        }

        public static List<(MathematicianData data, int yearsSince, int milestone)> FindAnniversaries(
            IEnumerable<MathematicianData> source,
            int currentYear)
        {
            var matches = new List<(MathematicianData data, int yearsSince, int milestone)>();
            if (source == null)
                return matches;

            foreach (var data in source)
            {
                if (data == null)
                    continue;

                if (!TryGetBirthYear(data.birthDate, out var birthYear))
                    continue;

                var yearsSince = YearsSinceBirth(birthYear, currentYear);
                if (!TryGetAnniversaryMilestone(yearsSince, out var milestone))
                    continue;

                matches.Add((data, yearsSince, milestone));
            }

            matches.Sort((a, b) =>
            {
                var byMilestone = b.milestone.CompareTo(a.milestone);
                if (byMilestone != 0)
                    return byMilestone;
                return b.yearsSince.CompareTo(a.yearsSince);
            });

            return matches;
        }

        public static HashSet<int> BirthdayDaysInMonth(IEnumerable<MathematicianData> source, int month)
        {
            var days = new HashSet<int>();
            if (source == null || month < 1 || month > 12)
                return days;

            var buffer = new List<(int month, int day)>(2);
            foreach (var data in source)
            {
                if (data == null)
                    continue;

                if (!TryGetMonthDays(data.birthDate, buffer))
                    continue;

                for (var i = 0; i < buffer.Count; i++)
                {
                    if (buffer[i].month == month)
                        days.Add(buffer[i].day);
                }
            }

            return days;
        }

        static void AddIfValid(List<(int month, int day)> results, int month, int day)
        {
            if (month < 1 || month > 12 || day < 1 || day > 31)
                return;

            for (var i = 0; i < results.Count; i++)
            {
                if (results[i].month == month && results[i].day == day)
                    return;
            }

            results.Add((month, day));
        }
    }
}
