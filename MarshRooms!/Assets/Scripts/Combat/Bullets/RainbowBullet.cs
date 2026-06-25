using UnityEngine;
using TopDown.Movement;

public class RainbowBullet : BaseBullet
{
    [Header("Laser Settings")]
    [SerializeField] private float maxRange = 10f;

    [Header("Mutated Settings")]
    [SerializeField] private float mutatedRangeMultiplier = 1.5f;
    [SerializeField] private float mutatedDamageMultiplier = 1.5f;

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
                beamLength = hit.distance;
                break;
            }
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            float spriteWidth = sr.sprite.bounds.size.x;
            transform.localScale = new Vector3(beamLength / spriteWidth, transform.localScale.y, 1f);
        }

        Animator anim = GetComponent<Animator>();
        if (anim != null)
            Destroy(gameObject, anim.GetCurrentAnimatorStateInfo(0).length);
    }

    protected override void Update() { }
}