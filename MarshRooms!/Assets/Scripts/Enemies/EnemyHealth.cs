// Enemy health, extends BaseHealth
// Handles enemy death, despawn and drops

using UnityEngine;

public class EnemyHealth : BaseHealth
{   
    [Header("Health Bar")]
    [SerializeField] private EnemyHealthBar healthBar;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtClip;
    [Range(0f, 1f)] public float hurtVolume;
    [SerializeField] private AudioClip dieClip;
    [Range(0f, 1f)] public float dieVolume;

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

        RoomManager room = FindObjectOfType<RoomManager>();

        int amount = Random.Range(0, controller.Data.maxSporeDrops + 1);
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
    }

    // -- HIT EFFECT --
    protected override void OnHitEffect()
    {
        AudioManager.Instance.PlaySFXWithPitch(hurtClip, hurtVolume, 0.1f);
    }

    // -- DIE --
    protected override void Die()
    {
        base.Die();
        EnemyManager.Instance.UnregisterEnemy(gameObject);
        VFXManager.Instance.SpawnEnemyExplosion(transform.position);
        AudioManager.Instance.PlaySFX(dieClip, dieVolume);

        EnemyController controller = GetComponent<EnemyController>();
        if (controller != null && controller.Data.possibleWeaponDrops.Length > 0)
        {
            RoomManager room = FindObjectOfType<RoomManager>();
            if (room != null)
            {
                // Pick a random weapon from the possible drops
                EnemyData.WeaponDrop drop = controller.Data.possibleWeaponDrops[Random.Range(0, controller.Data.possibleWeaponDrops.Length)];
                room.TryDropWeapon(transform.position, drop.weaponPickupPrefab, drop.dropChance);
            }
        }

        SpawnSpores();
        Destroy(gameObject);
    }
}