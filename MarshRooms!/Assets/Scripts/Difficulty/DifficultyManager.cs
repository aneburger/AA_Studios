using UnityEngine;
using System;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }
    public static event Action<Difficulty> OnDifficultyChanged;

    private const string PrefKey = "LastDifficulty";

    [Header("Active Difficulty")]
    [SerializeField] private Difficulty difficulty = Difficulty.Normal;

    [Header("Settings Assets")]
    [SerializeField] private DifficultySettings easy;
    [SerializeField] private DifficultySettings normal;
    [SerializeField] private DifficultySettings hard;
    [SerializeField] private DifficultySettings deathCap;

    public bool IsRunLocked { get; private set; }

    public static Difficulty Current => Instance != null ? Instance.difficulty : Difficulty.Normal;

    public static Difficulty PreferredDifficulty =>
        (Difficulty)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, (int)Difficulty.Normal), 0, 3);

    // -- AWAKE --
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // -- CALLS FOR THE UI --

    // -- START NEW GAME --
    public void StartNewRun(Difficulty value)
    {
        IsRunLocked = false;
        SetDifficulty(value);
        IsRunLocked = true;

        PlayerPrefs.SetInt(PrefKey, (int)value);
        PlayerPrefs.Save();
    }

    // -- RESUME RUN --
    public void ResumeRun(Difficulty saved)
    {
        IsRunLocked = false;
        SetDifficulty(saved);
        IsRunLocked = true;
    }

    public void UnlockRun() => IsRunLocked = false;

    // -- SET DIFFICULTY
    public void SetDifficulty(Difficulty value)
    {
        if (IsRunLocked) return;
        difficulty = value;
        OnDifficultyChanged?.Invoke(value);
    }

    //-- EDITOR TESTING --
    private void OnValidate()
    {
        if (Application.isPlaying && Instance == this)
            OnDifficultyChanged?.Invoke(difficulty);
    }

    [ContextMenu("Set Easy")] private void DebugEasy() { difficulty = Difficulty.Easy; OnDifficultyChanged?.Invoke(difficulty); }
    [ContextMenu("Set Normal")] private void DebugNormal() { difficulty = Difficulty.Normal; OnDifficultyChanged?.Invoke(difficulty); }
    [ContextMenu("Set Hard")] private void DebugHard() { difficulty = Difficulty.Hard; OnDifficultyChanged?.Invoke(difficulty); }
    [ContextMenu("Set Death Cap")] private void DebugDeathCap() { difficulty = Difficulty.DeathCap; OnDifficultyChanged?.Invoke(difficulty); }

    private DifficultySettings Active
    {
        get
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return easy;
                case Difficulty.Hard: return hard;
                case Difficulty.DeathCap: return deathCap;
                default: return normal;
            }
        }
    }

    //-- STATIC HELPERS--
    private static DifficultySettings S =>
        (Instance != null && TutorialDirector.Instance == null) ? Instance.Active : null;

    public static float EnemySpeedMult => S != null ? Mathf.Max(0.1f, S.enemySpeedMultiplier) : 1f;
    public static float EnemyFireRateMult => S != null ? Mathf.Max(0.1f, S.enemyFireRateMultiplier) : 1f;
    public static float EliteChanceMult => S != null ? S.eliteChanceMultiplier : 1f;
    public static float BossHealthMult => S != null ? Mathf.Max(0.1f, S.bossHealthMultiplier) : 1f;
    public static bool RestartRunOnDeath => S != null && S.restartRunOnDeath;

    public static int ScaleBudget(int budget)
    {
        if (S == null) return budget;
        return Mathf.Max(1, Mathf.RoundToInt(budget * S.waveBudgetMultiplier));
    }

    public static float AdjustDamage(float damage)
    {
        if (S == null || S.enemyDamageOffset == 0) return damage;
        if (S.enemyDamageOffset < 0 && damage <= 1f) return damage;
        return Mathf.Max(1f, damage + S.enemyDamageOffset);
    }
}