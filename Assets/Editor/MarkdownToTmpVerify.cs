#if UNITY_EDITOR
using PeopleOfMath.Text;
using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class MarkdownToTmpVerify
    {
        [MenuItem("PeopleOfMath/Verify Markdown Converter")]
        public static void Run()
        {
            AssertContains("bold", Convert("Plain text without markdown"), "Plain");
            AssertContains("<b>жирный</b>", Convert("Текст **жирный** текст"), "Bold RU");
            AssertContains("<size=115%>", Convert("Intro ### Заголовок"), "Header");
            AssertContains("<indent=1em>•", Convert("Para. *   **Пункт:** описание"), "Bullet");
            AssertContains("<indent=1em>1.", Convert("Start 1.  **First:** item"), "Numbered");
            AssertContains("&lt;", Convert("3 < π < 4"), "Escape lt");
            AssertContains("<i>", Convert("Цитата *курсив* здесь"), "Italic");
            AssertNoContains("**", Convert("Текст **жирный** текст"), "No raw bold markers");

            Debug.Log("MarkdownToTmp verification passed.");
        }

        static string Convert(string input) => MarkdownToTmp.Convert(input);

        static void AssertContains(string expected, string actual, string label)
        {
            if (actual.Contains(expected))
                return;

            Debug.LogError($"[{label}] Expected to contain '{expected}', got: {actual}");
        }

        static void AssertNoContains(string unexpected, string actual, string label)
        {
            if (!actual.Contains(unexpected))
                return;

            Debug.LogError($"[{label}] Expected NOT to contain '{unexpected}', got: {actual}");
        }
    }
}
#endif
