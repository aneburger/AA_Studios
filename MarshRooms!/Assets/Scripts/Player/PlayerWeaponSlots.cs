// Manages player weapon slots

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

    public event System.Action<WeaponData> OnWeaponChanged;
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
        TutorialDirector.Instance?.OnWeaponScrolled();
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
        TutorialDirector.Instance?.OnWeaponScrolled();
    }

    // -- PICKUP --
    public void PickupWeapon()
    {
        WeaponPickup nearbyPickup = GetNearestPickup();
        if (nearbyPickup == null) return;

        SaveCurrentAmmo();

        WeaponData incoming = nearbyPickup.weaponData;
        int incomingAmmo = nearbyPickup.ammo == -1 ? incoming.maxAmmo : nearbyPickup.ammo;

        // -- SETS AS DEFAULT WEAPON -- 
        if (nearbyPickup.setsAsDefault)
        {
            SaveCurrentAmmo();
            if (!DropWeapon(0)) return;

            slots[0] = incoming;
            ammo[0] = incomingAmmo;
            nearbyPickup.Destroy();
            currentSlot = 0;
            EquipCurrentSlot(true);
            return;
        }

        // -- ALREADY CARRYING THIS WEAPON -- top up ammo instead of picking up
        for (int i = 1; i < slots.Length; i++)
        {
            if (slots[i] != incoming) continue;

            ammo[i] = Mathf.Min(ammo[i] + incomingAmmo, incoming.maxAmmo);
            nearbyPickup.Destroy();
            currentSlot = i;
            EquipCurrentSlot(true);
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
            EquipCurrentSlot(true);
            return;
        }

        // -- INVENTORY FULL -- drop current weapon and replace with incoming
        int dropSlot = currentSlot == 0 ? 1 : currentSlot;
        if (!DropWeapon(dropSlot)) return;

        slots[dropSlot] = incoming;
        ammo[dropSlot]  = incomingAmmo;
        nearbyPickup.Destroy();
        currentSlot = dropSlot;
        EquipCurrentSlot(true);
    }

    // -- DROP WEAPON -- spawns pickup in world with remaining ammo
    private bool DropWeapon(int slot)
    {
        if (slots[slot] == null) return true;

        if (slots[slot].pickupPrefab == null)
        {
            return false;
        }

        Vector2 randomOffset = Random.insideUnitCircle * 0.6f;
        Vector2 dropPos = (Vector2)transform.position + randomOffset;

        RoomManager room = FindFirstObjectByType<RoomManager>();
        if (room != null)
            dropPos = room.GetSafeDropPosition(dropPos);

        GameObject dropped = Instantiate(slots[slot].pickupPrefab, dropPos, Quaternion.identity);

        WeaponPickup pickup = dropped.GetComponent<WeaponPickup>();
        if (pickup != null)
            pickup.ammo = ammo[slot];

        slots[slot] = null;
        ammo[slot]  = 0;
        return true;
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
        pickup.GetComponent<Interactable>()?.SetOutline(false);
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
    private void EquipCurrentSlot(bool isPickup = false, bool playSound = true)
    {
        if (slots[currentSlot] == null) return;
        shooter.EquipWeapon(slots[currentSlot], isPickup, playSound);
        shooter.SetAmmo(ammo[currentSlot]);
        OnWeaponChanged?.Invoke(slots[currentSlot]);
        //UpdateWeaponDisplay();
    }

    // -- UPDATE WEAPON DISPLAY --
    //private void UpdateWeaponDisplay()
    //{
    //    if (HUDManager.Instance != null && slots[currentSlot] != null)
    //    {
    //        HUDManager.Instance.UpdateWeaponDisplay(slots[currentSlot]);
    //    }
    //}

    // -- SKIP EMPTY SLOTS --
    private void SkipEmptySlots(int direction, ref int slot)
    {
        for (int i = 0; i < maxWeapons; i++)
        {
            if (slots[slot] != null) break;
            slot = (slot + direction + maxWeapons) % maxWeapons;
        }
    }

    // -- GET WEAPON AT SLOT --
    public WeaponData GetWeaponAtSlot(int slot)
    {
        if (slot < 0 || slot >= slots.Length)
            return null;
        return slots[slot];
    }

    // -- HAS ANY WEAPON --
    public bool HasAnyWeapon()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) return true;
        }
        return false;
    }

    // -- GRANT WEAPON --
    public void GrantWeapon(WeaponData weapon, int ammoAmount = -1)
    {
        if (weapon == null) return;
        if (slots[0] != null) return;

        slots[0] = weapon;
        ammo[0] = ammoAmount == -1 ? weapon.maxAmmo : ammoAmount;
        currentSlot = 0;
        EquipCurrentSlot(true, false);
    }
}