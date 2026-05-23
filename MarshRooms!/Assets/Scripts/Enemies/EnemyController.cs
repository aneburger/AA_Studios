// Central hub for the enemy, read EnemyData andd initialises components

using UnityEngine;
using TopDown.Movement;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private Transform weaponPivot;
    
    private EnemyShooter shooter;
    private EnemyMover mover;
    private WeaponAimer weaponAimer;
    private EnemyHealth health;
    private Animator anim;

    public EnemyData Data => enemyData;

    // -- AWAKE --
    private void Awake()
    {
        // Initialise componenets
        mover = GetComponent<EnemyMover>();
        shooter = GetComponent<EnemyShooter>();
        health = GetComponent<EnemyHealth>();
        anim = GetComponentInChildren<Animator>();
        mover.SetSpeed(enemyData.moveSpeed);
    }

    // -- START--
    private void Start()
    {
        InitialiseWeapon();
        health.Initialise(enemyData.maxHealth);
        EnemyManager.Instance.RegisterEnemy(gameObject);
    }

    // -- CONTACT DAMAGE --
    private void OnTriggerStay2D(Collider2D collision)
    {
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
        if (enemyData == null) return;
        if (enemyData.weapon == null) return;

        shooter.EquipWeapon(enemyData.weapon);
    }

    // -- PLAY SPAWN ANIMATION --
    public void PlaySpawnAnimation()
    {
        anim?.SetTrigger("Spawn");
    }


}
