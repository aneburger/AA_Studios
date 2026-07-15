// Holds all stats for a specific enemy type
// To add a new enemy: Right click Project tab - Create - Enemies - Enemy Data

using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemies/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Stats")]
    public float maxHealth;
    public float moveSpeed;
    public float contactDamage;
    public float contactKnockback;

    [Header("Weapon")]
    public WeaponData weapon;

    [Header("AI")]
    public float detectionRange;
    public float attackRange;

    [System.Serializable]
    public class WeaponDrop
    {
        public GameObject weaponPickupPrefab;
        [Range(0f, 1f)] public float dropChance;
    }

    [Header("Weapon Drop")]
    public WeaponDrop[] possibleWeaponDrops;

    [Header("Spore Drop")]
    public int maxSporeDrops;
    public GameObject sporePrefab;
    public bool guaranteesSporeDrop = false;

    [Header("Health Drop")]
    public GameObject healthPickupPrefab;
    [Range(0f, 1f)] public float healthDropChance;
}