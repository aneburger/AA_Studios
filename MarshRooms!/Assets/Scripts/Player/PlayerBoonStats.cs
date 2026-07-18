using UnityEngine;

public class PlayerBoonStats : MonoBehaviour
{
    private BaseShooter shooter;
    private TopDown.Movement.BaseMover mover;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        shooter = GetComponent<BaseShooter>();
        mover = GetComponent<TopDown.Movement.BaseMover>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Start()
    {
        ApplyAllStats();
        BoonManager.Instance.OnBoonApplied += OnBoonApplied;
    }

    private void OnDisable()
    {
        if (BoonManager.Instance != null)
            BoonManager.Instance.OnBoonApplied -= OnBoonApplied;
    }

    private void OnBoonApplied(string boonId)
    {
        ApplyAllStats();
    }

    // -- APPLY ALL STATS  --
    private void ApplyAllStats()
    {
        RunStats stats = BoonManager.Instance.Stats;

        // Damage side (BaseShooter)
        shooter.SetPermanentFireRateMultiplier(stats.permanentFireRateMultiplier);
        shooter.SetPermanentDamageMultiplier(stats.permanentDamageMultiplier);
        shooter.SetCritChance(stats.critChance);
        shooter.SetPermanentBulletCountBonus(stats.permanentBulletCountBonus);

        // Spore/mutation side (SporeManager)
        SporeManager.Instance.SetBonusMutatedDuration(stats.bonusMutationDuration);
        SporeManager.Instance.SetSporeGainAmount(stats.sporeGainAmount);

        // Health side (PlayerHealth)
        playerHealth.SetBonusIFrameDuration(stats.bonusIFrameDuration);
        playerHealth.SetDodgeDamageChance(stats.dodgeDamageChance);
    }
}