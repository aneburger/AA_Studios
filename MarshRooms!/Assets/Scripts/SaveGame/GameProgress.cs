using UnityEngine;

// Single source of truth for the "game beaten" marker.
public static class GameProgress
{
    public const string BeatenKey = "game_beaten";

    public static bool IsBeaten => PlayerPrefs.GetInt(BeatenKey, 0) == 1;

    public static void MarkBeaten()
    {
        PlayerPrefs.SetInt(BeatenKey, 1);
        PlayerPrefs.Save();
    }

    public static void ResetBeaten()
    {
        PlayerPrefs.DeleteKey(BeatenKey);
        PlayerPrefs.Save();
    }
}