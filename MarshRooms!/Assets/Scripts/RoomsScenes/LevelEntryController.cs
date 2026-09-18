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

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        playerHealth?.ResetHealth();
        playerHealth?.UpdateLowHealthEffect();
        ScreenEffects.Instance?.SetLowHealth(false);
    }
}