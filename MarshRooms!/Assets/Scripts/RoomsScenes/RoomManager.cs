// Manages enemy waves for a single room

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{   
    [Header("Settings")]
    [SerializeField] private float spawnDelay = 2f;
    [SerializeField] private float spawnIntervalMin = 0.2f;
    [SerializeField] private float spawnIntervalMax = 0.6f;

    public enum EnemyRole
    {
        Ranged,
        Erratic
    }

    private static readonly EnemyRole[][] wavePatterns = new EnemyRole[][]
    {
        new[] { EnemyRole.Ranged, EnemyRole.Erratic, EnemyRole.Ranged },
        new[] { EnemyRole.Erratic, EnemyRole.Erratic, EnemyRole.Ranged },
        new[] { EnemyRole.Ranged, EnemyRole.Ranged },
        new[] { EnemyRole.Erratic, EnemyRole.Ranged, EnemyRole.Erratic },
        new[] { EnemyRole.Erratic },
        new[] { EnemyRole.Ranged, EnemyRole.Erratic },
    };

    [System.Serializable]
    public class Wave
    {
        public int pointBudget;
        public Transform[] spawnPoints;
    }

    [Header("Waves")]
    [SerializeField] private Wave[] waves;

    [Header("Enemies")]
    [SerializeField] private EnemyEntry[] enemyTypes;

    [Header("Elites")]
    [SerializeField, Range(0f, 1f)] private float eliteChance = 0f;
    [SerializeField] private int maxElitesPerWave = 1;

    [Header("Exploding")]
    [SerializeField, Range(0f, 1f)] private float explodeChance = 0f;
    [SerializeField] private int maxExplodersPerWave = 1;

    [Header("Weapon Drops")]
    [SerializeField] private int minWeaponDrops;
    [SerializeField] private int maxWeaponDrops;

    [Header("References")]
    [SerializeField] private RoomDropZone dropZone;

    private int weaponDropsThisWave = 0;
    [SerializeField] private int maxWeaponDropsPerWave = 1;

    private int weaponDropsThisFloor = 0;

    private int enemiesRemainingInFloor = 0;

    private Transform player;

    public static event System.Action OnRoomCleared;

    [System.Serializable]
    public class EnemyEntry
    {
        public GameObject prefab;
        public int cost;
        public EnemyRole role;

        [Header("Elite Variant")]
        public GameObject eliteVariant;

        [Header("Exploding Variant")]
        public GameObject explodingVariant;
    }

    private int currentWave = -1;

    // -- START --
    private void Start()
    {
        weaponDropsThisWave = 0;
        weaponDropsThisFloor = 0;
        player = GameObject.FindWithTag("Player").transform;
        EnemyManager.OnAllEnemiesDead += OnWaveCleared;
    }

    // -- ON DESTROY --
    private void OnDestroy()
    {
        EnemyManager.OnAllEnemiesDead -= OnWaveCleared;
    }

    // -- WAVE CLEARED --
    private void OnWaveCleared()
    {
        weaponDropsThisWave = 0;
        currentWave++;

        if (currentWave >= waves.Length)
        {
            EnemyManager.OnAllEnemiesDead -= OnWaveCleared;
            OnRoomCleared?.Invoke();
            return;
        }

        SpawnWave(waves[currentWave]);
    }

    // -- SPAWN WAVE --
    private void SpawnWave(Wave wave)
    {
        StartCoroutine(SpawnWaveCoroutine(wave));
    }

    // -- SPAWN WAVE COROUTINE --
    private IEnumerator SpawnWaveCoroutine(Wave wave)
    {
        yield return new WaitForSeconds(spawnDelay);

        List<GameObject> enemiesToSpawn = GenerateEnemies(wave.pointBudget);
        enemiesRemainingInFloor += enemiesToSpawn.Count;

        List<Transform> sortedPoints = new List<Transform>(wave.spawnPoints);
        sortedPoints.Sort((a, b) =>
        {
            float distA = Vector2.Distance(a.position, player.position);
            float distB = Vector2.Distance(b.position, player.position);
            return distA.CompareTo(distB);
        });

        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
            if (i >= sortedPoints.Count) break;

            EnemyManager.Instance.SpawnEnemy(enemiesToSpawn[i], sortedPoints[i].position);
            yield return new WaitForSeconds(Random.Range(spawnIntervalMin, spawnIntervalMax));
        }
    }

    // -- GENERATE ENEMIES --
    private List<GameObject> GenerateEnemies(int budget)
    {
        List<EnemyEntry> chosenEntries = new List<EnemyEntry>();

        EnemyRole[] pattern = ChoosePattern(budget);

        if (pattern != null)
        {
            foreach (EnemyRole role in pattern)
            {
                EnemyEntry entry = PickAffordableEntry(role, budget);
                if (entry == null) break;

                chosenEntries.Add(entry);
                budget -= entry.cost;
            }
        }

        FillRemainingBudget(chosenEntries, ref budget);

        return ApplyWaveMods(chosenEntries);
    }

    // -- CHOOSE PATTERN --
    private EnemyRole[] ChoosePattern(int budget)
    {
        List<EnemyRole[]> eligible = new List<EnemyRole[]>();

        foreach (EnemyRole[] pattern in wavePatterns)
        {
            if (CanAffordPattern(pattern, budget))
                eligible.Add(pattern);
        }

        if (eligible.Count == 0) return null;

        return eligible[Random.Range(0, eligible.Count)];
    }

    // -- CAN AFFORD PATTERN --
    private bool CanAffordPattern(EnemyRole[] pattern, int budget)
    {
        int total = 0;

        foreach (EnemyRole role in pattern)
        {
            int cheapest = GetCheapestCost(role);
            if (cheapest < 0) return false;

            total += cheapest;
        }

        return total <= budget;
    }

    // -- GET CHEAPEST COST FOR ROLE --
    private int GetCheapestCost(EnemyRole role)
    {
        int cheapest = -1;

        foreach (EnemyEntry entry in enemyTypes)
        {
            if (entry.role != role) continue;
            if (cheapest == -1 || entry.cost < cheapest)
                cheapest = entry.cost;
        }

        return cheapest;
    }

    // -- PICK AFFORDABLE ENTRY OF ROLE --
    private EnemyEntry PickAffordableEntry(EnemyRole role, int budget)
    {
        List<EnemyEntry> candidates = new List<EnemyEntry>();

        foreach (EnemyEntry entry in enemyTypes)
        {
            if (entry.role == role && entry.cost <= budget)
                candidates.Add(entry);
        }

        if (candidates.Count == 0) return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    // -- FILL REMAINING BUDGET --
    private void FillRemainingBudget(List<EnemyEntry> chosenEntries, ref int budget)
    {
        while (budget > 0)
        {
            List<EnemyEntry> affordable = new List<EnemyEntry>();

            foreach (EnemyEntry entry in enemyTypes)
            {
                if (entry.cost <= budget)
                    affordable.Add(entry);
            }

            if (affordable.Count == 0) break;

            EnemyEntry pick = affordable[Random.Range(0, affordable.Count)];
            chosenEntries.Add(pick);
            budget -= pick.cost;
        }
    }

    // -- APPLY WAVE MODS --
    private List<GameObject> ApplyWaveMods(List<EnemyEntry> chosenEntries)
    {
        List<GameObject> result = new List<GameObject>();
        foreach (EnemyEntry entry in chosenEntries)
            result.Add(entry.prefab);

        HashSet<int> promotedIndices = new HashSet<int>();

        // -- Elite pass --
        if (eliteChance > 0f && maxElitesPerWave > 0)
        {
            List<int> eliteEligible = new List<int>();
            for (int i = 0; i < chosenEntries.Count; i++)
            {
                if (chosenEntries[i].eliteVariant != null)
                    eliteEligible.Add(i);
            }
            Shuffle(eliteEligible);

            int elitePromotions = 0;
            foreach (int index in eliteEligible)
            {
                if (elitePromotions >= maxElitesPerWave) break;

                if (Random.value <= eliteChance)
                {
                    result[index] = chosenEntries[index].eliteVariant;
                    promotedIndices.Add(index);
                    elitePromotions++;
                }
            }
        }

        // -- Exploding pass --
        if (explodeChance > 0f && maxExplodersPerWave > 0)
        {
            List<int> explodeEligible = new List<int>();
            for (int i = 0; i < chosenEntries.Count; i++)
            {
                if (promotedIndices.Contains(i)) continue;
                if (chosenEntries[i].explodingVariant != null)
                    explodeEligible.Add(i);
            }
            Shuffle(explodeEligible);

            int explodePromotions = 0;
            foreach (int index in explodeEligible)
            {
                if (explodePromotions >= maxExplodersPerWave) break;

                if (Random.value <= explodeChance)
                {
                    result[index] = chosenEntries[index].explodingVariant;
                    explodePromotions++;
                }
            }
        }

        return result;
    }

    // -- SHUFFLE --
    private void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swap = Random.Range(0, i + 1);
            (list[i], list[swap]) = (list[swap], list[i]);
        }
    }

    // -- TRY TO DROP WEAPON --
    public void TryDropWeapon(Vector2 position, GameObject weaponPrefab, float dropChance)
    {
        if (weaponDropsThisFloor >= maxWeaponDrops) return;
        if (weaponDropsThisWave >= maxWeaponDropsPerWave) return;
        if (Random.value > dropChance) return;

        Vector2 safePos = dropZone != null ? dropZone.GetSafeDropPosition(position) : position;
        Instantiate(weaponPrefab, safePos, Quaternion.identity);
        weaponDropsThisFloor++;
        weaponDropsThisWave++;
    }

    // -- SAFE DROP POSITION --
    public Vector2 GetSafeDropPosition(Vector2 position)
    {
        return dropZone != null ? dropZone.GetSafeDropPosition(position) : position;
    }
}