using UnityEngine;
using UnityEngine.UI;

public class InventoryDisplay : MonoBehaviour
{
    [Header("Inventory Container")]
    [SerializeField] private GameObject inventoryContainer;

    [Header("Active Weapon Slot")]
    [SerializeField] private Image activeWeaponSlot;

    [Header("Inventory Slots")]
    [SerializeField] private Image[] inventorySlots = new Image[3];

    private PlayerShooter playerShooter;
    private PlayerWeaponSlot weaponSlots;

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
        if (playerShooter.currentWeapon != null && activeWeaponSlot != null)
        {
            activeWeaponSlot.sprite = playerShooter.currentWeapon.hudSprite;
            activeWeaponSlot.enabled = true;
        }
        else if (activeWeaponSlot != null)
        {
            activeWeaponSlot.enabled = false;
        }

        // update inventory slots for all weapons
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] == null) continue;

            WeaponData weapon = weaponSlots.GetWeaponAtSlot(i);

            if (weapon != null)
            {
                inventorySlots[i].sprite = weapon.hudSprite;
                inventorySlots[i].enabled = true;
            }
            else
            {
                inventorySlots[i].enabled = false;
            }
        }
    }

    // -- HIDE ALL INVENTORY SLOTS --
    private void HideAllInventorySlots()
    {
        foreach (Image slot in inventorySlots)
        {
            if (slot != null)
                slot.enabled = false;
        }

        if (activeWeaponSlot != null)
            activeWeaponSlot.enabled = false;
    }
}