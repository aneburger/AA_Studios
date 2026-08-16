// Extends BaseShooter
// Gets shoot direction from WeaponAimer
// Called by EnemyAI

using UnityEngine;

public class EnemyShooter : BaseShooter
{
    private int burstShotsFired = 0;

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
        burstShotsFired++;

        int burstCount = Mathf.Max(1, currentWeapon.burstCount);

        if (burstShotsFired < burstCount)
        {
            nextFireTime = Time.time + currentWeapon.burstInterval * GetFireRateMultiplier();
        }
        else
        {
            burstShotsFired = 0;
            nextFireTime = Time.time + currentWeapon.fireRate * GetFireRateMultiplier();
        }
    }
}