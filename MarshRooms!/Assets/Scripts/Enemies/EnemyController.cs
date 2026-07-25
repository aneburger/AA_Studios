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

    public EnemyData Data => enemyData;
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
        mover.SetSpeed(enemyData.moveSpeed);
    }

    // -- START --
    private void Start()
    {
        InitialiseWeapon();
        health.Initialise(enemyData.maxHealth);
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

    // -- CONTACT DAMAGE --
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (IsSpawning) return;

        PlayerHealth playerHealth = collision.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;
        if (playerHealth.IsOnCooldown()) return;

        playerHealth.TakeDamage(enemyData.contactDamage);

        BaseMover playerMover = collision.GetComponentInParent<BaseMover>();
        if (playerMover != null)
        {
            Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
            playerMover.ApplyKnockback(knockbackDir * enemyData.contactKnockback);
        }
    }

    // -- INITIALISE WEAPON --
    private void InitialiseWeapon()
    {
        if (enemyData == null || enemyData.weapon == null) return;
        shooter.EquipWeapon(enemyData.weapon);
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
