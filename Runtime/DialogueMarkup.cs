using UnityEngine;

namespace Retro.Dialogue
{
    /// <summary>
    /// Emphasis tags for dialogue lines, turned into TextMeshPro rich text before a line is shown:
    /// <list type="bullet">
    /// <item><c>&lt;em&gt;word&lt;/em&gt;</c>: the speaker's emphasis colour.</item>
    /// <item><c>&lt;shake&gt;word&lt;/shake&gt;</c>: the letters tremble (needs <see cref="DialogueTextEffects"/>).</item>
    /// <item><c>&lt;wave&gt;word&lt;/wave&gt;</c>: the letters bob in a wave (needs <see cref="DialogueTextEffects"/>).</item>
    /// </list>
    /// Tags combine across each other (<c>&lt;em&gt;&lt;shake&gt;no&lt;/shake&gt;&lt;/em&gt;</c>), but shake and wave can't nest inside one another.
    /// Any other TextMeshPro tag (<c>&lt;b&gt;</c>, <c>&lt;i&gt;</c>, <c>&lt;size&gt;</c>) works as usual.
    /// </summary>
    public static class DialogueMarkup
    {
        public const string ShakeLinkId = "shake";
        public const string WaveLinkId = "wave";
        public const string AngryLinkId = "angry";
        public const string HappyLinkId = "happy";
        public const string EmbarrassedLinkId = "embarrassed";

        public static bool HasAnimatedText(string line)
        {
            return !string.IsNullOrEmpty(line) && (line.Contains("<shake>") || line.Contains("<wave>")
                || line.Contains("<angry>") || line.Contains("<happy>") || line.Contains("<embarrassed>"));
        }

        public static readonly Color DefaultEmphasisColor = new Color(0.86f, 0.2f, 0.2f);

        /// <summary>Converts the emphasis tags in <paramref name="line"/> to TextMeshPro rich text.</summary>
        public static string ToRichText(string line, Color emphasisColor)
        {
            if (string.IsNullOrEmpty(line) || line.IndexOf('<') < 0) return line;

            return line
                .Replace("<em>", $"<color=#{ColorUtility.ToHtmlStringRGBA(emphasisColor)}>")
                .Replace("</em>", "</color>")
                .Replace("<shake>", $"<link=\"{ShakeLinkId}\">")
                .Replace("</shake>", "</link>")
                .Replace("<wave>", $"<link=\"{WaveLinkId}\">")
                .Replace("</wave>", "</link>")
                .Replace("<angry>", $"<link=\"{AngryLinkId}\">")
                .Replace("</angry>", "</link>")
                .Replace("<happy>", $"<link=\"{HappyLinkId}\">")
                .Replace("</happy>", "</link>")
                .Replace("<embarrassed>", $"<link=\"{EmbarrassedLinkId}\">")
                .Replace("</embarrassed>", "</link>");
        }
    }
}
