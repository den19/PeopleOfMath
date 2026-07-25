using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PeopleOfMath.Data
{
    /// <summary>
    /// Strips chat/markdown chrome so biography text reads like a book.
    /// Keeps emojis, paragraph breaks, and typographic bullets; removes hashes, bold markers, HTML debris.
    /// Lines that start with an emoji are visually separated by blank lines.
    /// </summary>
    public static class EditorialText
    {
        static readonly Regex HtmlEntity = new(
            @"&(?:amp|lt|gt|quot|apos|nbsp);",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static readonly Regex BrokenGt = new(
            @"(?:(?<=\s)|(?<=\()|^)gt;(?=[\s).,]|$)",
            RegexOptions.Compiled);

        static readonly Regex MarkdownHeading = new(
            @"(?m)^\s*#{1,6}\s*",
            RegexOptions.Compiled);

        static readonly Regex InlineHashes = new(
            @"#{2,6}\s*",
            RegexOptions.Compiled);

        static readonly Regex BoldStars = new(
            @"\*\*(.+?)\*\*",
            RegexOptions.Singleline | RegexOptions.Compiled);

        static readonly Regex BoldUnderscores = new(
            @"__(.+?)__",
            RegexOptions.Singleline | RegexOptions.Compiled);

        static readonly Regex ItalicStars = new(
            @"(?<!\*)\*(?!\*)([^*\n]+?)(?<!\*)\*(?!\*)",
            RegexOptions.Compiled);

        static readonly Regex MarkdownBullet = new(
            @"(?m)^\s*[\*\-]\s+",
            RegexOptions.Compiled);

        static readonly Regex OrphanStar = new(
            @"(?<![\w*])\*(?![\w*])",
            RegexOptions.Compiled);

        static readonly Regex ExcessSpaces = new(@"[ \t]{2,}", RegexOptions.Compiled);
        static readonly Regex ExcessNewlines = new(@"\n{3,}", RegexOptions.Compiled);
        static readonly Regex SpaceBeforePunct = new(@" +([,.;:!?»])", RegexOptions.Compiled);

        public static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var text = UnicodeText.Normalize(value);
            text = DecodeHtmlEntities(text);
            text = BrokenGt.Replace(text, ">");
            text = MarkdownHeading.Replace(text, "");
            text = InlineHashes.Replace(text, "");
            text = BoldStars.Replace(text, "$1");
            text = BoldUnderscores.Replace(text, "$1");
            text = ItalicStars.Replace(text, "$1");
            text = MarkdownBullet.Replace(text, "• ");
            text = OrphanStar.Replace(text, "");
            text = ExcessSpaces.Replace(text, " ");
            text = Regex.Replace(text, @" *\n *", "\n");
            text = ExcessNewlines.Replace(text, "\n\n");
            text = SpaceBeforePunct.Replace(text, "$1");
            text = SeparateEmojiHeadingLines(text);
            text = ExcessNewlines.Replace(text, "\n\n");
            return text.Trim();
        }

        static string SeparateEmojiHeadingLines(string text)
        {
            var sourceLines = text.Replace("\r\n", "\n").Split('\n');
            var expanded = new List<string>();
            foreach (var line in sourceLines)
                expanded.AddRange(SplitInlineEmojiSegments(line));

            var result = new List<string>(expanded.Count + 8);
            for (var i = 0; i < expanded.Count; i++)
            {
                var line = expanded[i];
                if (!StartsWithEmoji(line))
                {
                    result.Add(line);
                    continue;
                }

                if (result.Count > 0 && result[^1].Length > 0)
                    result.Add("");

                result.Add(line.TrimEnd());

                if (i + 1 < expanded.Count && expanded[i + 1].Length > 0)
                    result.Add("");
            }

            return string.Join("\n", result);
        }

        static IEnumerable<string> SplitInlineEmojiSegments(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                yield return line;
                yield break;
            }

            var start = 0;
            var i = 0;
            while (i < line.Length)
            {
                var code = ReadCodePoint(line, i, out var len);
                if (i > start && IsEmojiCodePoint(code) && char.IsWhiteSpace(line[i - 1]))
                {
                    var breakAt = i;
                    while (breakAt > start && line[breakAt - 1] == ' ')
                        breakAt--;

                    if (breakAt > start)
                    {
                        yield return line.Substring(start, breakAt - start);
                        start = i;
                    }
                }

                i += len;
            }

            yield return line.Substring(start);
        }

        static bool StartsWithEmoji(string line)
        {
            if (string.IsNullOrEmpty(line))
                return false;

            var i = 0;
            while (i < line.Length && char.IsWhiteSpace(line[i]))
                i++;

            if (i >= line.Length)
                return false;

            return IsEmojiCodePoint(ReadCodePoint(line, i, out _));
        }

        static int ReadCodePoint(string text, int index, out int utf16Length)
        {
            if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
            {
                utf16Length = 2;
                return char.ConvertToUtf32(text[index], text[index + 1]);
            }

            utf16Length = 1;
            return text[index];
        }

        static bool IsEmojiCodePoint(int codePoint) =>
            codePoint is >= 0x1F300 and <= 0x1FAFF
                or >= 0x1F1E0 and <= 0x1F1FF
                or >= 0x2600 and <= 0x27BF
                or >= 0x2300 and <= 0x23FF
                or >= 0x2190 and <= 0x21FF
                or >= 0x2B00 and <= 0x2BFF;

        static string DecodeHtmlEntities(string text)
        {
            return HtmlEntity.Replace(text, m => m.Value.ToLowerInvariant() switch
            {
                "&amp;" => "&",
                "&lt;" => "<",
                "&gt;" => ">",
                "&quot;" => "\"",
                "&apos;" => "'",
                "&nbsp;" => " ",
                _ => m.Value
            });
        }

        public static bool TryClean(ref string field)
        {
            if (string.IsNullOrEmpty(field))
                return false;

            var cleaned = Clean(field);
            if (cleaned == field)
                return false;

            field = cleaned;
            return true;
        }
    }
}
