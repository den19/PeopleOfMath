using System.Globalization;
using System.Text.RegularExpressions;

namespace PeopleOfMath.Data
{
    public static class UnicodeText
    {
        static readonly Regex BrokenUnicode = new(@"\\?u([0-9a-fA-F]{4})", RegexOptions.Compiled);
        static readonly Regex BrokenHex = new(@"\\x([0-9a-fA-F]{2})", RegexOptions.Compiled);

        public static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            if (!value.Contains('\\'))
                return value;

            var decoded = BrokenHex.Replace(
                value,
                m => ((char)int.Parse(m.Groups[1].Value, NumberStyles.HexNumber)).ToString());

            return BrokenUnicode.Replace(
                decoded,
                m => ((char)int.Parse(m.Groups[1].Value, NumberStyles.HexNumber)).ToString());
        }
    }
}
