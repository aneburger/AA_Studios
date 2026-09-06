using UnityEngine;
using System.Collections.Generic;

public class BoonManager : MonoBehaviour
{
    public static BoonManager Instance { get; private set; }

    [Header("Boon Cards")]
    [SerializeField] private List<BoonCardData> allBoonCards;

    public RunStats Stats { get; private set; } = new RunStats();
    private Dictionary<string, int> ownedCounts = new Dictionary<string, int>();

    public event System.Action<string> OnBoonApplied;

    // Card rarity weight per floors
    private static readonly (float normal, float rare, float epic)[] rarityWeightsByTier = new[]
    {
        (0.60f, 0.30f, 0.10f), // floors 1-2
        (0.60f, 0.275f, 0.125f), // floors 3-4
        (0.50f, 0.30f, 0.20f), // floors 5-6
    };

    // -- AWAKE --
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // -- GET OWNED COUNT --
    public int GetOwnedCount(string boonId)
    {
        return ownedCounts.TryGetValue(boonId, out int count) ? count : 0;
    }

    // -- CAN APPLY BOON --
    public bool CanApplyBoon(string boonId)
    {
        BoonCardData card = allBoonCards.Find(c => c.boonId == boonId);
        if (card == null || card.maxCopies < 0) return true;
        return GetOwnedCount(boonId) < card.maxCopies;
    }

    // -- APPLY BOON --
    public void ApplyBoonById(string boonId)
    {
        if (!CanApplyBoon(boonId))
        {
            Debug.LogWarning($"BoonManager: '{boonId}' is already at its cap, ignoring.");
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
                Stats.healthDropRateMultiplier *= 2f;
                break;

            case "heal_full_chance":
                Stats.healToFullChance += 0.2f;
                break;

            case "iframe_extension":
                Stats.bonusIFrameDuration += 2f;
                break;

            case "dodge_damage_chance":
                Stats.dodgeDamageChance += 0.1f;
                break;

            case "mutation_duration":
                Stats.bonusMutationDuration += 2f;
                break;

            case "mutation_damage":
                if (ownedCounts["mutation_damage"] == 1)
                    Stats.mutationDamageBonus += 0.2f;
                else
                    Stats.mutationDamageBonus += 0.1f;
                break;

            case "mushroom_bomb":
                Stats.hasMushroomBomb = true;
                break;

            case "faster_spore_fill":
                Stats.sporeGainAmount += 1;
                break;

            case "fire_rate":
                Stats.permanentFireRateMultiplier += 0.18f;
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

        //.Log($"[BoonManager] Applied '{boonId}' (owned x{ownedCounts[boonId]})");
        OnBoonApplied?.Invoke(boonId);
    }

    // -- GET SAVE SNAPSHOT --
    // Returns the owned boon counts as a serializable list.
    public List<BoonCountEntry> GetOwnedCountsSnapshot()
    {
        List<BoonCountEntry> list = new List<BoonCountEntry>();
        foreach (KeyValuePair<string, int> kvp in ownedCounts)
        {
            list.Add(new BoonCountEntry { boonId = kvp.Key, count = kvp.Value });
        }
        return list;
    }

    // -- RESTORE FROM SAVE --
    // Restores Stats and ownedCounts wholesale rather than replaying ApplyBoonById.
    public void RestoreFromSave(RunStats savedStats, List<BoonCountEntry> savedCounts)
    {
        Stats = savedStats ?? new RunStats();

        ownedCounts.Clear();
        if (savedCounts != null)
        {
            foreach (BoonCountEntry entry in savedCounts)
                ownedCounts[entry.boonId] = entry.count;
        }

        // Reuse the existing event so PlayerBoonStats.ApplyAllStats() re-syncs
        // shooter/spore/health bonuses to the restored stats.
        OnBoonApplied?.Invoke(null);
    }

    // -- APPLY BONUS HEART --
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

    public List<BoonCardData> GetThreeCardOffers(int floorNumber)
    {
        List<BoonCardData> offers = new List<BoonCardData>();
        List<BoonCardData> excluded = new List<BoonCardData>();

        for (int i = 0; i < 3; i++)
        {
            BoonRarity rolledRarity = RollRarity(floorNumber);
            BoonCardData card = PickCardOfRarity(rolledRarity, excluded);

            if (card == null)
            {
                foreach (BoonRarity fallback in new[] { BoonRarity.Normal, BoonRarity.Rare, BoonRarity.Epic })
                {
                    card = PickCardOfRarity(fallback, excluded);
                    if (card != null) break;
                }
            }

            if (card != null)
            {
                offers.Add(card);
                excluded.Add(card);
            }
        }

        return offers;
    }

    private BoonRarity RollRarity(int floorNumber)
    {
        int tier = floorNumber <= 2 ? 0 : (floorNumber <= 4 ? 1 : 2);
        var weights = rarityWeightsByTier[tier];

        float roll = Random.value;
        if (roll <= weights.normal) return BoonRarity.Normal;
        if (roll <= weights.normal + weights.rare) return BoonRarity.Rare;
        return BoonRarity.Epic;
    }

    private BoonCardData PickCardOfRarity(BoonRarity rarity, List<BoonCardData> excluded)
    {
        List<BoonCardData> candidates = allBoonCards.FindAll(c =>
            c.rarity == rarity &&
            !excluded.Contains(c) &&
            CanApplyBoon(c.boonId)
        );

        if (candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)];
    }
}










