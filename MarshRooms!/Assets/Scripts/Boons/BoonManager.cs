using UnityEngine;
using System.Collections.Generic;

public class BoonManager : MonoBehaviour
{
    public static BoonManager Instance { get; private set; }

    public RunStats Stats { get; private set; } = new RunStats();

    private Dictionary<string, int> ownedCounts = new Dictionary<string, int>();

    private static readonly Dictionary<string, int> boonCaps = new Dictionary<string, int>
    {
        { "bonus_heart", 3 },
        { "bonus_heart_full_heal", 3 },
        { "dodge_damage_chance", 3 },
        { "mutation_duration", 3 },
        { "mutation_damage", 3 },
        { "extra_bullet", 3 },
        { "faster_spore_fill", 1 },
        { "mushroom_bomb", 1 },
    };

    public event System.Action<string> OnBoonApplied;

    // -- AWAKE --
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int GetOwnedCount(string boonId)
    {
        return ownedCounts.TryGetValue(boonId, out int count) ? count : 0;
    }

    public bool CanApplyBoon(string boonId)
    {
        if (!boonCaps.TryGetValue(boonId, out int cap)) return true;
        return GetOwnedCount(boonId) < cap;
    }

    // -- APPLY BOON --
   public void ApplyBoonById(string boonId)
    {
        if (!CanApplyBoon(boonId))
        {
            Debug.LogWarning($"BoonManager: '{boonId}' is already at its cap.");
            return;
        }
        ownedCounts.TryGetValue(boonId, out int current);
        ownedCounts[boonId] = current + 1;

        switch (boonId)
        {
            case "bonus_heart":
                Stats.bonusMaxHearts++;
                ApplyBonusHeart(healToFull: false);
                break;

            case "bonus_heart_full_heal":
                Stats.bonusMaxHearts++;
                ApplyBonusHeart(healToFull: true);
                break;
            case "health_drop_rate":
                Stats.healthDropRateMultiplier += 0.5f;
                break;
            case "heal_full_chance":
                Stats.healToFullChance += 0.25f;
                break;
            case "iframe_extension":
                Stats.bonusIFrameDuration += 0.2f;
                break;
            case "dodge_damage_chance":
                Stats.dodgeDamageChance += 0.1f;
                break;
            case "mutation_duration":
                Stats.bonusMutationDuration += 2f;
                break;
            case "mutation_damage":
                Stats.mutationDamageBonus += 0.2f;
                break;
            case "mushroom_bomb":
                Stats.hasMushroomBomb = true;
                break;
            case "faster_spore_fill":
                Stats.sporeGainAmount += 1;
                break;
            case "fire_rate":
                Stats.permanentFireRateMultiplier += 0.2f;
                break;
            case "crit_chance":
                Stats.critChance += 0.1f;
                break;
            case "overall_damage":
                Stats.permanentDamageMultiplier += 0.15f;
                break;
            case "extra_bullet":
                Stats.permanentBulletCountBonus += 1;
                break;
            default:
                Debug.LogWarning($"BoonManager: unknown boonId '{boonId}'");
                return;
        }

        Debug.Log($"[BoonManager] Applied '{boonId}' (owned x{ownedCounts[boonId]})");
        OnBoonApplied?.Invoke(boonId);
    }

    private void ApplyBonusHeart(bool healToFull)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.IncreaseMaxHealth(4, healToFull: healToFull);
    }
}