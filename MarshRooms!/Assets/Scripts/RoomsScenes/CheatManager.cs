using UnityEngine;

public class CheatManager : MonoBehaviour
{
    public static CheatManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool cheatsEnabled = true;

    public bool CheatsEnabled => cheatsEnabled;
    public bool Invincibility { get; private set; }
    public bool InfiniteAmmo { get; private set; }

    public event System.Action<bool> OnInvincibilityChanged;
    public event System.Action<bool> OnInfiniteAmmoChanged;

    // -- AWAKE --
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private GameObject FindPlayer() => GameObject.FindGameObjectWithTag("Player");

    // -- INVINCIBILITY --
    public void SetInvincibility(bool value)
    {
        if (!cheatsEnabled) return;

        Invincibility = value;
        FindPlayer()?.GetComponent<PlayerHealth>()?.SetCheatInvincible(value);
        OnInvincibilityChanged?.Invoke(value);
    }

    // -- REFILL HEALTH --
    public void RefillHealth()
    {
        if (!cheatsEnabled) return;
        FindPlayer()?.GetComponent<PlayerHealth>()?.ResetHealth();
    }

    // -- FILL MUTATION BAR --
    public void FillMutationBar()
    {
        if (!cheatsEnabled) return;
        SporeManager.Instance?.FillToMax();
    }

    // -- SKIP TO LEVEL --
    public void SkipToLevel(int floorNumber)
    {
        if (!cheatsEnabled) return;
        LevelLoader.Instance?.LoadLevel($"Floor_{floorNumber:D2}");
    }

    // -- BOONS --
    public void SetBoonCheat(string boonId, bool value)
    {
        if (!cheatsEnabled) return;

        if (value) BoonManager.Instance?.ApplyBoonById(boonId);
        else BoonManager.Instance?.RemoveBoonById(boonId);
    }

    public bool IsBoonActive(string boonId)
    {
        return BoonManager.Instance != null && BoonManager.Instance.GetOwnedCount(boonId) > 0;
    }

    // -- WEAPONS --
    public void GiveWeapon(WeaponData weapon)
    {
        if (!cheatsEnabled || weapon == null) return;
        FindPlayer()?.GetComponent<PlayerWeaponSlot>()?.CheatEquipWeapon(weapon);
    }

    // -- INFINITE AMMO --
    public void SetInfiniteAmmo(bool value)
    {
        if (!cheatsEnabled) return;

        InfiniteAmmo = value;
        OnInfiniteAmmoChanged?.Invoke(value);
    }
}