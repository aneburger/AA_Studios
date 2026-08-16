// Central hub for the enemy, read EnemyData andd initialises components

using UnityEngine;
using System.Collections;
using TopDown.Movement;

public class EnemyController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private bool externalAIControl = false;
    [SerializeField] private float spawnDuration = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip spawnClip;
    [Range(0f, 1f)] public float spawnVolume;

    private EnemyShooter shooter;
    private EnemyMover mover;
    private EnemyHealth health;
    private Animator anim;
    private Behaviour aiBehaviour;
    private EliteModifier eliteModifier;

    private EnemyData effectiveData;
    public EnemyData Data
    {
        get
        {
            if (effectiveData == null)
                effectiveData = ResolveEffectiveData();
            return effectiveData;
        }
    }

    public bool IsSpawning { get; private set; }

    private bool shouldSpawnAnimate = false;

    // -- AWAKE --
    private void Awake()
    {
        mover = GetComponent<EnemyMover>();
        shooter = GetComponent<EnemyShooter>();
        health = GetComponent<EnemyHealth>();
        anim = GetComponentInChildren<Animator>();
        aiBehaviour = GetComponent<IEnemyAI>() as Behaviour;

        mover.SetSpeed(Data.moveSpeed);
    }

    // -- START --
    private void Start()
    {
        InitialiseWeapon();
        health.Initialise(Data.maxHealth);
        EnemyManager.Instance.RegisterEnemy(gameObject);

        if (shouldSpawnAnimate)
        {
            shooter?.HideWeapon(true);
            StartCoroutine(SpawnAnimation());
        }
        else
        {
            shooter?.HideWeapon(false);
        }
    }

    // -- RESOLVE EFFECTIVE DATA --
    private EnemyData ResolveEffectiveData()
    {
        if (eliteModifier == null)
            eliteModifier = GetComponent<EliteModifier>();

        return eliteModifier != null ? eliteModifier.ApplyStatModifiers(enemyData) : enemyData;
    }

    // -- CONTACT DAMAGE --
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (IsSpawning) return;

        PlayerHealth playerHealth = collision.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;
        if (playerHealth.IsOnCooldown()) return;

        playerHealth.TakeDamage(Data.contactDamage);

        BaseMover playerMover = collision.GetComponentInParent<BaseMover>();
        if (playerMover != null)
        {
            Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
            playerMover.ApplyKnockback(knockbackDir * Data.contactKnockback);
        }
    }

    // -- INITIALISE WEAPON --
    private void InitialiseWeapon()
    {
        if (Data == null || Data.weapon == null) return;

        WeaponData weaponToEquip = Data.weapon;
        if (eliteModifier != null)
            weaponToEquip = eliteModifier.ApplyWeaponModifiers(weaponToEquip);

        shooter.EquipWeapon(weaponToEquip);
    }

    // -- SET SHOULD SPAWN ANIMATE --
    public void SetShouldSpawnAnimate()
    {
        shouldSpawnAnimate = true;
    }

    public void SetExternalAIControl(bool value) => externalAIControl = value;

    // -- SPAWN ANIMATION --
    private IEnumerator SpawnAnimation()
    {
        AudioManager.Instance.PlaySFXWithPitch(spawnClip, spawnVolume, 0.1f);

        IsSpawning = true;
        if (aiBehaviour != null) aiBehaviour.enabled = false;
        if (mover != null) mover.enabled = false;

        anim?.SetTrigger("Spawn");
        yield return new WaitForSeconds(spawnDuration);

        shooter?.HideWeapon(false);
        if (mover != null) mover.enabled = true;
        if (!externalAIControl && aiBehaviour != null) aiBehaviour.enabled = true;
        IsSpawning = false;
    }
}