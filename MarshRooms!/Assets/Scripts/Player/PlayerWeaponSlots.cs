// Manages player weapon slots
// Max 3 weapons, scroll to switch, E to pickup

using UnityEngine;
using System.Collections.Generic;

public class PlayerWeaponSlot : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private WeaponData defaultWeapon;
    [SerializeField] private int maxWeapons = 3;

    public GameObject pickupPrefab;
    private PlayerShooter shooter;

    private int[] ammo;
    private WeaponData[] slots;
    private int currentSlot = 0;
    
    private List<WeaponPickup> nearbyPickups = new List<WeaponPickup>();
    
    // -- AWAKE --
    private void Awake()
    {
        shooter = GetComponent<PlayerShooter>();

        slots = new WeaponData[maxWeapons];
        slots[0] = defaultWeapon;

        ammo = new int[maxWeapons];
        ammo[0] = -1;
    }

    // -- START --
    private void Start()
    {
        EquipCurrentSlot();
    }

    // -- SCROLL UP --
    public void ScrollUp()
    {
        SaveCurrentAmmo();

        int newSlot = (currentSlot - 1 + maxWeapons) % maxWeapons;
        SkipEmptySlots(-1, ref newSlot);
        if (newSlot == currentSlot) return;
        currentSlot = newSlot;

        EquipCurrentSlot();
    }

    // -- SCROLL DOWN --
    public void ScrollDown()
    {
        SaveCurrentAmmo();

        int newSlot = (currentSlot + 1) % maxWeapons;
        SkipEmptySlots(1, ref newSlot);
        if (newSlot == currentSlot) return;
        currentSlot = newSlot;
        
        EquipCurrentSlot();
    }

    // -- PICKUP --
    public void PickupWeapon()
    {
        WeaponPickup nearbyPickup = GetNearestPickup();
        if (nearbyPickup == null) return;

        SaveCurrentAmmo();

        WeaponData incoming = nearbyPickup.weaponData;
        int incomingAmmo = nearbyPickup.ammo == -1 ? incoming.maxAmmo : nearbyPickup.ammo;

        // -- ALREADY CARRYING THIS WEAPON -- top up ammo instead of picking up
        for (int i = 1; i < slots.Length; i++)
        {
            if (slots[i] != incoming) continue;

            ammo[i] = Mathf.Min(ammo[i] + incomingAmmo, incoming.maxAmmo);
            nearbyPickup.Destroy();
            currentSlot = i;
            EquipCurrentSlot();
            return;
        }

        // -- EMPTY SLOT AVAILABLE -- place weapon in first free slot
        for (int i = 1; i < slots.Length; i++)
        {
            if (slots[i] != null) continue;

            slots[i] = incoming;
            ammo[i]  = incomingAmmo;
            nearbyPickup.Destroy();
            currentSlot = i;
            EquipCurrentSlot();
            return;
        }

        // -- INVENTORY FULL -- drop current weapon and replace with incoming
        int dropSlot = currentSlot == 0 ? 1 : currentSlot;
        DropWeapon(dropSlot);

        slots[dropSlot] = incoming;
        ammo[dropSlot]  = incomingAmmo;
        nearbyPickup.Destroy();
        currentSlot = dropSlot;
        EquipCurrentSlot();
    }

    // -- DROP WEAPON -- spawns pickup in world with remaining ammo
    private void DropWeapon(int slot)
    {
        if (slots[slot] == null) return;
        if (slots[slot].pickupPrefab == null) return;

        GameObject dropped = Instantiate(slots[slot].pickupPrefab, transform.position, Quaternion.identity);
        
        // Pass remaining ammo to the pickup
        WeaponPickup pickup = dropped.GetComponent<WeaponPickup>();
        if (pickup != null)
            pickup.ammo = ammo[slot];

        slots[slot] = null;
        ammo[slot]  = 0;
    }

    // -- SAVE CURRENT AMMO --
    private void SaveCurrentAmmo()
    {
        ammo[currentSlot] = shooter.GetAmmo();
    }

    // -- SET NEARBY PICKUP --
    public void SetNearbyPickup(WeaponPickup pickup)
    {
        if (!nearbyPickups.Contains(pickup))
            nearbyPickups.Add(pickup);
    }

    // -- CLEAR NEARBY PICKUP --
    public void ClearNearbyPickup(WeaponPickup pickup)
    {
        nearbyPickups.Remove(pickup);
        pickup.SetOutline(false);
    }

    // -- GET NEAREST PICKUP --
    private WeaponPickup GetNearestPickup()
    {
        if (nearbyPickups.Count == 0) return null;

        WeaponPickup nearest = null;
        float closest = float.MaxValue;

        foreach (WeaponPickup pickup in nearbyPickups)
        {
            float dist = Vector2.Distance(transform.position, pickup.transform.position);
            if (dist < closest)
            {
                closest = dist;
                nearest = pickup;
            }
        }

        return nearest;
    }

    // -- EQUIP CURRENT SLOT --
    private void EquipCurrentSlot()
    {
        if (slots[currentSlot] == null) return;
        shooter.EquipWeapon(slots[currentSlot]);
        shooter.SetAmmo(ammo[currentSlot]);

        // Update HUD with current weapon sprite
        UpdateWeaponDisplay();
    }

    // -- UPDATE WEAPON DISPLAY --
    private void UpdateWeaponDisplay()
    {
        if (HUDManager.Instance != null && slots[currentSlot] != null)
        {
            HUDManager.Instance.UpdateWeaponDisplay(slots[currentSlot]);
        }
    }

    // -- SKIP EMPTY SLOTS --
    private void SkipEmptySlots(int direction, ref int slot)
    {
        for (int i = 0; i < maxWeapons; i++)
        {
            if (slots[slot] != null) break;
            slot = (slot + direction + maxWeapons) % maxWeapons;
        }
    }
}