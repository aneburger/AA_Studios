using System.Collections.Generic;
using UnityEngine;

public static class StoryFlags
{
    private const string StoryKey = "StoryFlags.Story";
    private const string RunKey   = "StoryFlags.Run";

    private static HashSet<string> story;
    private static HashSet<string> run;

    // Editor with domain reload disabled would otherwise keep a stale cache
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() { story = null; run = null; }

    // -- STORY FLAGS --
    public static bool Has(string id) => Load(ref story, StoryKey).Contains(id);
    public static void Set(string id) => Add(ref story, StoryKey, id);

    // -- RUN FLAGS --
    public static bool HasRun(string id) => Load(ref run, RunKey).Contains(id);
    public static void SetRun(string id) => Add(ref run, RunKey, id);

    public static void ClearRunFlags()
    {
        run = new HashSet<string>();
        PlayerPrefs.DeleteKey(RunKey);
        PlayerPrefs.Save();
    }

    // -- NEW GAME --
    public static void ClearAll()
    {
        story = new HashSet<string>();
        PlayerPrefs.DeleteKey(StoryKey);
        ClearRunFlags();
    }

    private static HashSet<string> Load(ref HashSet<string> set, string key)
    {
        if (set != null) return set;
        string raw = PlayerPrefs.GetString(key, "");
        set = new HashSet<string>(raw.Split('|', System.StringSplitOptions.RemoveEmptyEntries));
        return set;
    }

    private static void Add(ref HashSet<string> set, string key, string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (!Load(ref set, key).Add(id)) return;
        PlayerPrefs.SetString(key, string.Join("|", set));
        PlayerPrefs.Save();
    }
}
