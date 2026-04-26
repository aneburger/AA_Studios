// Holds all stats for a specific weapon
// To add a new weapon: Right click Project tab - Create - Weapons - WeaponData

using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapons/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Firing")]
    public GameObject bulletPrefab;
    public float fireRate;
    public float damage;
    public float bulletSpeed;

    [Header("Visuals")]
    public Sprite sprite;

    [Header("Audio")]

    [Header("Feel")]
    public float shakeForce;
    public float knockbackForce;
    public float hitKnockback;
    public float recoilAmount;
    public float recoilDecay;
}
