// Spawns a wave of minions with the normal enemy spawn animation, and guarantees a weapon drop
// from among them if none has appeared yet this fight. Put this anywhere in the boss scene.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossMinionSpawner : MonoBehaviour
{
    public event System.Action<GameObject> MinionSpawned;

    private bool hasForcedWeaponDrop;
    private bool hasForcedHealthDrop;
    private int lastSpawnPointIndex = -1;

    // -- RESET --
    public void ResetGuarantees()
    {
        hasForcedWeaponDrop = false;
        hasForcedHealthDrop = false;
    }

    // -- SPAWN WAVE --
    public Coroutine SpawnWave(GameObject prefab, int count, Transform[] points,
        float weaponGuaranteeChance, float healthGuaranteeChance, float intervalMin, float intervalMax)
    {
        return StartCoroutine(SpawnWaveRoutine(prefab, count, points, weaponGuaranteeChance, healthGuaranteeChance, intervalMin, intervalMax));
    }

    // -- SPAWN WAVE ROUTINE --
    private IEnumerator SpawnWaveRoutine(GameObject prefab, int count, Transform[] points,
        float weaponGuaranteeChance, float healthGuaranteeChance, float intervalMin, float intervalMax)
    {
        if (prefab == null || points == null || points.Length == 0)
        {
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            Transform point = PickPoint(points);
            SpawnOne(prefab, point.position, weaponGuaranteeChance, healthGuaranteeChance);

            if (i < count - 1)
                yield return new WaitForSeconds(Random.Range(intervalMin, intervalMax));
        }
    }

    // -- PICK POINT --
    private Transform PickPoint(Transform[] points)
    {
        if (points.Length == 1) return points[0];

        int index = Random.Range(0, points.Length);
        if (index == lastSpawnPointIndex)
            index = (index + 1) % points.Length;

        lastSpawnPointIndex = index;
        return points[index];
    }

    // -- SPAWN ONE --
    private GameObject SpawnOne(GameObject prefab, Vector2 position, float weaponGuaranteeChance, float healthGuaranteeChance)
    {
        GameObject go = Instantiate(prefab, position, Quaternion.identity);

        EnemyController controller = go.GetComponent<EnemyController>();
        controller?.SetShouldSpawnAnimate();

        EnemyHealth health = go.GetComponent<EnemyHealth>();
        if (health != null)
            health.OnDied += pos => HandleMinionDied(controller, pos, weaponGuaranteeChance, healthGuaranteeChance);

        MinionSpawned?.Invoke(go);
        return go;
    }

    // -- HANDLEMINION DIED --
    private void HandleMinionDied(EnemyController controller, Vector2 position, float weaponGuaranteeChance, float healthGuaranteeChance)
    {
        if (controller == null || controller.Data == null) return;

        if (!hasForcedWeaponDrop && Random.value <= weaponGuaranteeChance)
        {
            var drops = controller.Data.possibleWeaponDrops;
            if (drops != null && drops.Length > 0)
            {
                var drop = drops[Random.Range(0, drops.Length)];
                if (drop.weaponPickupPrefab != null && RoomManager.Current != null)
                {
                    hasForcedWeaponDrop = true;
                    RoomManager.Current.ForceWeaponDrop(drop.weaponPickupPrefab, position);
                }
            }
        }

        if (!hasForcedHealthDrop && Random.value <= healthGuaranteeChance)
        {
            GameObject healthPrefab = controller.Data.healthPickupPrefab;
            if (healthPrefab != null)
            {
                Vector2 spawnPos = RoomManager.Current != null ? RoomManager.Current.GetSafeDropPosition(position) : position;
                hasForcedHealthDrop = true;
                Instantiate(healthPrefab, spawnPos, Quaternion.identity);
            }
        }
    }
}