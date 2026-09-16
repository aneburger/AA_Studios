using UnityEngine;
using UnityEngine.UI;

public class InventoryDisplay : MonoBehaviour
{
    [Header("Inventory Container")]
    [SerializeField] private GameObject inventoryContainer;

    [Header("Inventory Slots")]
    [SerializeField] private Image[] inventorySlots = new Image[3];

    [Header("Inventory Slots Backgrounds")]
    [SerializeField] private Image[] inventorySlotsBackgrounds = new Image[3];

    [Header("Active Slot Highlight")]
    [SerializeField] private RectTransform activeHighlight;

    [Range(0f, 1f)]
    [SerializeField] private float emptySlotAlpha = 0.35f;

    private PlayerWeaponSlot weaponSlots;

    // -- START --
    private void Start()
    {
        weaponSlots = FindFirstObjectByType<PlayerWeaponSlot>();

        HideAllInventorySlots();
        UpdateInventoryDisplay();
    }

    // -- UPDATE --
    private void Update()
    {
        if (weaponSlots == null) return;

        UpdateInventoryDisplay();
    }

    // -- UPDATE INVENTORY DISPLAY --
    private void UpdateInventoryDisplay()
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            Image slotImage = inventorySlots[i];
            if (slotImage == null)
                continue;

            WeaponData weapon = weaponSlots.GetWeaponAtSlot(i);
            bool hasWeapon = weapon != null && weapon.hudSprite != null;

            if (hasWeapon)
            {
                slotImage.sprite = weapon.hudSprite;
                slotImage.color = Color.white;
                slotImage.enabled = true;
                slotImage.SetNativeSize();

                SetBackgroundAlpha(i, 1f);
            }
            else
            {
                slotImage.sprite = null;
                slotImage.enabled = false;

                SetBackgroundAlpha(i, emptySlotAlpha);
            }
        }

        UpdateActiveHighlight(weaponSlots.CurrentSlotIndex);
    }

    // -- UPDATE ACTIVE HIGHLIGHT --
    private void UpdateActiveHighlight(int activeSlot)
    {
        if (activeHighlight == null) return;
        if (activeSlot < 0 || activeSlot >= inventorySlots.Length) return;

        Image targetSlot = inventorySlots[activeSlot];
        if (targetSlot == null) return;

        activeHighlight.position = targetSlot.rectTransform.position;
    }

    // -- SET BACKGROUND OPACITY --
    private void SetBackgroundAlpha(int index, float alpha)
    {
        if (inventorySlotsBackgrounds == null)
            return;

        if (index < 0 || index >= inventorySlotsBackgrounds.Length)
            return;

        Image background = inventorySlotsBackgrounds[index];
        if (background == null)
            return;

        Color color = background.color;
        color.a = alpha;
        background.color = color;
        background.enabled = true;
    }

    // -- HIDE ALL INVENTORY SLOTS --
    private void HideAllInventorySlots()
    {
        foreach (Image slot in inventorySlots)
        {
            if (slot != null)
            {
                slot.sprite = null;
                slot.enabled = false;
            }
        }

        if (inventorySlotsBackgrounds == null)
            return;

        for (int i = 0; i < inventorySlotsBackgrounds.Length; i++)
        {
            if (inventorySlotsBackgrounds[i] != null)
            {
                Color color = inventorySlotsBackgrounds[i].color;
                color.a = emptySlotAlpha;
                inventorySlotsBackgrounds[i].color = color;
                inventorySlotsBackgrounds[i].enabled = true;
            }
        }
    }
}