using UnityEngine;
using TopDown.Movement;

public class PlayerMutatedStats : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BaseMover mover;
    [SerializeField] private PlayerShooter shooter;
    [SerializeField] private PlayerWeaponSlot weaponSlot;

    [Header("Mutated Boosts")]
    [SerializeField] private float speedMultiplier;
    [SerializeField] private float fireRateMultiplier;
    [SerializeField] private float mutatedShakeMultiplier;

    [Header("Rainbow Laser Mutated")]
    [SerializeField] private int laserMutatedBulletCount = 3;
    [SerializeField] private float laserMutatedSpread = 5f;
    [SerializeField] private string laserGunName = "Rainbow Laser";

    [Header("Spore Blaster Mutated")]
    [SerializeField] private int sporeMutatedBulletCount = 5;
    [SerializeField] private float sporeMutatedSpread = 10f;
    [SerializeField] private string sporeGunName = "Spore Blaster";

    [Header("Bubble Gun Mutated")]
    [SerializeField] private float bubbleMutatedMinDamage = 1f;
    [SerializeField] private float bubbleMutatedMaxDamage = 2f;
    [SerializeField] private float bubbleMutatedBulletSpeedMultiplier = 1.4f;
    [SerializeField] private string bubbleGunName = "Bubble Gun";


    private void Start()
    {
        SporeManager.Instance.OnMutatedActivated += OnActivated;
        SporeManager.Instance.OnMutatedEnded += OnEnded;
        weaponSlot.OnWeaponChanged += OnWeaponChanged;
    }

    private void OnDisable()
    {
        if (SporeManager.Instance != null)
        {
            SporeManager.Instance.OnMutatedActivated -= OnActivated;
            SporeManager.Instance.OnMutatedEnded -= OnEnded;
        }
        if (weaponSlot != null)
            weaponSlot.OnWeaponChanged -= OnWeaponChanged;
    }

    private void OnActivated()
    {
        mover.SetSpeedMultiplier(speedMultiplier);
        shooter.SetFireRateMultiplier(fireRateMultiplier);
        shooter.SetShakeMultiplier(mutatedShakeMultiplier);
        shooter.SetDamageMultiplier(1f + BoonManager.Instance.Stats.mutationDamageBonus);
        ApplyWeaponOverrides();
    }

    private void OnEnded()
    {
        ResetStateSilently();
    }

    public void ResetStateSilently()
    {
        mover.SetSpeedMultiplier(1f);
        shooter.SetFireRateMultiplier(1f);
        shooter.SetShakeMultiplier(1f);
        shooter.SetDamageMultiplier(1f);
        shooter.ClearBulletOverrides();
        shooter.ClearDamageOverride();
        shooter.SetBulletSpeedMultiplier(1f);
    }

    private void ApplyWeaponOverrides()
    {
        if (shooter.currentWeapon == null) return;

        shooter.ClearDamageOverride();
        shooter.SetBulletSpeedMultiplier(1f);

        if (shooter.currentWeapon.gunName == laserGunName)
            shooter.SetBulletOverrides(laserMutatedBulletCount, laserMutatedSpread);
        else if (shooter.currentWeapon.gunName == sporeGunName)
            shooter.SetBulletOverrides(sporeMutatedBulletCount, sporeMutatedSpread);
        else if (shooter.currentWeapon.gunName == bubbleGunName)
        {
            shooter.SetDamageOverride(bubbleMutatedMinDamage, bubbleMutatedMaxDamage);
            shooter.SetBulletSpeedMultiplier(bubbleMutatedBulletSpeedMultiplier);
        }
    }

    private void OnWeaponChanged(WeaponData weapon)
    {
        if (!SporeManager.Instance.IsMutated) return;
        shooter.ClearBulletOverrides();
        ApplyWeaponOverrides();
    }
}