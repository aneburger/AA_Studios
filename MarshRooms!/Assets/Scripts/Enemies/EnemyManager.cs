// Handles enemy spawning and tracking

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    private List<GameObject> activeEnemies = new List<GameObject>();
    private Coroutine confirmRoutine;

    private int spawningLocks = 0;

    public bool IsSpawning => spawningLocks > 0;
    public bool IsClearing { get; private set; }

    // Events
    public static event System.Action OnAllEnemiesDead;
    public static event System.Action OnEnemyKilled;

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

    // -- BEGIN SPAWNING --
    public void BeginSpawning()
    {
        spawningLocks++;

        if (confirmRoutine != null)
        {
            StopCoroutine(confirmRoutine);
            confirmRoutine = null;
        }
    }

    // -- END SPAWNING --
    public void EndSpawning()
    {
        spawningLocks = Mathf.Max(0, spawningLocks - 1);

        if (spawningLocks == 0 && activeEnemies.Count == 0 && confirmRoutine == null)
            confirmRoutine = StartCoroutine(ConfirmAllDead());
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

        if (confirmRoutine != null)
        {
            StopCoroutine(confirmRoutine);
            confirmRoutine = null;
        }
    }

    // -- UNREGISTER ENEMY --
    public void UnregisterEnemy(GameObject enemy)
    {
        if (activeEnemies.Remove(enemy))
            OnEnemyKilled?.Invoke();

        if (activeEnemies.Count == 0 && spawningLocks == 0 && confirmRoutine == null)
            confirmRoutine = StartCoroutine(ConfirmAllDead());
    }

    // -- CONFIRM ALL DEAD --
    private IEnumerator ConfirmAllDead()
    {
        yield return new WaitForSeconds(0.15f);

        confirmRoutine = null;

        if (activeEnemies.Count == 0 && spawningLocks == 0)
            OnAllEnemiesDead?.Invoke();
    }

    // -- GET ACTIVE ENEMY COUNT --
    public int GetActiveEnemyCount()
    {
        return activeEnemies.Count;
    } 

    // -- DESTROY ALL ENEMIES --
    public void DestroyAllEnemies()
    {
        IsClearing = true;

        foreach (GameObject enemy in new List<GameObject>(activeEnemies))
        {
            if (enemy == null) { activeEnemies.Remove(enemy); continue; }
            UnregisterEnemy(enemy);
            Destroy(enemy);
        }

        StartCoroutine(ResetClearing());
    }

    private IEnumerator ResetClearing()
    {
        yield return new WaitForSecondsRealtime(0.25f);
        IsClearing = false;
    }
}