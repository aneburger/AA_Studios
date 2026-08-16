// Spawns splitPrefab x splitCount when this enemy dies.

using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class SplitOnDeath : MonoBehaviour
{
    [Header("Split")]
    [SerializeField] private GameObject splitPrefab;
    [SerializeField] private int splitCount = 2;
    [SerializeField] private float scatterRadius = 0.6f;

    [Header("Elite Inheritance")]
    [SerializeField] private GameObject eliteSplitPrefab;

    private EliteModifier eliteModifier;
    private EnemyHealth health;

    // -- AWAKE --
    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        eliteModifier = GetComponent<EliteModifier>();
    }

    // -- ENABLE --
    private void OnEnable()
    {
        health.OnDied += HandleDied;
    }

    // -- DISABLE --
    private void OnDisable()
    {
        health.OnDied -= HandleDied;
    }

    // -- SET SPLIT COUNT --
    public void SetSplitCount(int count)
    {
        splitCount = Mathf.Max(0, count);
    }

    // -- HANDLE DIED --
    private void HandleDied(Vector2 deathPosition)
    {
        if (splitPrefab == null || splitCount <= 0) return;

        GameObject prefabToSpawn = splitPrefab;
        if (eliteSplitPrefab != null && eliteModifier != null)
            prefabToSpawn = eliteSplitPrefab;

        RoomManager room = FindFirstObjectByType<RoomManager>();

        for (int i = 0; i < splitCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * scatterRadius;
            Vector2 spawnPos = deathPosition + offset;

            if (room != null)
                spawnPos = room.GetSafeDropPosition(spawnPos);

            EnemyManager.Instance.SpawnEnemy(prefabToSpawn, spawnPos, playSpawnAnimation: false);
        }
    }
}