// Enemy health, extends BaseHealth
// Handles enemy death, despawn and drops

using UnityEngine;

public class EnemyHealth : BaseHealth
{
    [Header("Audio")]
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private float hurtVolume = 1f;
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Die()
    {
        base.Die();

        // Destroy game object for now
        Destroy(gameObject);
    }

    // -- HIT EFFECT --
    protected override void OnHitEffect()
    {
        base.OnHitEffect();
        AudioManager.Instance.PlaySFX(hurtClip, hurtVolume);
    }
}