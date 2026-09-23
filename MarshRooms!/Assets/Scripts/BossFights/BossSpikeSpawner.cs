// Spawns a batch of spikes that target the player without ever overlapping each other.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSpikeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] spikePrefabs;
    [SerializeField] private float minSpacing = 1f;
    [SerializeField] private int placementAttemptsPerSpike = 12;
    [SerializeField] private float spreadRadius = 1.2f;

    public event System.Action<GameObject> SpikeSpawned;
    private readonly List<GameObject> activeSpikes = new List<GameObject>();

    // Waits until every spike spawned so far
    public IEnumerator WaitForAllSpikesFinished()
    {
        while (true)
        {
            activeSpikes.RemoveAll(s => s == null);
            if (activeSpikes.Count == 0) yield break;
            yield return null;
        }
    }

    public Coroutine SpawnRounds(int countPerRound, int rounds, float warningTime, float roundInterval, Transform playerTransform)
    {
        return StartCoroutine(SpawnRoundsRoutine(countPerRound, rounds, warningTime, roundInterval, playerTransform));
    }

    private IEnumerator SpawnRoundsRoutine(int countPerRound, int rounds, float warningTime, float roundInterval, Transform playerTransform)
    {
        for (int i = 0; i < rounds; i++)
        {
            if (playerTransform == null) yield break;

            SpawnBatch(countPerRound, warningTime, playerTransform.position);

            if (i < rounds - 1)
                yield return new WaitForSeconds(roundInterval);
        }
    }

    // Spawns `count` spikes clustered near the player
    public void SpawnBatch(int count, float warningTime, Vector2 playerPosition)
    {
        List<Vector2> placed = new List<Vector2>();

        for (int i = 0; i < count; i++)
        {
            if (!TryFindSpot(playerPosition, placed, out Vector2 spot)) continue;

            placed.Add(spot);
            SpawnOne(spot, warningTime);
        }
    }

    private bool TryFindSpot(Vector2 center, List<Vector2> placed, out Vector2 result)
    {
        for (int attempt = 0; attempt < placementAttemptsPerSpike; attempt++)
        {
            Vector2 candidate = center + Random.insideUnitCircle * spreadRadius;

            if (!Overlaps(candidate, placed))
            {
                result = candidate;
                return true;
            }
        }

        result = center;
        return false;
    }

    private bool Overlaps(Vector2 candidate, List<Vector2> placed)
    {
        foreach (Vector2 p in placed)
        {
            if (Vector2.Distance(candidate, p) < minSpacing)
                return true;
        }

        return false;
    }

    private void SpawnOne(Vector2 position, float warningTime)
    {
        if (spikePrefabs == null || spikePrefabs.Length == 0) return;

        GameObject prefab = spikePrefabs[Random.Range(0, spikePrefabs.Length)];
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, position, Quaternion.identity);
        SpikeHazard hazard = go.GetComponent<SpikeHazard>();

        if (hazard != null)
        {
            activeSpikes.Add(go);
            hazard.Begin(warningTime);
        }

        SpikeSpawned?.Invoke(go);
    }
}