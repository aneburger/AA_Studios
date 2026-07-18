[System.Serializable]
public class RunStats
{
    // Health
    public int bonusMaxHearts = 0;
    public float healthDropRateMultiplier = 1f;
    public float healToFullChance = 0f;
    public float bonusIFrameDuration = 0f;
    public float dodgeDamageChance = 0f;

    // Spore Mutation
    public float bonusMutationDuration = 0f;
    public float mutationDamageBonus = 0f;
    public bool hasMushroomBomb = false;
    public int sporeGainAmount = 1;

    // Damage
    public float permanentFireRateMultiplier = 1f;
    public float critChance = 0f;
    public float permanentDamageMultiplier = 1f;
    public int permanentBulletCountBonus = 0;
}