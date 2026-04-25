// Holds all stats for a specific weapon
// To add a new weapon: Right click Project tab - Create - Weapons - WeaponData

using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapons/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Firing")]
    public GameObject bulletPrefab;
    public float fireRate = 0.4f;

    [Header("Visuals")]
    public Sprite sprite;

    [Header("Audio")]

    [Header("Feel")]
    public float shakeForce = 0.1f;
    public float knockbackForce = 2f;
    public float recoilAmount = 15f;
    public float recoilDecay = 10f;
}
