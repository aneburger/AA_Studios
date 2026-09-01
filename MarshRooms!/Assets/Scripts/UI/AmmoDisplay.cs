using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AmmoDisplay : MonoBehaviour
{
    [SerializeField] private Image ammoDisplayImage;
    [SerializeField] private TextMeshProUGUI ammoCountText;
    [SerializeField] private TextMeshProUGUI maxAmmoCountText;
    [SerializeField] private TextMeshProUGUI slashText;

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
            maxAmmoCountText.enabled = false;
            if (slashText != null) slashText.enabled = false;
            return;
        }

        int currentAmmo = playerShooter.GetAmmo();

        // Plunger: show nothing
        if (currentAmmo == -1)
        {
            ammoDisplayImage.enabled = false;
            ammoCountText.enabled = false;
            maxAmmoCountText.enabled = false;
            if (slashText != null) slashText.enabled = false;
            return;
        }

        ammoDisplayImage.enabled = playerShooter.currentWeapon.ammoDisplaySprite != null;
        if (ammoDisplayImage.enabled)
            ammoDisplayImage.sprite = playerShooter.currentWeapon.ammoDisplaySprite;

        ammoCountText.text = currentAmmo.ToString();
        maxAmmoCountText.text = playerShooter.currentWeapon.maxAmmo.ToString();

        ammoCountText.enabled = true;
        maxAmmoCountText.enabled = true;
        if (slashText != null) slashText.enabled = true;
    }

    
}