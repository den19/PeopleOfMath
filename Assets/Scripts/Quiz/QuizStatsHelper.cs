using UnityEngine;

namespace PeopleOfMath.Quiz
{
    public static class QuizStatsHelper
    {
        const string BestPortraitKey = "quiz_best_portrait";
        const string BestFactKey = "quiz_best_fact";
        const string BestMixedKey = "quiz_best_mixed";
        const string GamesPlayedKey = "quiz_games_played";

        public static int GetBestScore(QuizMode mode) =>
            PlayerPrefs.GetInt(GetBestKey(mode), 0);

        public static int GetGamesPlayed() =>
            PlayerPrefs.GetInt(GamesPlayedKey, 0);

        public static bool TryUpdateBestScore(QuizMode mode, int score)
        {
            var key = GetBestKey(mode);
            var previous = PlayerPrefs.GetInt(key, 0);
            if (score <= previous)
                return false;

            PlayerPrefs.SetInt(key, score);
            PlayerPrefs.Save();
            return true;
        }

        public static void RecordGamePlayed()
        {
            PlayerPrefs.SetInt(GamesPlayedKey, GetGamesPlayed() + 1);
            PlayerPrefs.Save();
        }

        static string GetBestKey(QuizMode mode) =>
            mode switch
            {
                QuizMode.Portrait => BestPortraitKey,
                QuizMode.Fact => BestFactKey,
                _ => BestMixedKey
            };
    }

}
