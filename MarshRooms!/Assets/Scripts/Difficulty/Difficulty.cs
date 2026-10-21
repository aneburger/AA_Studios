using UnityEngine;

public enum Difficulty { Easy, Normal, Hard, DeathCap }

[CreateAssetMenu(menuName = "Difficulty Settings")]
public class DifficultySettings : ScriptableObject
{
    [Header("Enemies")]
    public float enemySpeedMultiplier = 1f;
    public float enemyFireRateMultiplier = 1f;
    public int enemyDamageOffset = 0;

    [Header("Waves")]
    public float waveBudgetMultiplier = 1f;
    public float eliteChanceMultiplier = 1f;

    [Header("Bosses")]
    public float bossHealthMultiplier = 1f;

    [Header("Rules")]
    public bool restartRunOnDeath = false;
}