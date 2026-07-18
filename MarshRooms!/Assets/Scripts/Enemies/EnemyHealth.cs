// Enemy health, extends BaseHealth
// Handles enemy death, despawn and drops

using UnityEngine;
using System.Collections;

public class EnemyHealth : BaseHealth
{   
    [Header("Health Bar")]
    [SerializeField] private EnemyHealthBar healthBar;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtClip;
    [Range(0f, 1f)] public float hurtVolume;
    [SerializeField] private AudioClip dieClip;
    [Range(0f, 1f)] public float dieVolume;

    public event System.Action<Vector2> OnDied;

    // -- AWAKE --
    protected override void Awake()
    {
        base.Awake();
    }

    // -- SPAWN SPORES --
    private void SpawnSpores()
    {
        EnemyController controller = GetComponent<EnemyController>();
        if (controller == null) return;

        RoomManager room = FindFirstObjectByType<RoomManager>();

        int amount = controller.Data.guaranteesSporeDrop
            ? Random.Range(1, controller.Data.maxSporeDrops + 1)
            : Random.Range(0, controller.Data.maxSporeDrops + 1);

        for (int i = 0; i < amount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 0.5f;
            Vector2 spawnPos = (Vector2)transform.position + offset;

            if (room != null)
                spawnPos = room.GetSafeDropPosition(spawnPos);

            Instantiate(controller.Data.sporePrefab, spawnPos, Quaternion.identity);
        }
    }
    
    // -- TAKE DAMAGE --
    public override void TakeDamage(float amount)
    {
        EnemyController controller = GetComponent<EnemyController>();
        if (controller != null && controller.IsSpawning) return;

        base.TakeDamage(amount);
        healthBar?.UpdateHealth(currentHealth, maxHealth);

        Vector2 topPosition = GetTopPosition();
        VFXManager.Instance.SpawnDamageNumber(amount * 10, topPosition);
    }

     // -- TOP POSITION --
    private Vector2 GetTopPosition()
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            return new Vector2(transform.position.x, sr.bounds.max.y);

        return transform.position;
    }

    // -- HIT EFFECT --
    protected override void OnHitEffect()
    {
        if (IsDead()) return;
        AudioManager.Instance.PlaySFXWithPitch(hurtClip, hurtVolume, 0.2f);
    }

    // -- DIE --
    protected override void Die()
    {
        base.Die();
        AudioManager.Instance.PlaySFX(dieClip, dieVolume);
        EnemyManager.Instance.UnregisterEnemy(gameObject);
        VFXManager.Instance.SpawnEnemyExplosion(transform.position);

        OnDied?.Invoke(transform.position);

        EnemyController controller = GetComponent<EnemyController>();
        RoomManager room = FindFirstObjectByType<RoomManager>();

        if (controller != null)
        {
            // Weapon drop
            if (controller.Data.possibleWeaponDrops.Length > 0 && room != null)
            {
                EnemyData.WeaponDrop drop = controller.Data.possibleWeaponDrops[Random.Range(0, controller.Data.possibleWeaponDrops.Length)];
                room.TryDropWeapon(transform.position, drop.weaponPickupPrefab, drop.dropChance);
            }

           // Health drop
            float healthDropChance = controller.Data.healthDropChance * BoonManager.Instance.Stats.healthDropRateMultiplier;
            float roll = Random.value;
            bool willDrop = controller.Data.healthPickupPrefab != null && roll <= healthDropChance;

            if (willDrop)
            {
                Vector2 offset = Random.insideUnitCircle * 0.8f;
                Vector2 safePos = room != null ? room.GetSafeDropPosition((Vector2)transform.position + offset) : (Vector2)transform.position + offset;
                Instantiate(controller.Data.healthPickupPrefab, safePos, Quaternion.identity);
            }
        }

        SpawnSpores();
        Destroy(gameObject);
    }
}