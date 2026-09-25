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
    public event System.Action<string> OnBoonRemoved;

    // Card rarity weight per floors
    private static readonly (float normal, float rare, float epic)[] rarityWeightsByTier = new[]
    {
        (0.70f, 0.30f, 0f),    // floors 1-2
        (0.65f, 0.35f, 0f),    // floors 3-4
        (0.60f, 0.30f, 0.10f), // floors 5-6
        (0.50f, 0.35f, 0.15f), // floors 7-8
        (0.45f, 0.35f, 0.20f), // floors 9-10
        (0.35f, 0.35f, 0.30f), // floors 11-12
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
        int newCount = current + 1;

        if (!ApplyEffect(boonId, newCount, +1))
            return; // unknown id, nothing changed

        ownedCounts[boonId] = newCount;
        OnBoonApplied?.Invoke(boonId);
    }

    // -- REMOVE BOON (one copy) --
    public void RemoveBoonById(string boonId)
    {
        if (!ownedCounts.TryGetValue(boonId, out int current) || current <= 0)
            return;

        if (!ApplyEffect(boonId, current, -1))
            return;

        ownedCounts[boonId] = current - 1;
        OnBoonRemoved?.Invoke(boonId);
        OnBoonApplied?.Invoke(boonId);
    }

    // -- APPLY/REMOVE EFFECT --
    private bool ApplyEffect(string boonId, int copyIndex, int sign)
    {
        switch (boonId)
        {
            case "bonus_heart":
                Stats.bonusMaxHearts += sign;
                ApplyHeartDelta(sign);
                break;

            case "heal_on_mutate":
                Stats.healOnMutateAmount += 4 * sign;
                break;

            case "health_drop_rate":
                Stats.healthDropRateMultiplier = sign > 0
                    ? Stats.healthDropRateMultiplier * 1.3f
                    : Stats.healthDropRateMultiplier / 1.3f;
                break;

            case "iframe_extension":
                Stats.bonusIFrameDuration += 2f * sign;
                break;

            case "dodge_damage_chance":
                Stats.dodgeDamageChance += 0.1f * sign;
                break;

            case "mutation_duration":
                Stats.bonusMutationDuration += 2f * sign;
                break;

            case "mutation_damage":
                Stats.mutationDamageBonus += (copyIndex == 1 ? 0.2f : 0.1f) * sign;
                break;

            case "mushroom_bomb":
                Stats.hasMushroomBomb = sign > 0;
                break;

            case "faster_spore_fill":
                Stats.sporeGainAmount += 1 * sign;
                break;

            case "fire_rate":
                Stats.permanentFireRateMultiplier += 0.18f * sign;
                break;

            case "crit_chance":
                Stats.critChance += 0.1f * sign;
                break;

            case "overall_damage":
                Stats.permanentDamageMultiplier += 0.2f * sign;
                break;

            case "extra_bullet":
                Stats.permanentBulletCountBonus += 1 * sign;
                break;

            default:
                Debug.LogWarning($"BoonManager: unknown boonId '{boonId}'");
                return false;
        }

        return true;
    }

    // -- APPLY HEART DELTA (+1 or -1 hearts) --
    private void ApplyHeartDelta(int sign)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        if (sign > 0)
            playerHealth.IncreaseMaxHealth(4, healToFull: false);
        else
            playerHealth.DecreaseMaxHealth(4);
    }

    // -- GET SAVE SNAPSHOT --
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
    public void RestoreFromSave(RunStats savedStats, List<BoonCountEntry> savedCounts)
    {
        Stats = savedStats ?? new RunStats();

        ownedCounts.Clear();
        if (savedCounts != null)
        {
            foreach (BoonCountEntry entry in savedCounts)
                ownedCounts[entry.boonId] = entry.count;
        }

        OnBoonApplied?.Invoke(null);
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
        int tier = Mathf.Clamp((floorNumber - 1) / 2, 0, rarityWeightsByTier.Length - 1);
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