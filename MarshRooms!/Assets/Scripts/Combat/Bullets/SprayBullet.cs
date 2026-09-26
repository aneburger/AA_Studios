// Adds random angle + speed jitter at spawn, for a messier spray effect.

using UnityEngine;

public class SprayBullet : BaseBullet
{
    [SerializeField] private float angleJitter = 12f;
    [SerializeField] private float minSpeedMultiplier = 0.85f;
    [SerializeField] private float maxSpeedMultiplier = 1.15f;

    private bool jittered;

    public override void SetDirection(Vector2 dir)
    {
        if (!jittered)
        {
            jittered = true;
            float angle = Random.Range(-angleJitter, angleJitter);
            dir = Rotate(dir, angle);
        }

        base.SetDirection(dir);
    }

    public override void SetBullet(float speed, float damage, float knockback, GameObject hitVFX, int sortingOrder, AudioClip wallHitClip = null, float wallHitVolume = 0.7f)
    {
        float multiplier = Random.Range(minSpeedMultiplier, maxSpeedMultiplier);
        base.SetBullet(speed * multiplier, damage, knockback, hitVFX, sortingOrder, wallHitClip, wallHitVolume);
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}