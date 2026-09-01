using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AmmoDisplay : MonoBehaviour
{
    [SerializeField] private Image ammoDisplayImage;
    [SerializeField] private TextMeshProUGUI ammoCountText;
    [SerializeField] private TextMeshProUGUI maxAmmoCountText;
    [SerializeField] private TextMeshProUGUI slashText;
    [SerializeField] private TextMeshProUGUI weaponName;

    [Header("Weapon Name Timing")]
    [SerializeField] private float weaponNameVisibleTime = 2f;
    [SerializeField] private float weaponNameFadeTime = 0.4f;

    private PlayerShooter playerShooter;
    private PlayerWeaponSlot weaponSlots;

    private WeaponData lastWeapon;
    private Coroutine weaponNameRoutine;

    private void Start()
    {
        playerShooter = FindFirstObjectByType<PlayerShooter>();
        weaponSlots = FindFirstObjectByType<PlayerWeaponSlot>();

        if (weaponName != null)
        {
            weaponName.enabled = false;
            SetWeaponNameAlpha(0f);
        }
    }

    private void Update()
    {
        if (playerShooter == null) return;

        UpdateAmmoDisplay();
    }

    // -- UPDATE AMMO DISPLAY --
    private void UpdateAmmoDisplay()
    {
        WeaponData currentWeapon = playerShooter.currentWeapon;

        if (currentWeapon == null)
        {
            SetAmmoUiVisible(false, false, false, false);
            HideWeaponNameImmediate();
            lastWeapon = null;
            return;
        }

        // Show weapon name only when the active weapon changes
        if (currentWeapon != lastWeapon)
        {
            lastWeapon = currentWeapon;
            ShowWeaponName(currentWeapon.gunName);
        }

        int currentAmmo = playerShooter.GetAmmo();

        // Plunger / infinite ammo: show nothing for ammo UI
        if (currentAmmo == -1)
        {
            ammoDisplayImage.enabled = false;
            ammoCountText.enabled = false;
            maxAmmoCountText.enabled = false;
            if (slashText != null) slashText.enabled = false;
            return;
        }

        if (currentWeapon.ammoDisplaySprite != null)
        {
            ammoDisplayImage.sprite = currentWeapon.ammoDisplaySprite;
            ammoDisplayImage.enabled = true;
        }
        else
        {
            ammoDisplayImage.enabled = false;
        }

        ammoCountText.text = currentAmmo.ToString();
        maxAmmoCountText.text = currentWeapon.maxAmmo.ToString();

        ammoCountText.enabled = true;
        maxAmmoCountText.enabled = true;
        if (slashText != null) slashText.enabled = true;
    }

    private void ShowWeaponName(string name)
    {
        if (weaponName == null)
            return;

        if (weaponNameRoutine != null)
            StopCoroutine(weaponNameRoutine);

        weaponName.text = name;
        weaponName.enabled = true;
        SetWeaponNameAlpha(1f);

        weaponNameRoutine = StartCoroutine(FadeWeaponNameRoutine());
    }

    private IEnumerator FadeWeaponNameRoutine()
    {
        yield return new WaitForSeconds(weaponNameVisibleTime);

        float elapsed = 0f;
        while (elapsed < weaponNameFadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / weaponNameFadeTime);
            SetWeaponNameAlpha(alpha);
            yield return null;
        }

        SetWeaponNameAlpha(0f);
        if (weaponName != null)
            weaponName.enabled = false;

        weaponNameRoutine = null;
    }

    private void HideWeaponNameImmediate()
    {
        if (weaponNameRoutine != null)
        {
            StopCoroutine(weaponNameRoutine);
            weaponNameRoutine = null;
        }

        if (weaponName != null)
        {
            weaponName.enabled = false;
            SetWeaponNameAlpha(0f);
        }
    }

    private void SetWeaponNameAlpha(float alpha)
    {
        if (weaponName == null)
            return;

        Color color = weaponName.color;
        color.a = alpha;
        weaponName.color = color;
    }

    private void SetAmmoUiVisible(bool currentVisible, bool maxVisible, bool slashVisible, bool imageVisible)
    {
        if (ammoCountText != null)
            ammoCountText.enabled = currentVisible;

        if (maxAmmoCountText != null)
            maxAmmoCountText.enabled = maxVisible;

        if (slashText != null)
            slashText.enabled = slashVisible;

        if (ammoDisplayImage != null)
            ammoDisplayImage.enabled = imageVisible;
    }
}