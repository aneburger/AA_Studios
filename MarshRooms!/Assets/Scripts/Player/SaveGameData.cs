using System.Collections.Generic;

[System.Serializable]
public class SaveGameData
{
    public string sceneName;

    // Boon / run stats
    public RunStats runStats;
    public List<BoonCountEntry> boonCounts = new List<BoonCountEntry>();

    // Spores
    public int currentSpores;

    // Health
    public int maxHealth;
    public int currentHealth;

    // Weapons
    public List<WeaponSaveEntry> weapons = new List<WeaponSaveEntry>();
    public int currentWeaponSlot;
}

[System.Serializable]
public class BoonCountEntry
{
    public string boonId;
    public int count;
}

[System.Serializable]
public class WeaponSaveEntry
{
    public int slot;
    public string weaponId;
    public int ammo;
}