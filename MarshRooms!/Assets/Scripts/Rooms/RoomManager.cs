// Manages enemy waves for a single room

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{   
    [Header("Settings")]
    [SerializeField] private float spawnDelay = 1f;
    [SerializeField] private float spawnIntervalMin = 0.2f;
    [SerializeField] private float spawnIntervalMax = 0.6f;

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

    [System.Serializable]
    public class EnemyEntry
    {
        public GameObject prefab;
        public int cost;
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
            Debug.Log("Room cleared!");
            EnemyManager.OnAllEnemiesDead -= OnWaveCleared;
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
        List<GameObject> generated = new List<GameObject>();

        while (budget > 0)
        {
            // Shuffle enemy types
            EnemyEntry entry = enemyTypes[Random.Range(0, enemyTypes.Length)];

            if (budget - entry.cost >= 0)
            {
                generated.Add(entry.prefab);
                budget -= entry.cost;
            }
            else
            {
                break;
            }
        }

        return generated;
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