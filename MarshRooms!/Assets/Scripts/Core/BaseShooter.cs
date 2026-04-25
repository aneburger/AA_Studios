// Base class for all shooting logic
// Handles fire rate and bullet spawning

using UnityEngine;
using TopDown.Movement;

public abstract class BaseShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected WeaponAimer weaponAimer;
    [SerializeField] protected BaseMover mover;
    [SerializeField] protected SpriteRenderer weaponSprite;

    [Header("Weapon")]
    [SerializeField] protected WeaponData currentWeapon;

    public bool IsArmed => currentWeapon != null;

    protected float nextFireTime = 0f;

    // -- EQUIP WEAPON --
    public void EquipWeapon(WeaponData weapon)
    {
        currentWeapon = weapon;
        nextFireTime = 0f;
        UpdateWeaponVisuals();
    }

    // -- GET SHOOT DIRECTION (implemented by subclasses) --
    protected abstract Vector2 GetShootDirection();

    // -- SHOOT --
    public void Shoot()
    {
        if (currentWeapon == null) return;

        Vector2 direction = GetShootDirection();

        GameObject bullet = Instantiate(currentWeapon.bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.GetComponent<Bullet>().SetDirection(direction);

        OnShootEffects(direction);
    }

    // -- SHOOT EFFECTS --
    protected virtual void OnShootEffects(Vector2 direction)
    {
        weaponAimer?.ApplyRecoil(currentWeapon.recoilAmount, currentWeapon.recoilDecay);
        mover?.ApplyKnockback(-direction * currentWeapon.knockbackForce);
    }

    // -- SHOW / HIDE WEAPON --
    protected void UpdateWeaponVisuals()
    {
        if (weaponSprite != null)
        {
            weaponSprite.enabled = IsArmed;
            if (IsArmed) weaponSprite.sprite = currentWeapon.sprite;
        }

        if (weaponAimer != null) weaponAimer.enabled = IsArmed;
    }
}
