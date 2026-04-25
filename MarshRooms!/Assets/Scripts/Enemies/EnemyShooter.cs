// Extends BaseShooter
// Gets shoot direction from WeaponAimer
// Called by EnemyAI

using UnityEngine;

public class EnemyShooter : BaseShooter
{
    // -- START --
    private void Start()
    {
        UpdateWeaponVisuals();
    }

    // -- GET SHOOT DIRECTION --
    protected override Vector2 GetShootDirection()
    {
        if (weaponAimer != null)
            return weaponAimer.AimDirection;

        return Vector2.right;
    }

    // -- SHOOT --
    public void TryShoot()
    {
        if (!IsArmed) return;

        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + currentWeapon.fireRate;
        }
    }
}