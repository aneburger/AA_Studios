// Spawns a weapon's bullets in a shape that shoots outwards

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
        public Vector2 offset;
        public Vector2 direction;
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
    public Coroutine Throw(WeaponData weapon, KnifePatternData pattern, System.Func<Vector2> aimProvider,
        float speedMultiplier, float countMultiplier, float hangMultiplier, System.Action onSpawned)
    {
        return StartCoroutine(ThrowRoutine(weapon, pattern, aimProvider, speedMultiplier, countMultiplier, hangMultiplier, onSpawned));
    }

    // -- STOP ALL --
    public void StopAll()
    {
        StopAllCoroutines();
    }

    private IEnumerator ThrowRoutine(WeaponData weapon, KnifePatternData pattern, System.Func<Vector2> aimProvider,
        float speedMultiplier, float countMultiplier, float hangMultiplier, System.Action onSpawned)
    {
        if (weapon == null || pattern == null)
        {
            onSpawned?.Invoke();
            yield break;
        }

        Vector2 aim = aimProvider != null ? aimProvider() : Vector2.right;
        float spawnAngle = AngleOf(aim);

        pattern.BuildPoints(aim, countMultiplier, points);

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

            GameObject prefab = weapon.GetBulletPrefab();
            if (prefab == null) continue;

            GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
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
            knives.Add(new PendingKnife { bullet = bullet, offset = p.offset, direction = p.direction, speed = speed, damage = damage });
        }

        PlayThrowSound(weapon);
        onSpawned?.Invoke();

        if (!hangs) yield break;

        bool tracks = aimProvider != null
            && pattern.alignToPlayer
            && pattern.trackWhileHanging
            && pattern.trackTurnRate > 0f;

        float trackedAngle = spawnAngle;
        float elapsed = 0f;

        while (elapsed < hang)
        {
            elapsed += Time.deltaTime;

            if (tracks && elapsed < hang - pattern.trackLockTime)
            {
                float targetAngle = AngleOf(aimProvider());
                float step = pattern.trackTurnRate * Time.deltaTime;
                trackedAngle += Mathf.Clamp(Mathf.DeltaAngle(trackedAngle, targetAngle), -step, step);

                ApplyFormation(knives, center, trackedAngle - spawnAngle);
            }

            yield return null;
        }

        // Launch
        foreach (PendingKnife k in knives)
        {
            if (k.bullet != null)
                k.bullet.SetBullet(k.speed, k.damage, weapon.hitKnockback, weapon.hitPrefab, sortingOrder, weapon.wallHitClip, weapon.wallHitVolume);

            if (pattern.launchStagger > 0f)
                yield return new WaitForSeconds(pattern.launchStagger);
        }
    }

    // -- APPLY FORMATION --
    private static void ApplyFormation(List<PendingKnife> knives, Vector2 center, float deltaDegrees)
    {
        foreach (PendingKnife k in knives)
        {
            if (k.bullet == null) continue;

            k.bullet.transform.position = center + Rotate(k.offset, deltaDegrees);
            k.bullet.SetDirection(Rotate(k.direction, deltaDegrees));
        }
    }

    private static float AngleOf(Vector2 v)
    {
        return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void PlayThrowSound(WeaponData weapon)
    {
        AudioClip clip = (weapon.shootClips != null && weapon.shootClips.Length > 0)
            ? weapon.shootClips[Random.Range(0, weapon.shootClips.Length)]
            : weapon.shootClip;

        AudioManager.Instance?.PlaySFXWithPitch(clip, weapon.shootVolume, 0.1f);
    }
}