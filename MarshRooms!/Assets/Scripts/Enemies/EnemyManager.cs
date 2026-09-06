// Handles enemy spawning and tracking

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    private List<GameObject> activeEnemies = new List<GameObject>();

    public static event System.Action OnAllEnemiesDead;

    // -- AWAKE --
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // -- SPAWN ENEMY --
    public GameObject SpawnEnemy(GameObject enemyPrefab, Vector2 position, bool playSpawnAnimation = true)
    {
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);

        if (playSpawnAnimation)
            enemy.GetComponent<EnemyController>()?.SetShouldSpawnAnimate();

        RegisterEnemy(enemy);
        return enemy;
    }

    // -- REGISTER ENEMY --
    public void RegisterEnemy(GameObject enemy)
    {
        if (!activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
    }

    // -- UNREGISTER ENEMY -- called when an enemy dies
    public void UnregisterEnemy(GameObject enemy)
    {
        activeEnemies.Remove(enemy);

        if (activeEnemies.Count == 0)
            OnAllEnemiesDead?.Invoke();
    }

    // -- GET ACTIVE ENEMY COUNT --
    public int GetActiveEnemyCount()
    {
        return activeEnemies.Count;
    } 
}