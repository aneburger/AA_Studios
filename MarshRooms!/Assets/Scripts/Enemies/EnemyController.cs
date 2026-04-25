// Central hub for the enemy - read EnemyData andd initialises components

using UnityEngine;
using TopDown.Movement;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private Transform weaponPivot;
    
    private EnemyShooter shooter;
    private EnemyMover mover;
    private WeaponAimer weaponAimer;

    public EnemyData Data => enemyData;

    // -- AWAKE --
    private void Awake()
    {
        // Initialise componenets
        mover = GetComponent<EnemyMover>();
        shooter = GetComponent<EnemyShooter>();
        mover.SetSpeed(enemyData.moveSpeed);
    }

    private void Start()
    {
        InitialiseWeapon();
    }

    private void InitialiseWeapon()
    {
        if (enemyData == null) return;
        if (enemyData.weapon == null) return;

        shooter.EquipWeapon(enemyData.weapon);
    }
}
