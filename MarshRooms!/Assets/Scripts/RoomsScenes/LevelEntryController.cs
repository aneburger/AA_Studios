using UnityEngine;

public class LevelEntryController : MonoBehaviour
{
    [SerializeField] private WeaponData defaultWeapon;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var weaponSlot = player.GetComponent<PlayerWeaponSlot>();
        if (weaponSlot != null)
        {
            if (!weaponSlot.HasAnyWeapon() && defaultWeapon != null)
            {
                weaponSlot.GrantWeapon(defaultWeapon);
            }
        }

        if (LevelLoader.Instance.GetCurrentFloorNumber() == 1)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            playerHealth?.ResetHealth();
        }

        TryHealToFull(player);
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        health?.UpdateLowHealthEffect();
    }

    private void TryHealToFull(GameObject player)
    {
        if (BoonManager.Instance == null) return;

        float chance = BoonManager.Instance.Stats.healToFullChance;
        if (chance <= 0f) return;

        if (Random.value <= chance)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.Heal(playerHealth.GetMaxHealth());
        }
    }
}