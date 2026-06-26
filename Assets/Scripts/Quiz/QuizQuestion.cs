using System.Collections.Generic;
using UnityEngine;

namespace PeopleOfMath.Quiz
{
    public sealed class QuizQuestion
    {
        public string CorrectId;
        public IReadOnlyList<string> OptionIds;
        public QuizPromptKind Kind;
        public Sprite Portrait;
        public string PromptText;
    }
}
