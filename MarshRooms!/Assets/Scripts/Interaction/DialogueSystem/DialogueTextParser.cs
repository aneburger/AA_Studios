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
    Shake,
    Rainbow,
    Pulse
}

public static class DialogueTextParser
{
    // Map of tag names to effect types
    private static readonly Dictionary<string, TextEffectType> tagMap = new Dictionary<string, TextEffectType>
    {
        { "wave", TextEffectType.Wave },
        { "shake", TextEffectType.Shake },
        { "rainbow", TextEffectType.Rainbow },
        { "pulse",   TextEffectType.Pulse   }
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

        var allTags = new List<(int openIdx, int closeIdx, string tag, TextEffectType effectType)>();

        foreach (var kvp in tagMap)
        {
            string tag = kvp.Key;
            TextEffectType effectType = kvp.Value;
            string openTag = $"<{tag}>";
            string closeTag = $"</{tag}>";

            int searchFrom = 0;
            while (true)
            {
                int openIdx = text.IndexOf(openTag, searchFrom);
                if (openIdx < 0) break;
                int closeIdx = text.IndexOf(closeTag, openIdx);
                if (closeIdx < 0) break;
                allTags.Add((openIdx, closeIdx, tag, effectType));
                searchFrom = closeIdx + closeTag.Length;
            }
        }

        allTags.Sort((a, b) => a.openIdx.CompareTo(b.openIdx));

        int totalRemoved = 0;
        foreach (var (openIdx, closeIdx, tag, effectType) in allTags)
        {
            string openTag = $"<{tag}>";
            string closeTag = $"</{tag}>";

            int adjustedOpen  = openIdx  - totalRemoved;
            int adjustedClose = closeIdx - totalRemoved;
            int contentStart  = adjustedOpen;
            int contentLength = adjustedClose - adjustedOpen - openTag.Length;

            text = text.Remove(adjustedClose, closeTag.Length);
            text = text.Remove(adjustedOpen,  openTag.Length);
            totalRemoved += openTag.Length + closeTag.Length;

            int tmpTagOffset = CountTMPTagCharsBefore(text, contentStart);

            effectRanges.Add(new TextEffectRange
            {
                startIndex = contentStart - tmpTagOffset,
                endIndex   = contentStart - tmpTagOffset + contentLength - 1,
                effectType = effectType
            });
        }

        return new ParseResult
        {
            cleanText    = text,
            effectRanges = effectRanges
        };
    }

    private static int CountTMPTagCharsBefore(string text, int beforeIndex)
    {
        int count = 0;
        int i = 0;
        while (i < beforeIndex && i < text.Length)
        {
            if (text[i] == '<')
            {
                int closeAngle = text.IndexOf('>', i);
                if (closeAngle > 0 && closeAngle < beforeIndex)
                {
                    count += closeAngle - i + 1;
                    i = closeAngle + 1;
                    continue;
                }
            }
            i++;
        }
        return count;
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
