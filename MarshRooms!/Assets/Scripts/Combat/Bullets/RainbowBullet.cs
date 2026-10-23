using UnityEngine;
using TopDown.Movement;

public class RainbowBullet : BaseBullet
{
    [Header("Laser Settings")]
    [SerializeField] private float maxRange = 10f;

    [Header("Mutated Settings")]
    [SerializeField] private float mutatedRangeMultiplier = 1.5f;
    [SerializeField] private float mutatedDamageMultiplier = 1.5f;

    [Header("Beam Visual")]
    [SerializeField] private float beamLifetime = 0f;
    [SerializeField] private float minLifetime = 0.1f;
    [SerializeField] private float spriteWorldWidth = 0f;

    protected override void Start()
    {
        base.Start();

        bool mutated = SporeManager.Instance != null && SporeManager.Instance.IsMutated;

        float currentRange = mutated ? maxRange * mutatedRangeMultiplier : maxRange;
        float currentDamage = mutated ? damage * mutatedDamageMultiplier : damage;

        int layerMask = Physics2D.GetLayerCollisionMask(gameObject.layer);
        RaycastHit2D[] hits = Physics2D.RaycastAll(aimOrigin, direction, currentRange, layerMask);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        float beamLength = currentRange;

        foreach (var hit in hits)
        {
            if (BreakableUtil.IgnoresBullets(hit.collider)) continue;

            BaseHealth health = hit.collider.GetComponentInParent<BaseHealth>();

            if (health != null)
            {
                health.TakeDamage(currentDamage);

                BaseMover mover = hit.collider.GetComponentInParent<BaseMover>();
                if (mover != null)
                    mover.ApplyKnockback(direction * knockback);

                VFXManager.Instance.SpawnHitVFX(hitVFX, hit.point, weaponSortingOrder);
            }
            else
            {
                BreakableUtil.TryBreak(hit.collider);
                beamLength = hit.distance;
                break;
            }
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float spriteWidth = spriteWorldWidth > 0f
            ? spriteWorldWidth
            : (sr != null && sr.sprite != null ? sr.sprite.bounds.size.x : 0f);

        if (spriteWidth > 0f)
            transform.localScale = new Vector3(beamLength / spriteWidth, transform.localScale.y, 1f);

        Destroy(gameObject, GetBeamLifetime());
    }

    // -- GET BEAM LIFETIME --
    private float GetBeamLifetime()
    {
        if (beamLifetime > 0f) return Mathf.Max(beamLifetime, minLifetime);

        float longest = 0f;
        Animator anim = GetComponent<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
                longest = Mathf.Max(longest, clip.length);
        }

        return Mathf.Max(longest, minLifetime);
    }

    protected override void Update() { }
}