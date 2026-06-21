using System.Text;
using System.Text.RegularExpressions;

namespace PeopleOfMath.Text
{
    public static class MarkdownToTmp
    {
        static readonly Regex HeaderBreak = new(@"\s+(#{3}\s+)", RegexOptions.Compiled);
        static readonly Regex BulletBreak = new(@"\s+(\*\s+\*\*)", RegexOptions.Compiled);
        static readonly Regex NumberedBreak = new(@"\s+(\d+\.\s+\*\*)", RegexOptions.Compiled);

        public static string Convert(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return "";

            var normalized = Preprocess(markdown);
            var lines = normalized.Split('\n');
            var result = new StringBuilder(normalized.Length + 64);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd();
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (result.Length > 0)
                        result.Append("<br><br>");
                    continue;
                }

                if (result.Length > 0 && !EndsWithBreak(result))
                    result.Append("<br>");

                AppendLine(result, line);
            }

            return result.ToString();
        }

        static string Preprocess(string text)
        {
            text = text.Replace("/n/n", "\n\n").Replace("/n", "\n");
            text = HeaderBreak.Replace(text, "\n$1");
            text = BulletBreak.Replace(text, "\n$1");
            text = NumberedBreak.Replace(text, "\n$1");
            return text;
        }

        static void AppendLine(StringBuilder result, string line)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("### "))
            {
                result.Append("<size=115%><b>")
                    .Append(FormatInline(trimmed.Substring(4)))
                    .Append("</b></size>");
                return;
            }

            if (TryParseBullet(trimmed, out var bulletBody))
            {
                result.Append("<indent=1em>• ")
                    .Append(FormatInline(bulletBody))
                    .Append("</indent>");
                return;
            }

            if (TryParseNumbered(trimmed, out var number, out var numberedBody))
            {
                result.Append("<indent=1em>")
                    .Append(number)
                    .Append(". ")
                    .Append(FormatInline(numberedBody))
                    .Append("</indent>");
                return;
            }

            result.Append(FormatInline(trimmed));
        }

        static bool TryParseBullet(string line, out string body)
        {
            body = "";
            if (line.Length < 2 || line[0] != '*')
                return false;

            var i = 1;
            while (i < line.Length && line[i] == ' ')
                i++;

            if (i >= line.Length)
                return false;

            body = line.Substring(i);
            return true;
        }

        static bool TryParseNumbered(string line, out string number, out string body)
        {
            number = "";
            body = "";
            var dotIndex = 0;
            while (dotIndex < line.Length && char.IsDigit(line[dotIndex]))
                dotIndex++;

            if (dotIndex == 0 || dotIndex >= line.Length || line[dotIndex] != '.')
                return false;

            var i = dotIndex + 1;
            while (i < line.Length && line[i] == ' ')
                i++;

            if (i >= line.Length)
                return false;

            number = line.Substring(0, dotIndex);
            body = line.Substring(i);
            return true;
        }

        static string FormatInline(string text)
        {
            var sb = new StringBuilder(text.Length + 32);
            var i = 0;
            while (i < text.Length)
            {
                if (i + 1 < text.Length && text[i] == '*' && text[i + 1] == '*')
                {
                    var end = text.IndexOf("**", i + 2, System.StringComparison.Ordinal);
                    if (end >= 0)
                    {
                        sb.Append("<b>");
                        AppendEscaped(sb, text, i + 2, end);
                        sb.Append("</b>");
                        i = end + 2;
                        continue;
                    }
                }

                if (text[i] == '*' && (i + 1 >= text.Length || text[i + 1] != '*'))
                {
                    var end = text.IndexOf('*', i + 1);
                    if (end > i + 1 && (end + 1 >= text.Length || text[end + 1] != '*'))
                    {
                        sb.Append("<i>");
                        AppendEscaped(sb, text, i + 1, end);
                        sb.Append("</i>");
                        i = end + 1;
                        continue;
                    }
                }

                if (text[i] == '_' && (i + 1 >= text.Length || text[i + 1] != '_'))
                {
                    var end = text.IndexOf('_', i + 1);
                    if (end > i + 1)
                    {
                        sb.Append("<i>");
                        AppendEscaped(sb, text, i + 1, end);
                        sb.Append("</i>");
                        i = end + 1;
                        continue;
                    }
                }

                AppendEscapedChar(sb, text[i]);
                i++;
            }

            return sb.ToString();
        }

        static void AppendEscaped(StringBuilder sb, string text, int start, int endExclusive)
        {
            for (var i = start; i < endExclusive; i++)
                AppendEscapedChar(sb, text[i]);
        }

        static void AppendEscapedChar(StringBuilder sb, char c)
        {
            switch (c)
            {
                case '&':
                    sb.Append("&amp;");
                    break;
                case '<':
                    sb.Append("&lt;");
                    break;
                case '>':
                    sb.Append("&gt;");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        static bool EndsWithBreak(StringBuilder sb)
        {
            var s = sb.ToString();
            return s.EndsWith("<br><br>") || s.EndsWith("<br>");
        }
    }
}
