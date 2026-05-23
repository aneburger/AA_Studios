// Manages enemy waves for a single room

using UnityEngine;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{
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
        List<GameObject> enemiesToSpawn = GenerateEnemies(wave.pointBudget);
        
        // Sort spawn points by distance to player (closest first)
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

            GameObject enemy = EnemyManager.Instance.SpawnEnemy(enemiesToSpawn[i], sortedPoints[i].position);
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
}