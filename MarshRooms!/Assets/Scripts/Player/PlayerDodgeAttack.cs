using System.Collections.Generic;
using UnityEngine;
using TopDown.Movement;

public class PlayerDodgeAttack : MonoBehaviour
{
    [Header("Unlock")]
    [SerializeField] private bool unlocked = false;

    [Header("Damage")]
    [SerializeField] private float damage = 3f;
    [SerializeField] private float hitRadius = 0.9f;
    [SerializeField] private float knockback = 4f;
    [SerializeField] private Vector3 centerOffset = new Vector3(0f, 0.5f, 0f);

    [Header("VFX")]
    [SerializeField] private ParticleSystem trailParticles;
    [SerializeField] private GameObject hitVFX;
    [SerializeField] private GameObject dodgeAttackVFX;
    [SerializeField] private int hitVFXSortingOrder = 50;

    [Header("Audio")]
    [SerializeField] private AudioClip hitClip;
    [Range(0f, 1f)] [SerializeField] private float hitVolume = 1f;

    public bool IsUnlocked => unlocked;

    private PlayerMover mover;
    private bool wasDodging;
    private readonly HashSet<BaseHealth> hitThisDodge = new HashSet<BaseHealth>();

    private Vector3 Center => transform.position + centerOffset;

    // -- AWAKE --
    private void Awake()
    {
        mover = GetComponent<PlayerMover>();
    }

    // -- UNLOCK --
    public void SetUnlocked(bool value)
    {
        unlocked = value;
    }
    
    // -- UPDATE --
    private void Update()
    {
        if (mover == null) return;

        bool isDodging = mover.IsDodging;

        if (isDodging && !wasDodging)
        {
            hitThisDodge.Clear();    
        }

        if (unlocked)
        {
            if (isDodging && !wasDodging) trailParticles?.Play();
            if (!isDodging && wasDodging) trailParticles?.Stop();

            if (isDodging) DealDamageAroundMarsh();
            if (isDodging) SpawnDodgeVFX();
        }

        wasDodging = isDodging;
    }

    private void SpawnDodgeVFX()
    {
        if (dodgeAttackVFX == null) return;

        Instantiate(dodgeAttackVFX, Center, Quaternion.identity);
    }

    private void DealDamageAroundMarsh()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(Center, hitRadius);

        foreach (Collider2D hit in hits)
        {
            if (!DamageTargets.TryGetEnemyHealth(hit, out BaseHealth health)) continue;
            if (health.IsDead() || !hitThisDodge.Add(health)) continue;

            health.TakeDamage(damage);

            BaseMover targetMover = hit.GetComponentInParent<BaseMover>();
            if (targetMover != null)
            {
                Vector2 dir = ((Vector2)targetMover.transform.position - (Vector2)Center).normalized;
                targetMover.ApplyKnockback(dir * knockback);
            }

            PlayHitFeedback(hit.transform.position);
        }
    }

    private void PlayHitFeedback(Vector2 position)
    {
        AudioManager.Instance?.PlaySFXWithPitch(hitClip, hitVolume, 0.1f);

        if (hitVFX != null)
            VFXManager.Instance?.SpawnHitVFX(hitVFX, position, hitVFXSortingOrder);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Center, hitRadius);
    }
}