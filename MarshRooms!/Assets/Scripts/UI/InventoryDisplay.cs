using UnityEngine;
using UnityEngine.UI;

public class InventoryDisplay : MonoBehaviour
{
    [Header("Inventory Container")]
    [SerializeField] private GameObject inventoryContainer;

    [Header("Inventory Slots")]
    [SerializeField] private Image[] inventorySlots = new Image[3];

    private PlayerShooter playerShooter;
    private PlayerWeaponSlot weaponSlots;
    private int currentDisplaySlot = 0;

    private void Start()
    {
        playerShooter = FindObjectOfType<PlayerShooter>();
        weaponSlots = FindObjectOfType<PlayerWeaponSlot>();

        HideAllInventorySlots();
    }

    private void Update()
    {
        if (playerShooter == null || weaponSlots == null) return;

        UpdateInventoryDisplay();
    }

    // -- UPDATE INVENTORY DISPLAY --
    private void UpdateInventoryDisplay()
    {
        int currentSlot = GetCurrentWeaponSlot();

        if (currentSlot != currentDisplaySlot)
        {
            currentDisplaySlot = currentSlot;
        }

        for (int displayIndex = 0; displayIndex < inventorySlots.Length; displayIndex++)
        {
            if (inventorySlots[displayIndex] == null) continue;

            int slotIndex = currentDisplaySlot + displayIndex;

            // wrap around if it goes past slot 2
            if (slotIndex >= 3)
                slotIndex -= 3;

            WeaponData weapon = weaponSlots.GetWeaponAtSlot(slotIndex);

            if (weapon != null)
            {
                inventorySlots[displayIndex].sprite = weapon.hudSprite;
                inventorySlots[displayIndex].enabled = true;
            }
            else
            {
                inventorySlots[displayIndex].enabled = false;
            }
        }
    }

    // -- GET CURRENT WEAPON SLOT --
    private int GetCurrentWeaponSlot()
    {
        for (int i = 0; i < 3; i++)
        {
            WeaponData weapon = weaponSlots.GetWeaponAtSlot(i);
            if (weapon != null && weapon == playerShooter.currentWeapon)
                return i;
        }
        return 0; 
    }

    // -- HIDE ALL INVENTORY SLOTS --
    private void HideAllInventorySlots()
    {
        foreach (Image slot in inventorySlots)
        {
            if (slot != null)
                slot.enabled = false;
        }
    }
}