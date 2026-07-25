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
        return weaponAimer != null ? weaponAimer.AimDirection : Vector2.right;
    }

    // -- SHOOT --
    public void TryShoot()
    {
        if (!IsArmed) return;
        if (Time.time < nextFireTime) return;

        Shoot();
        nextFireTime = Time.time + currentWeapon.fireRate * GetFireRateMultiplier();
    }
}