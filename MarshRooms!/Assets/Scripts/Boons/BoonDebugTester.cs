using UnityEngine;

public class BoonDebugTester : MonoBehaviour
{
    private BaseShooter shooter;

    private void Awake()
    {
        shooter = GetComponent<BaseShooter>();
    }

    [ContextMenu("Test - Permanent Damage x2")]
    private void TestDamage()
    {
        shooter.SetPermanentDamageMultiplier(2f);
        Debug.Log("Permanent damage multiplier set to 2x");
    }

    [ContextMenu("Test - Crit Chance 50%")]
    private void TestCrit()
    {
        shooter.SetCritChance(0.5f);
        Debug.Log("Crit chance set to 50%");
    }

    [ContextMenu("Test - Fire Rate x1.5")]
    private void TestFireRate()
    {
        shooter.SetPermanentFireRateMultiplier(1.5f);
        Debug.Log("Fire rate multiplier set to 1.5x");
    }

    [ContextMenu("Test - Extra Bullet +2")]
    private void TestBulletCount()
    {
        shooter.SetPermanentBulletCountBonus(2);
        Debug.Log("Bullet count bonus set to +2");
    }

    [ContextMenu("Test - Mutation Damage Bonus +50%")]
    private void TestMutationDamage()
    {
        BoonManager.Instance.Stats.mutationDamageBonus = 0.5f;
        Debug.Log("Mutation damage bonus set to +50%");
    }

    [ContextMenu("Test - Mushroom Bomb ON")]
    private void TestMushroomBomb()
    {
        BoonManager.Instance.Stats.hasMushroomBomb = true;
        Debug.Log("Mushroom Bomb enabled - mutate to trigger it");
    }

    [ContextMenu("Test - Bonus Mutation Duration +5s")]
    private void TestMutationDuration()
    {
        SporeManager.Instance.SetBonusMutatedDuration(5f);
        Debug.Log("Bonus mutation duration set to +5s");
    }

    [ContextMenu("Test - Spore Gain x3")]
    private void TestSporeGain()
    {
        SporeManager.Instance.SetSporeGainAmount(3);
        Debug.Log("Spore gain amount set to 3 per pickup");
    }

    [ContextMenu("Test - Bonus IFrames +2s")]
    private void TestIFrames()
    {
        GetComponent<PlayerHealth>().SetBonusIFrameDuration(2f);
        Debug.Log("Bonus i-frame duration set to +2s");
    }

    [ContextMenu("Test - Dodge Chance 100%")]
    private void TestDodge()
    {
        GetComponent<PlayerHealth>().SetDodgeDamageChance(1f);
        Debug.Log("Dodge chance set to 100%");
    }

    [ContextMenu("Test - Increase Max Health +4 (heal too)")]
    private void TestMaxHealth()
    {
        GetComponent<PlayerHealth>().IncreaseMaxHealth(4, healToFull: true);
        Debug.Log("Max health increased by 4, healed to full");
    }

    [ContextMenu("Test - Apply Health Drop Rate")]
    private void TestApplyHealthDropRate()
    {
        BoonManager.Instance.ApplyBoonById("health_drop_rate");
        Debug.Log($"[TEST] healthDropRateMultiplier = {BoonManager.Instance.Stats.healthDropRateMultiplier}");
    }

    [ContextMenu("Test - Apply Heal Full Chance")]
    private void TestApplyHealFullChance()
    {
        BoonManager.Instance.ApplyBoonById("heal_full_chance");
        Debug.Log($"[TEST] healToFullChance = {BoonManager.Instance.Stats.healToFullChance}");
    }

    [ContextMenu("Test - Apply IFrame Extension")]
    private void TestApplyIFrameExtension()
    {
        BoonManager.Instance.ApplyBoonById("iframe_extension");
        Debug.Log($"[TEST] bonusIFrameDuration = {BoonManager.Instance.Stats.bonusIFrameDuration}");
    }

    [ContextMenu("Test - Apply Mutation Damage (call twice)")]
    private void TestApplyMutationDamage()
    {
        BoonManager.Instance.ApplyBoonById("mutation_damage");
        Debug.Log($"[TEST] mutationDamageBonus = {BoonManager.Instance.Stats.mutationDamageBonus}, owned count = {BoonManager.Instance.GetOwnedCount("mutation_damage")}");
    }

    [ContextMenu("Test - Get Three Card Offers (Floor 1)")]
    private void TestOffersFloor1()
    {
        var offers = BoonManager.Instance.GetThreeCardOffers(1);
        foreach (var c in offers)
            Debug.Log($"Offered: {c.displayName} ({c.rarity})");
    }

    [ContextMenu("Test - Get Three Card Offers (Floor 3)")]
    private void TestOffersFloor3()
    {
        var offers = BoonManager.Instance.GetThreeCardOffers(3);
        foreach (var c in offers)
            Debug.Log($"Offered: {c.displayName} ({c.rarity})");
    }

    [ContextMenu("Test - Get Three Card Offers (Floor 5)")]
    private void TestOffersFloor5()
    {
        var offers = BoonManager.Instance.GetThreeCardOffers(5);
        foreach (var c in offers)
            Debug.Log($"Offered: {c.displayName} ({c.rarity})");
    }

    [ContextMenu("Test - Get Three Card Offers (Floor 7)")]
    private void TestOffersFloor7()
    {
        var offers = BoonManager.Instance.GetThreeCardOffers(7);
        foreach (var c in offers)
            Debug.Log($"Offered: {c.displayName} ({c.rarity})");
    }

    [ContextMenu("Test - Get Three Card Offers (Floor 9)")]
    private void TestOffersFloor9()
    {
        var offers = BoonManager.Instance.GetThreeCardOffers(9);
        foreach (var c in offers)
            Debug.Log($"Offered: {c.displayName} ({c.rarity})");
    }

    [ContextMenu("Test - Get Three Card Offers (Floor 10)")]
    private void TestOffersFloor10()
    {
        var offers = BoonManager.Instance.GetThreeCardOffers(10);
        foreach (var c in offers)
            Debug.Log($"Offered: {c.displayName} ({c.rarity})");
    }
    
}