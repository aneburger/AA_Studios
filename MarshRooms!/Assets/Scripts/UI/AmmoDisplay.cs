using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AmmoDisplay : MonoBehaviour
{
    [SerializeField] private Image ammoDisplayImage;
    [SerializeField] private TextMeshProUGUI ammoCountText;

    private PlayerShooter playerShooter;
    private PlayerWeaponSlot weaponSlots;

    private void Start()
    {
        playerShooter = FindFirstObjectByType<PlayerShooter>();
        weaponSlots = FindFirstObjectByType<PlayerWeaponSlot>();
    }

    private void Update()
    {
        if (playerShooter == null) return;

        UpdateAmmoDisplay();
    }

    // -- UPDATE AMMO DISPLAY --
    private void UpdateAmmoDisplay()
    {
        if (playerShooter.currentWeapon == null)
        {
            ammoDisplayImage.enabled = false;
            ammoCountText.enabled = false;
            return;
        }

        if (playerShooter.currentWeapon.ammoDisplaySprite != null)
        {
            ammoDisplayImage.sprite = playerShooter.currentWeapon.ammoDisplaySprite;
            ammoDisplayImage.enabled = true;
        }
        else
        {
            ammoDisplayImage.enabled = false;
        }

        // update ammo count text only for weapons with limited ammo
        int currentAmmo = playerShooter.GetAmmo();

        if (currentAmmo == -1)
        {
            // plunger shows infinity symbol
            ammoCountText.enabled = false;
        }
        else if (currentAmmo >= 0)
        {
            // display ammo count for limited ammo weapons
            ammoCountText.text = currentAmmo.ToString();
            ammoCountText.enabled = true;
        }
        else
        {
            ammoCountText.enabled = false;
        }
    }
}