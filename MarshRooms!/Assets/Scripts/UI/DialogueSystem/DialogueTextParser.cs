using System.Collections.Generic;
using System.Text.RegularExpressions;

// Stores which character index range should have a given effect applied.
public class TextEffectRange
{
    public int startIndex;
    public int endIndex;
    public TextEffectType effectType;
}

public enum TextEffectType
{
    Wave,
    Shake
}

public static class DialogueTextParser
{
    // Map of tag names to effect types
    private static readonly Dictionary<string, TextEffectType> tagMap = new Dictionary<string, TextEffectType>
    {
        { "wave", TextEffectType.Wave },
        { "shake", TextEffectType.Shake }
    };

    public struct ParseResult
    {
        public string cleanText;
        public List<TextEffectRange> effectRanges;
    }

    public static ParseResult Parse(string rawText)
    {
        var effectRanges = new List<TextEffectRange>();
        string text = rawText;

        foreach (var kvp in tagMap)
        {
            string tag = kvp.Key;
            TextEffectType effectType = kvp.Value;

            string openTag = $"<{tag}>";
            string closeTag = $"</{tag}>";

            while (true)
            {
                int openIdx = text.IndexOf(openTag);
                if (openIdx < 0) break;

                int closeIdx = text.IndexOf(closeTag, openIdx);
                if (closeIdx < 0) break;

                int contentStart = openIdx;
                int contentLength = closeIdx - openIdx - openTag.Length;

                text = text.Remove(closeIdx, closeTag.Length);
                text = text.Remove(openIdx, openTag.Length);

                effectRanges.Add(new TextEffectRange
                {
                    startIndex = contentStart,
                    endIndex = contentStart + contentLength - 1,
                    effectType = effectType
                });
            }
        }

        return new ParseResult
        {
            cleanText = text,
            effectRanges = effectRanges
        };
    }
 
    public static List<TextEffectType> GetEffectsAt(int charIndex, List<TextEffectRange> ranges)
    {
        var result = new List<TextEffectType>();
        foreach (var range in ranges)
        {
            if (charIndex >= range.startIndex && charIndex <= range.endIndex)
                result.Add(range.effectType);
        }
        return result;
    }
}
