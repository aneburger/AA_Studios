using UnityEngine;
using TopDown.Movement;

public class SporeBullet : BaseBullet
{
    [Header("Spore Settings")]
    [SerializeField] private float maxRange = 5f;
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private float maxRangeVariance = 0.3f;

    [Header("Tick Damage")]
    [SerializeField] private float tickDamage = 2f;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private float tickDuration = 3f;

    [Header("Poison Tint")]
    [SerializeField] private Color poisonTint = new Color(0.4f, 1f, 0.4f);

    [Header("Audio")]
    [SerializeField] private AudioClip explodeClip;
    [Range(0f, 1f)] public float explodeVolume;

    private Vector2 spawnPosition;
    private bool hasExploded = false;

    protected override void Start()
    {
        base.Start();
        spawnPosition = transform.position;
        maxRange += Random.Range(-maxRangeVariance, maxRangeVariance);
    }

    protected override void Update()
    {
        base.Update();

        if (Vector2.Distance(transform.position, spawnPosition) >= maxRange)
            Explode();
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyHealth enemyHealth = collision.GetComponentInParent<EnemyHealth>();
        
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);

            BaseMover mover = collision.GetComponentInParent<BaseMover>();
            if (mover != null)
                mover.ApplyKnockback(direction * knockback);
        }

        Explode();
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("EnemyPhysics"));
        filter.useTriggers = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, LayerMask.GetMask("EnemyPhysics"));
        foreach (var hit in hits)
        {
            EnemyHealth health = hit.GetComponentInParent<EnemyHealth>();
            if (health != null && !health.IsDead())
            {
                SporeTick tick = hit.GetComponentInParent<SporeTick>();
                if (tick == null)
                    tick = hit.GetComponentInParent<Transform>().gameObject.AddComponent<SporeTick>();

                tick.Apply(tickDamage, tickInterval, tickDuration, poisonTint);
            }
        }

        VFXManager.Instance.SpawnHitVFX(hitVFX, transform.position, weaponSortingOrder);
        AudioManager.Instance.PlaySFXWithPitch(explodeClip, explodeVolume, 0.1f);
        Destroy(gameObject);
    }

    // -- GIZMOS --
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}