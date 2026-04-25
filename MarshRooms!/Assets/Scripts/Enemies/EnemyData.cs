// Holds all stats for a specific enemy type
// To add a new enemy: Right click Project tab - Create - Enemies - Enemy Data

using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemies/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Stats")]
    public float maxHealth;
    public float moveSpeed;

    [Header("Weapon")]
    public WeaponData weapon;

    [Header("AI")]
    public float detectionRange;
    public float attackRange;
}
