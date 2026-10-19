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

    [Header("Active Slot Positioning")]
    [SerializeField] private RectTransform[] slotAnchors = new RectTransform[3];
    [SerializeField] private Vector2 offsetFromSlot = new Vector2(0f, -60f);

    [Header("Ammo Effects")]
    [SerializeField] private Color lowAmmoColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField, Range(0f, 1f)] private float lowAmmoFraction = 0.15f;

    private RectTransform selfRect;
    private Canvas rootCanvas;

    private PlayerShooter playerShooter;
    private PlayerWeaponSlot weaponSlots;

    private WeaponData lastWeapon;
    private Coroutine weaponNameRoutine;

    private Color normalAmmoColor = Color.white;

    private void Start()
    {
        selfRect = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        playerShooter = FindFirstObjectByType<PlayerShooter>();
        weaponSlots = FindFirstObjectByType<PlayerWeaponSlot>();

        if (ammoCountText != null)
            normalAmmoColor = ammoCountText.color;

        if (weaponName != null)
        {
            weaponName.enabled = false;
            SetWeaponNameAlpha(0f);
        }
    }

    private void Update()
    {
        PositionUnderActiveSlot();

        if (playerShooter == null) return;

        UpdateAmmoDisplay();
    }

    // -- POSITION UNDER ACTIVE SLOT --
    private void PositionUnderActiveSlot()
    {
        if (selfRect == null || weaponSlots == null || slotAnchors == null) return;

        int activeSlot = weaponSlots.CurrentSlotIndex;
        if (activeSlot < 0 || activeSlot >= slotAnchors.Length) return;

        RectTransform anchor = slotAnchors[activeSlot];
        if (anchor == null) return;

        float scale = rootCanvas != null ? rootCanvas.scaleFactor : 1f;
        selfRect.position = anchor.position + (Vector3)(offsetFromSlot * scale);
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

        UpdateAmmoEffects(currentAmmo, currentWeapon.maxAmmo);
    }

    // -- UPDATE AMMO EFFECTS --
    private void UpdateAmmoEffects(int currentAmmo, int maxAmmo)
    {
        // red when low
        bool isLow = maxAmmo > 0 && currentAmmo <= maxAmmo * lowAmmoFraction;
        ammoCountText.color = isLow ? lowAmmoColor : normalAmmoColor;    }

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