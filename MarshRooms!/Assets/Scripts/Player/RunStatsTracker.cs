// Tracks stats that need to survive across level reloads within a single run:
// death count, kill count, elapsed play time, and cards collected in elevators.

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RunStatsSaveData
{
    public int deathCount;
    public int killCount;
    public float elapsedPlayTime;
    public List<string> collectedCardIds = new List<string>();
}

public class RunStatsTracker : MonoBehaviour
{
    private static RunStatsTracker instance;

    public static RunStatsTracker Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<RunStatsTracker>();

                if (instance == null)
                {
                    GameObject go = new GameObject("RunStatsTracker (auto-created)");
                    instance = go.AddComponent<RunStatsTracker>();
                    DontDestroyOnLoad(go);
                    Debug.LogWarning("RunStatsTracker: no instance found in the scene - auto-created one with no 'All Boon Cards' assigned. Add a RunStatsTracker component to your Persistent scene and assign that list so collected cards can be saved/restored.");
                }
            }

            return instance;
        }
    }

    [Header("Save/Load")]
    [SerializeField] private List<BoonCardData> allBoonCards;

    public int DeathCount { get; private set; }
    public int KillCount { get; private set; }
    public float ElapsedPlayTime { get; private set; }

    private readonly List<BoonCardData> collectedCards = new List<BoonCardData>();
    public IReadOnlyList<BoonCardData> CollectedCards => collectedCards;

    private bool isTiming = true;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
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

    public void ResetForNewRun()
    {
        DeathCount = 0;
        KillCount = 0;
        ElapsedPlayTime = 0f;
        collectedCards.Clear();
        isTiming = true;
    }

    // -- GET SAVE DATA --
    public RunStatsSaveData GetSaveData()
    {
        RunStatsSaveData data = new RunStatsSaveData
        {
            deathCount = DeathCount,
            killCount = KillCount,
            elapsedPlayTime = ElapsedPlayTime
        };

        foreach (BoonCardData card in collectedCards)
        {
            if (card != null && !string.IsNullOrEmpty(card.boonId))
                data.collectedCardIds.Add(card.boonId);
        }

        return data;
    }

    // -- RESTORE FROM SAVE --
    public void RestoreFromSave(RunStatsSaveData data)
    {
        if (data == null) return;

        DeathCount = data.deathCount;
        KillCount = data.killCount;
        ElapsedPlayTime = data.elapsedPlayTime;

        collectedCards.Clear();

        if (data.collectedCardIds != null)
        {
            foreach (string id in data.collectedCardIds)
            {
                BoonCardData card = FindCardById(id);
                if (card != null)
                    collectedCards.Add(card);
                else
                    Debug.LogWarning($"RunStatsTracker: could not find BoonCardData with id '{id}' while restoring save.");
            }
        }
    }

    // -- FIND CARD BY ID --
    private BoonCardData FindCardById(string boonId)
    {
        if (string.IsNullOrEmpty(boonId) || allBoonCards == null) return null;
        return allBoonCards.Find(c => c != null && c.boonId == boonId);
    }

    public static string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        int millis = Mathf.FloorToInt((seconds * 1000f) % 1000f);
        return $"{minutes:00}:{secs:00}:{millis:000}";
    }
}