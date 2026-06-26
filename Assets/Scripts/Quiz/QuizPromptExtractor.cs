using System;
using System.Collections.Generic;
using System.Linq;
using PeopleOfMath.Data;
using UnityEngine;

namespace PeopleOfMath.Quiz
{
    public static class QuizPromptExtractor
    {
        const int MaxPromptLength = 180;

        static readonly char[] LineSeparators = { '\n', '\r' };
        static readonly char[] BulletPrefixes = { '•', '-', '–', '—', '*', '·' };

        public static bool TryGetFactPrompt(MathematicianData data, bool english, out string prompt)
        {
            prompt = null;
            if (data == null)
                return false;

            var facts = data.GetInterestingFacts(english);
            if (TryPickRandomLine(facts, out prompt))
                return true;

            var achievements = data.GetAchievements(english);
            if (TryFirstSentence(achievements, out prompt))
                return true;

            var bio = data.GetShortBio(english);
            if (TryTruncate(bio, out prompt))
                return true;

            return false;
        }

        public static bool HasFactPrompt(MathematicianData data, bool english) =>
            TryGetFactPrompt(data, english, out _);

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
                if (text[i] is '.' or '!' or '?')
                    return i + 1;
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

            var cut = normalized[..MaxPromptLength];
            var lastSpace = cut.LastIndexOf(' ');
            if (lastSpace > MaxPromptLength / 2)
                cut = cut[..lastSpace];

            return cut.TrimEnd() + "…";
        }
    }

}
