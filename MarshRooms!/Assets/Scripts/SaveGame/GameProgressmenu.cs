#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class GameProgressMenu
{
    [MenuItem("Tools/MarshRooms/Game Beaten/Set (unlock bonus menu)")]
    private static void SetBeaten()
    {
        GameProgress.MarkBeaten();
        Debug.Log("GameProgress: game_beaten = 1");
    }

    [MenuItem("Tools/MarshRooms/Game Beaten/Reset")]
    private static void ResetBeaten()
    {
        GameProgress.ResetBeaten();
        Debug.Log("GameProgress: game_beaten cleared");
    }
}
#endif