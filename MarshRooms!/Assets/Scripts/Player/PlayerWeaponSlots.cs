// Manages player weapon slots
// Max 3 weapons, scroll to switch, E to pickup

using UnityEngine;

public class PlayerWeaponSlot : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private WeaponData defaultWeapon;
    [SerializeField] private int maxWeapons = 3;

    private WeaponData[] slots;
    private int currentSlot = 0;
    private PlayerShooter shooter;
    private WeaponPickup nearbyPickup;

    // -- AWAKE --
    private void Awake()
    {
        shooter = GetComponent<PlayerShooter>();
        slots = new WeaponData[maxWeapons];
        slots[0] = defaultWeapon;
    }

    // -- START --
    private void Start()
    {
        EquipCurrentSlot();
    }

    // -- SCROLL UP --
    public void ScrollUp()
    {
        currentSlot = (currentSlot - 1 + maxWeapons) % maxWeapons;
        SkipEmptySlots(-1);
        EquipCurrentSlot();
    }

    // -- SCROLL DOWN --
    public void ScrollDown()
    {
        currentSlot = (currentSlot + 1) % maxWeapons;
        SkipEmptySlots(1);
        EquipCurrentSlot();
    }

    // -- PICKUP --
    public void PickupWeapon()
    {
        if (nearbyPickup == null) return;

        // Find empty slot
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = nearbyPickup.weaponData;
                currentSlot = i;
                nearbyPickup.Destroy();
                nearbyPickup = null;
                EquipCurrentSlot();
                return;
            }
        }

        // No empty slot — swap with current
        slots[currentSlot] = nearbyPickup.weaponData;
        nearbyPickup.Destroy();
        nearbyPickup = null;
        EquipCurrentSlot();
    }

    // -- SET NEARBY PICKUP --
    public void SetNearbyPickup(WeaponPickup pickup)
    {
        nearbyPickup = pickup;
    }

    // -- CLEAR NEARBY PICKUP --
    public void ClearNearbyPickup(WeaponPickup pickup)
    {
        if (nearbyPickup == pickup)
            nearbyPickup = null;
    }

    // -- EQUIP CURRENT SLOT --
    private void EquipCurrentSlot()
    {
        if (slots[currentSlot] != null)
            shooter.EquipWeapon(slots[currentSlot]);
    }

    // -- SKIP EMPTY SLOTS --
    private void SkipEmptySlots(int direction)
    {
        for (int i = 0; i < maxWeapons; i++)
        {
            if (slots[currentSlot] != null) break;
            currentSlot = (currentSlot + direction + maxWeapons) % maxWeapons;
        }
    }
}