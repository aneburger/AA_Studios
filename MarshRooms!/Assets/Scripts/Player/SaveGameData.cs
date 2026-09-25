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

    // Weapons
    public List<WeaponSaveEntry> weapons = new List<WeaponSaveEntry>();
    public int currentWeaponSlot;

    // Death screen stats
    public RunStatsSaveData runStatsTrackerData;

    // Abilities
    public bool dodgeAttackUnlocked;
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