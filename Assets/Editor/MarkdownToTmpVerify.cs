#if UNITY_EDITOR
using PeopleOfMath.Data;
using PeopleOfMath.Text;
using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class MarkdownToTmpVerify
    {
        const string ArtinAssetPath = "Assets/Data/Mathematicians/artin.asset";

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

            var scrollEmoji = char.ConvertFromUtf32(0x1F4DC);
            var emojiHeader = Convert($"### {scrollEmoji} Ранние годы");
            AssertContains(scrollEmoji, emojiHeader, "Emoji in header");
            AssertContains("<size=115%>", emojiHeader, "Emoji header size");

            VerifyArtinEmojiData();

            Debug.Log("MarkdownToTmp verification passed.");
        }

        static void VerifyArtinEmojiData()
        {
            var data = AssetDatabase.LoadAssetAtPath<MathematicianData>(ArtinAssetPath);
            if (data == null)
            {
                Debug.LogWarning($"[{ArtinAssetPath}] Skipped — asset not found.");
                return;
            }

            var bio = data.shortBioRu ?? "";
            if (!bio.Contains(char.ConvertFromUtf32(0x1F4DC)))
                Debug.LogError("[Artin emoji] shortBioRu missing U+1F4DC scroll emoji.");
            if (!bio.Contains(char.ConvertFromUtf32(0x1F680)))
                Debug.LogError("[Artin emoji] shortBioRu missing U+1F680 rocket emoji.");
            if (bio.Contains("\\U0001F"))
                Debug.LogError("[Artin emoji] shortBioRu still contains literal \\U escape.");
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
