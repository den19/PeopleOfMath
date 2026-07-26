using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PeopleOfMath.Data;
using UnityEngine;

namespace PeopleOfMath.Quiz
{
    public static class QuizPromptExtractor
    {
        const int MaxPromptLength = 900;

        static readonly char[] LineSeparators = { '\n', '\r' };
        static readonly char[] BulletPrefixes = { '•', '-', '–', '—', '*', '·' };
        static readonly char[] NameTokenSeparators = { ' ', ',' };

        static readonly HashSet<string> NameParticles = new(StringComparer.OrdinalIgnoreCase)
        {
            "van", "von", "de", "der", "den", "des", "du", "da", "di", "la", "le",
            "of", "al", "el", "bin", "ibn", "abu", "ben", "ten", "ter", "del", "della",
            "dos", "das", "y", "und"
        };

        public static bool TryGetFactPrompt(MathematicianData data, bool english, out string prompt)
        {
            prompt = null;
            if (data == null)
                return false;

            // No cross-locale fallback: English UI must not surface Russian quiz copy.
            const bool fallbackToOtherLocale = false;
            var facts = NormalizeStoredText(data.GetInterestingFacts(english, fallbackToOtherLocale));
            if (TryPickRandomLine(facts, out prompt))
            {
                prompt = RedactNameComponents(prompt, data.GetFullName(english));
                return true;
            }

            var achievements = NormalizeStoredText(data.GetAchievements(english, fallbackToOtherLocale));
            if (TryFirstSentence(achievements, out prompt))
            {
                prompt = RedactNameComponents(prompt, data.GetFullName(english));
                return true;
            }

            var bio = NormalizeStoredText(data.GetShortBio(english, fallbackToOtherLocale));
            if (TryTruncate(bio, out prompt))
            {
                prompt = RedactNameComponents(prompt, data.GetFullName(english));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Unity YAML often stores long strings with visual wraps as real newlines plus
        /// indentation spaces ("\n    "). Collapse those so RU/EN quiz prompts both use
        /// the same MaxPromptLength budget instead of ~25-char wrap fragments.
        /// Intentional paragraph breaks (newline without indent) are kept.
        /// </summary>
        static string NormalizeStoredText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return Regex.Replace(text, @"\n[ \t]+", " ");
        }

        public static bool HasFactPrompt(MathematicianData data, bool english) =>
            TryGetFactPrompt(data, english, out _);

        static string RedactNameComponents(string prompt, string fullName)
        {
            if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrWhiteSpace(fullName))
                return prompt;

            // Match on diacritic-folded forms so stressed "Хорезми́" equals unstressed "Хорезми".
            var tokens = fullName
                .Split(NameTokenSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => FoldForNameMatch(t.Trim()))
                .Where(t => t.Length >= 3 && !NameParticles.Contains(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(t => t.Length)
                .ToList();

            if (tokens.Count == 0)
                return prompt;

            var result = prompt;
            foreach (var token in tokens)
                result = RedactFoldedToken(result, token);

            return Regex.Replace(result, @"(\*\*\*)(?:\s+\*\*\*)+", "$1");
        }

        static string RedactFoldedToken(string prompt, string foldedToken)
        {
            BuildFoldMap(prompt, out var foldedPrompt, out var foldToOrig);
            var escaped = Regex.Escape(foldedToken);
            // Non-letter boundaries so Cyrillic/Latin names match as whole words.
            // Cyrillic tokens ≥5 also swallow up to 4 trailing letters (declensions).
            var pattern = foldedToken.Length >= 5 && IsCyrillicToken(foldedToken)
                ? $@"(?<![\p{{L}}]){escaped}[\p{{IsCyrillic}}]{{0,4}}(?![\p{{L}}])"
                : $@"(?<![\p{{L}}]){escaped}(?![\p{{L}}])";

            var matches = Regex.Matches(foldedPrompt, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (matches.Count == 0)
                return prompt;

            var sb = new StringBuilder(prompt);
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var match = matches[i];
                var origStart = foldToOrig[match.Index];
                var origEnd = foldToOrig[match.Index + match.Length];
                sb.Remove(origStart, origEnd - origStart);
                sb.Insert(origStart, "***");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Strips combining marks (Unicode Mn), including Cyrillic stress U+0301,
        /// so accented and plain name spellings compare equal.
        /// </summary>
        static string FoldForNameMatch(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var normalized = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds a diacritic-folded string plus a map from each folded index to the
        /// corresponding start index in <paramref name="original"/> (sentinel at end).
        /// </summary>
        static void BuildFoldMap(string original, out string folded, out int[] foldToOrig)
        {
            var fold = new StringBuilder(original.Length);
            var map = new List<int>(original.Length + 1);
            var i = 0;
            while (i < original.Length)
            {
                var len = char.IsHighSurrogate(original[i]) && i + 1 < original.Length && char.IsLowSurrogate(original[i + 1])
                    ? 2
                    : 1;
                var piece = original.Substring(i, len).Normalize(NormalizationForm.FormD);
                foreach (var c in piece)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                        continue;
                    fold.Append(c);
                    map.Add(i);
                }

                i += len;
            }

            map.Add(original.Length);
            folded = fold.ToString();
            foldToOrig = map.ToArray();
        }

        static bool IsCyrillicToken(string token)
        {
            foreach (var c in token)
            {
                if (c is (>= '\u0400' and <= '\u04FF') or 'Ё' or 'ё')
                    return true;
            }

            return false;
        }

        static bool TryPickRandomLine(string text, out string prompt)
        {
            prompt = null;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var lines = SplitLines(text)
                .Select(NormalizeLine)
                .Where(line => line.Length >= 20)
                .ToList();

            if (lines.Count == 0)
                return false;

            prompt = Truncate(lines[UnityEngine.Random.Range(0, lines.Count)]);
            return !string.IsNullOrWhiteSpace(prompt);
        }

        static bool TryFirstSentence(string text, out string prompt)
        {
            prompt = null;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var normalized = text.Trim();
            var end = FindSentenceEnd(normalized, 0);
            if (end <= 0)
                return TryTruncate(normalized, out prompt);

            prompt = Truncate(normalized[..end].Trim());
            return prompt.Length >= 20;
        }

        static bool TryTruncate(string text, out string prompt)
        {
            prompt = null;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var normalized = text.Trim();
            if (normalized.Length < 20)
                return false;

            prompt = Truncate(normalized);
            return true;
        }

        static IEnumerable<string> SplitLines(string text)
        {
            foreach (var part in text.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = part.Trim();
                if (line.Length == 0)
                    continue;

                yield return line;
            }
        }

        static string NormalizeLine(string line)
        {
            var trimmed = line.Trim();
            while (trimmed.Length > 0 && BulletPrefixes.Contains(trimmed[0]))
                trimmed = trimmed[1..].TrimStart();

            return trimmed;
        }

        static int FindSentenceEnd(string text, int start)
        {
            for (var i = start; i < text.Length; i++)
            {
                if (TryGetSentenceEndExclusive(text, i, out var end))
                    return end;
            }

            return -1;
        }

        static string Truncate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            var normalized = text.Trim();
            if (normalized.Length <= MaxPromptLength)
                return normalized;

            // Prefer ending on a complete sentence within the budget — do not start the next one.
            var sentenceEnd = FindLastSentenceEndExclusive(normalized, MaxPromptLength);
            if (sentenceEnd >= 20)
                return normalized[..sentenceEnd].TrimEnd();

            var cut = normalized[..MaxPromptLength];
            var lastSpace = cut.LastIndexOf(' ');
            if (lastSpace > MaxPromptLength / 2)
                cut = cut[..lastSpace];

            return cut.TrimEnd() + "…";
        }

        /// <summary>
        /// Length of the longest prefix that ends on a complete sentence and stays
        /// within <paramref name="maxLength"/> (exclusive end index). Returns -1 if none.
        /// </summary>
        static int FindLastSentenceEndExclusive(string text, int maxLength)
        {
            var limit = Math.Min(text.Length, maxLength);
            for (var i = limit - 1; i >= 0; i--)
            {
                if (!TryGetSentenceEndExclusive(text, i, out var end))
                    continue;

                if (end > maxLength)
                    continue;

                // Skip single-letter initials ("A.", "И.").
                if (text[i] == '.' && i > 0 && char.IsLetter(text[i - 1]) &&
                    (i == 1 || !char.IsLetter(text[i - 2])))
                    continue;

                // Terminator must close the text or be followed by whitespace (next sentence starts later).
                if (end < text.Length && !char.IsWhiteSpace(text[end]))
                    continue;

                return end;
            }

            return -1;
        }

        /// <summary>
        /// If <paramref name="index"/> is the last character of a sentence terminator
        /// (. ! ? … or "..."), sets <paramref name="endExclusive"/> past optional closing quotes.
        /// </summary>
        static bool TryGetSentenceEndExclusive(string text, int index, out int endExclusive)
        {
            endExclusive = index + 1;
            if (index < 0 || index >= text.Length)
                return false;

            var c = text[index];
            if (c == '…')
            {
                endExclusive = index + 1;
            }
            else if (c is '.' or '!' or '?')
            {
                // Only treat the final '.' of "..." / ".." as the terminator.
                if (c == '.' && index + 1 < text.Length && text[index + 1] == '.')
                    return false;
            }
            else
            {
                return false;
            }

            while (endExclusive < text.Length && IsClosingQuote(text[endExclusive]))
                endExclusive++;

            return true;
        }

        static bool IsClosingQuote(char c) =>
            c is '"' or '\'' or '»' or '”' or '’' or '‹' or '›';
    }

}
