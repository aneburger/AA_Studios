// Tracks stats that need to survive across level reloads within a single run:
// death count, kill count, elapsed play time, and cards collected in elevators.
//
// This is a separate concern from the "RunStats" referenced in SaveGameData -
// that one (BoonManager.Stats) holds gameplay modifiers from boons (e.g.
// healthDropRateMultiplier), not play-session analytics, so there's no overlap.
//
// This does NOT currently persist to your save file, so death count / kills /
// time / cards will reset to zero if the player quits the app and later
// continues a saved game. If you want those to survive that, add fields to
// SaveGameData and read/write them in LevelLoader's SaveCurrentLevel /
// ApplyPendingRestore.

using System.Collections.Generic;
using UnityEngine;

public class RunStatsTracker : MonoBehaviour
{
    public static RunStatsTracker Instance { get; private set; }

    public int DeathCount { get; private set; }
    public int KillCount { get; private set; }
    public float ElapsedPlayTime { get; private set; }

    private readonly List<BoonCardData> collectedCards = new List<BoonCardData>();
    public IReadOnlyList<BoonCardData> CollectedCards => collectedCards;

    private bool isTiming = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (isTiming)
            ElapsedPlayTime += Time.deltaTime;
    }

    public void PauseTimer() => isTiming = false;
    public void ResumeTimer() => isTiming = true;

    public void RegisterKill()
    {
        KillCount++;
    }

    public void RegisterDeath()
    {
        DeathCount++;
    }

    public void RegisterCardCollected(BoonCardData card)
    {
        if (card != null)
            collectedCards.Add(card);
    }

    // Call this when a brand new run starts (new game), NOT on retry/reload
    // after a death - death count and cards collected should persist through
    // a retry, since the death screen for THIS run needs to show them.
    // TODO: call this from wherever your "New Game" button logic lives.
    public void ResetForNewRun()
    {
        DeathCount = 0;
        KillCount = 0;
        ElapsedPlayTime = 0f;
        collectedCards.Clear();
        isTiming = true;
    }

    public static string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        int millis = Mathf.FloorToInt((seconds * 1000f) % 1000f);
        return $"{minutes:00}:{secs:00}:{millis:000}";
    }
}