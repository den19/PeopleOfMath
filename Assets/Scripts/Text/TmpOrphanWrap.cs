using TMPro;
using UnityEngine;

namespace PeopleOfMath.Text
{
    /// <summary>
    /// Avoids ugly 1–2 character last lines when TMP mid-word wraps long names.
    /// Prefers breaking at the last space; falls back to pulling enough chars onto the last line.
    /// </summary>
    public static class TmpOrphanWrap
    {
        const int MaxPasses = 3;

        public static void AvoidShortLastLine(TMP_Text text, float width, int minLastLineChars = 3)
        {
            if (text == null || string.IsNullOrEmpty(text.text) || width <= 1f || minLastLineChars < 1)
                return;

            text.text = NormalizeName(text.text);
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

            for (var pass = 0; pass < MaxPasses; pass++)
            {
                text.ForceMeshUpdate();
                var info = text.textInfo;
                if (info.lineCount < 2)
                    return;

                var lastLine = info.lineInfo[info.lineCount - 1];
                if (CountVisibleChars(text, lastLine) >= minLastLineChars)
                    return;

                if (!TryInsertBreak(text, minLastLineChars))
                    return;
            }
        }

        static string NormalizeName(string value)
        {
            // Collapse breaks from a previous layout pass so re-measure starts from the source name.
            return value.Replace('\r', ' ').Replace('\n', ' ');
        }

        static int CountVisibleChars(TMP_Text text, TMP_LineInfo line)
        {
            var count = 0;
            var end = Mathf.Min(line.lastCharacterIndex, text.textInfo.characterCount - 1);
            for (var i = line.firstCharacterIndex; i <= end; i++)
            {
                var c = text.textInfo.characterInfo[i].character;
                if (!char.IsWhiteSpace(c) && !char.IsControl(c))
                    count++;
            }

            return count;
        }

        static bool TryInsertBreak(TMP_Text text, int minLastLineChars)
        {
            var value = text.text;
            var segmentStart = value.LastIndexOf('\n') + 1;
            var segment = value.Substring(segmentStart);
            if (segment.Length == 0)
                return false;

            var spaceIdx = segment.LastIndexOf(' ');
            if (spaceIdx > 0)
            {
                text.text = value.Substring(0, segmentStart)
                    + segment.Substring(0, spaceIdx)
                    + "\n"
                    + segment.Substring(spaceIdx + 1);
                return true;
            }

            if (segment.Length <= minLastLineChars)
                return false;

            var breakAt = segment.Length - minLastLineChars;
            text.text = value.Substring(0, segmentStart)
                + segment.Substring(0, breakAt)
                + "\n"
                + segment.Substring(breakAt);
            return true;
        }
    }
}
