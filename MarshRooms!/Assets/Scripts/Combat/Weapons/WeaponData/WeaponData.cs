// Holds all stats for a specific weapon
// To add a new weapon: Right click Project tab - Create - Weapons - WeaponData

using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapons/WeaponData")]
public class WeaponData : ScriptableObject
{   
    [Header("Name")]
    public string gunName;

    [Header("Prefabs")]
    public GameObject bulletPrefab;
    public GameObject shellsPrefab;
    public GameObject hitPrefab;
    public GameObject muzzleFlashPrefab;
    public GameObject pickupPrefab;

    [Header("Firing")]
    public float fireRate;
    public float damage;
    public float bulletSpeed;
    public int maxAmmo;

    [Header("Aim")]
    public Vector2 firePointOffset;

    [Header("Visuals")]
    public Sprite sprite;
    public Sprite hudSprite;
    public Sprite ammoDisplaySprite;

    [Header("Audio")]
    public AudioClip shootClip;
    [Range(0f, 1f)] public float shootVolume;

    [Header("Feel")]
    public float shakeForce;
    public float knockbackForce;
    public float hitKnockback;
    public float recoilAmount;
    public float recoilDecay;
}
