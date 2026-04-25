// Handles firing logic for any character player or enemies

using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using TopDown.Movement;

public class Shooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private PlayerAim aim;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private WeaponAimer weaponAimer;
    [SerializeField] private Mover mover;
    [SerializeField] private SpriteRenderer weaponSprite;

    [Header("Weapon")]
    [SerializeField] private WeaponData currentWeapon;

    public bool IsArmed => currentWeapon != null;

    private float nextFireTime = 0f;
    private bool isShooting = false;

    // -- START --
    private void Start()
    {
        UpdateWeaponVisuals();
    }

    // -- UPDATE --
    private void Update()
    {   
        if (!IsArmed) return;
        
        if (isShooting && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + currentWeapon.fireRate;
        }
    }

     // -- EQUIP WEAPON --
    public void EquipWeapon(WeaponData weapon)
    {
        // For picking up a weapon
        currentWeapon = weapon;
        nextFireTime = 0f;
        UpdateWeaponVisuals();
    }

    // -- SHOW / HIDE WEAPON --
    // We can update this later for weapon switching
    private void UpdateWeaponVisuals()
    {
        if (weaponSprite != null)
        {
            weaponSprite.enabled = IsArmed;
            if (IsArmed) weaponSprite.sprite = currentWeapon.sprite;
        }

        if (weaponAimer != null) weaponAimer.enabled = IsArmed;
    }

    // -- SHOOT INPUT --
    public void OnShoot(InputAction.CallbackContext context)
    {
        if (!IsArmed) return;
        if (context.started)  isShooting = true;
        if (context.canceled) isShooting = false;
    }

    // -- SHOOT --
    public void Shoot()
    {   
        if (currentWeapon == null) return;

        GameObject bullet = Instantiate(currentWeapon.bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.GetComponent<Bullet>().SetDirection(aim.AimDirection);

        // Screen shake
        impulseSource?.GenerateImpulse(currentWeapon.shakeForce);

        // Weapon recoil
        weaponAimer?.ApplyRecoil(currentWeapon.recoilAmount, currentWeapon.recoilDecay);

        // Weapon knockback
        mover?.ApplyKnockback(-aim.AimDirection * currentWeapon.knockbackForce);
    }
}
