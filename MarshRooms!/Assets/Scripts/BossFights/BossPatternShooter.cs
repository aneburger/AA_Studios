// Spawns a weapon's bullets in a shape

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPatternShooter : MonoBehaviour
{
    [SerializeField] private Transform origin;

    public event System.Action<GameObject> BulletSpawned;

    private struct PendingKnife
    {
        public BaseBullet bullet;
        public float speed;
        public float damage;
    }

    private readonly List<KnifePatternData.PatternPoint> points = new List<KnifePatternData.PatternPoint>();
    private int wallMask;
    private SpriteRenderer sortingReference;

    // -- AWAKE --
    private void Awake()
    {
        wallMask = LayerMask.GetMask("Walls");
        sortingReference = GetComponentInChildren<SpriteRenderer>();

        if (origin == null)
        {
            WeaponAimer aimer = GetComponentInChildren<WeaponAimer>();
            origin = aimer != null ? aimer.transform : transform;
        }
    }

    // -- THROW --
    public Coroutine Throw(WeaponData weapon, KnifePatternData pattern, Vector2 aimDirection,
        float speedMultiplier, float countMultiplier, float hangMultiplier, System.Action onSpawned)
    {
        return StartCoroutine(ThrowRoutine(weapon, pattern, aimDirection, speedMultiplier, countMultiplier, hangMultiplier, onSpawned));
    }

    public void StopAll()
    {
        StopAllCoroutines();
    }

    private IEnumerator ThrowRoutine(WeaponData weapon, KnifePatternData pattern, Vector2 aimDirection,
        float speedMultiplier, float countMultiplier, float hangMultiplier, System.Action onSpawned)
    {
        if (weapon == null || pattern == null || weapon.bulletPrefab == null)
        {
            onSpawned?.Invoke();
            yield break;
        }

        pattern.BuildPoints(aimDirection, countMultiplier, points);

        Vector2 center = origin.position;
        int sortingOrder = sortingReference != null ? sortingReference.sortingOrder + 100 : 0;
        float baseSpeed = weapon.bulletSpeed * speedMultiplier * pattern.speedMultiplier;
        float hang = pattern.hangTime * hangMultiplier;
        bool hangs = hang > 0f;

        List<PendingKnife> knives = new List<PendingKnife>(points.Count);

        foreach (KnifePatternData.PatternPoint p in points)
        {
            Vector2 spawnPos = center + p.offset;
            if (Physics2D.OverlapPoint(spawnPos, wallMask) != null) continue;

            GameObject go = Instantiate(weapon.bulletPrefab, spawnPos, Quaternion.identity);
            BaseBullet bullet = go.GetComponent<BaseBullet>();
            if (bullet == null)
            {
                Destroy(go);
                continue;
            }

            float damage = Random.Range(weapon.minDamage, weapon.maxDamage);
            float speed = baseSpeed * p.speedFactor;

            bullet.SetDirection(p.direction);
            bullet.SetAimOrigin(center);
            bullet.SetBullet(hangs ? 0f : speed, damage, weapon.hitKnockback, weapon.hitPrefab, sortingOrder, weapon.wallHitClip, weapon.wallHitVolume);

            BulletSpawned?.Invoke(go);
            knives.Add(new PendingKnife { bullet = bullet, speed = speed, damage = damage });
        }

        PlayThrowSound(weapon);
        onSpawned?.Invoke();

        if (!hangs) yield break;

        // Hang in formation so the shape can be read, then launch
        yield return new WaitForSeconds(hang);

        foreach (PendingKnife k in knives)
        {
            if (k.bullet != null)
                k.bullet.SetBullet(k.speed, k.damage, weapon.hitKnockback, weapon.hitPrefab, sortingOrder, weapon.wallHitClip, weapon.wallHitVolume);

            if (pattern.launchStagger > 0f)
                yield return new WaitForSeconds(pattern.launchStagger);
        }
    }

    private void PlayThrowSound(WeaponData weapon)
    {
        AudioClip clip = (weapon.shootClips != null && weapon.shootClips.Length > 0)
            ? weapon.shootClips[Random.Range(0, weapon.shootClips.Length)]
            : weapon.shootClip;

        AudioManager.Instance?.PlaySFXWithPitch(clip, weapon.shootVolume, 0.1f);
    }
}