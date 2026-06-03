using System.Globalization;
using System.Text.RegularExpressions;

namespace PeopleOfMath.Data
{
    public static class UnicodeText
    {
        static readonly Regex BrokenUnicode = new(@"\\?u([0-9a-fA-F]{4})", RegexOptions.Compiled);

        public static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            if (!value.Contains('u') && !value.Contains('\\'))
                return value;

            return BrokenUnicode.Replace(
                value,
                m => ((char)int.Parse(m.Groups[1].Value, NumberStyles.HexNumber)).ToString());
        }
    }
}
