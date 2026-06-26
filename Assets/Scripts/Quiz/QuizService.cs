using System;
using System.Collections.Generic;
using System.Linq;
using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using UnityEngine;

namespace PeopleOfMath.Quiz
{
    public static class QuizService
    {
        public const int DefaultQuestionCount = 10;
        public const int OptionCount = 4;
        const int MinPoolSize = OptionCount;

        public static IReadOnlyList<MathematicianData> GetEligiblePool(
            QuizMode mode,
            IReadOnlyList<MathematicianData> all)
        {
            if (all == null || all.Count == 0)
                return Array.Empty<MathematicianData>();

            var english = LocaleHelper.IsEnglish;
            return mode switch
            {
                QuizMode.Portrait => all.Where(HasPortrait).ToList(),
                QuizMode.Fact => all.Where(d => QuizPromptExtractor.HasFactPrompt(d, english)).ToList(),
                QuizMode.Mixed => all.Where(d => HasPortrait(d) || QuizPromptExtractor.HasFactPrompt(d, english)).ToList(),
                _ => Array.Empty<MathematicianData>()
            };
        }

        public static IReadOnlyList<MathematicianData> GetPortraitPool(IReadOnlyList<MathematicianData> all) =>
            all?.Where(HasPortrait).ToList() ?? new List<MathematicianData>();

        public static IReadOnlyList<MathematicianData> GetFactPool(IReadOnlyList<MathematicianData> all)
        {
            if (all == null)
                return Array.Empty<MathematicianData>();

            var english = LocaleHelper.IsEnglish;
            return all.Where(d => QuizPromptExtractor.HasFactPrompt(d, english)).ToList();
        }

        public static List<QuizQuestion> GenerateRound(
            IReadOnlyList<MathematicianData> all,
            QuizMode mode,
            int count = DefaultQuestionCount,
            int? seed = null)
        {
            if (all == null || all.Count < MinPoolSize)
                return null;

            var rng = seed.HasValue ? new System.Random(seed.Value) : null;
            var portraitPool = GetPortraitPool(all);
            var factPool = GetFactPool(all);

            if (mode == QuizMode.Portrait && portraitPool.Count < MinPoolSize)
                return null;
            if (mode == QuizMode.Fact && factPool.Count < MinPoolSize)
                return null;
            if (mode == QuizMode.Mixed && portraitPool.Count < MinPoolSize && factPool.Count < MinPoolSize)
                return null;

            var mixedPool = GetEligiblePool(QuizMode.Mixed, all);
            if (mixedPool.Count < MinPoolSize)
                return null;

            var questions = new List<QuizQuestion>(count);
            var usedIds = new HashSet<string>();

            for (var i = 0; i < count; i++)
            {
                var kind = ResolvePromptKind(mode, portraitPool, factPool, rng);
                var pool = kind == QuizPromptKind.Portrait ? portraitPool : factPool;
                if (pool.Count < MinPoolSize)
                {
                    kind = kind == QuizPromptKind.Portrait ? QuizPromptKind.Fact : QuizPromptKind.Portrait;
                    pool = kind == QuizPromptKind.Portrait ? portraitPool : factPool;
                }

                if (pool.Count < MinPoolSize)
                    return null;

                var correct = PickCorrect(pool, usedIds, rng);
                if (correct == null)
                    break;

                usedIds.Add(correct.id);
                questions.Add(BuildQuestion(correct, pool, kind));
            }

            return questions.Count == count ? questions : null;
        }

        static QuizPromptKind ResolvePromptKind(
            QuizMode mode,
            IReadOnlyList<MathematicianData> portraitPool,
            IReadOnlyList<MathematicianData> factPool,
            System.Random rng)
        {
            return mode switch
            {
                QuizMode.Portrait => QuizPromptKind.Portrait,
                QuizMode.Fact => QuizPromptKind.Fact,
                _ => ResolveMixedKind(portraitPool, factPool, rng)
            };
        }

        static QuizPromptKind ResolveMixedKind(
            IReadOnlyList<MathematicianData> portraitPool,
            IReadOnlyList<MathematicianData> factPool,
            System.Random rng)
        {
            var portraitOk = portraitPool.Count >= MinPoolSize;
            var factOk = factPool.Count >= MinPoolSize;
            if (portraitOk && !factOk)
                return QuizPromptKind.Portrait;
            if (factOk && !portraitOk)
                return QuizPromptKind.Fact;

            var roll = rng?.Next(0, 2) ?? UnityEngine.Random.Range(0, 2);
            return roll == 0 ? QuizPromptKind.Portrait : QuizPromptKind.Fact;
        }

        static MathematicianData PickCorrect(
            IReadOnlyList<MathematicianData> pool,
            HashSet<string> usedIds,
            System.Random rng)
        {
            var available = pool.Where(d => !usedIds.Contains(d.id)).ToList();
            if (available.Count == 0)
                return null;

            var index = rng?.Next(0, available.Count) ?? UnityEngine.Random.Range(0, available.Count);
            return available[index];
        }

        static QuizQuestion BuildQuestion(
            MathematicianData correct,
            IReadOnlyList<MathematicianData> pool,
            QuizPromptKind kind)
        {
            var english = LocaleHelper.IsEnglish;
            var distractors = PickDistractors(correct, pool, OptionCount - 1);
            var optionIds = new List<string> { correct.id };
            optionIds.AddRange(distractors.Select(d => d.id));
            Shuffle(optionIds);

            return new QuizQuestion
            {
                CorrectId = correct.id,
                OptionIds = optionIds,
                Kind = kind,
                Portrait = kind == QuizPromptKind.Portrait
                    ? PortraitResolver.GetPrimaryPortrait(correct)
                    : null,
                PromptText = kind == QuizPromptKind.Fact
                    ? QuizPromptExtractor.TryGetFactPrompt(correct, english, out var prompt)
                        ? prompt
                        : ""
                    : null
            };
        }

        static List<MathematicianData> PickDistractors(
            MathematicianData correct,
            IReadOnlyList<MathematicianData> pool,
            int count)
        {
            var candidates = pool.Where(d => d.id != correct.id).ToList();
            if (candidates.Count <= count)
                return candidates.OrderBy(_ => UnityEngine.Random.value).Take(count).ToList();

            var related = candidates
                .Where(d => SharesTaxonomy(correct, d))
                .OrderBy(_ => UnityEngine.Random.value)
                .Take(count)
                .ToList();

            foreach (var candidate in candidates.OrderBy(_ => UnityEngine.Random.value))
            {
                if (related.Count >= count)
                    break;
                if (related.Any(d => d.id == candidate.id))
                    continue;
                related.Add(candidate);
            }

            return related;
        }

        static bool SharesTaxonomy(MathematicianData a, MathematicianData b)
        {
            if (a == null || b == null)
                return false;

            return a.centuryKeys.Any(k => b.centuryKeys.Contains(k))
                || a.branchKeys.Any(k => b.branchKeys.Contains(k));
        }

        static bool HasPortrait(MathematicianData data) =>
            data != null && PortraitResolver.GetPrimaryPortrait(data) != null;

        static void Shuffle(IList<string> ids)
        {
            for (var i = ids.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (ids[i], ids[j]) = (ids[j], ids[i]);
            }
        }
    }

}
