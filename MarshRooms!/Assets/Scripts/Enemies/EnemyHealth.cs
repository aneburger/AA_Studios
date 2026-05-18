// Enemy health, extends BaseHealth
// Handles enemy death, despawn and drops

using UnityEngine;

public class EnemyHealth : BaseHealth
{
    [Header("Audio")]
    [SerializeField] private AudioClip hurtClip;
    [Range(0f, 1f)] public float hurtVolume;
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Die()
    {
        base.Die();
        SpawnSpores();
        // Destroy game object for now
        Destroy(gameObject);
    }

    // -- SPAWN SPORES --
    private void SpawnSpores()
    {
        EnemyController controller = GetComponent<EnemyController>();
        if (controller == null) return;

        int amount = Random.Range(0, controller.Data.maxSporeDrops + 1);
        for (int i = 0; i < amount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 0.5f;
            Instantiate(controller.Data.sporePrefab, (Vector2)transform.position + offset, Quaternion.identity);
        }
    }


    // -- HIT EFFECT --
    protected override void OnHitEffect()
    {
        base.OnHitEffect();
        AudioManager.Instance.PlaySFX(hurtClip, hurtVolume);
    }
}