// Handles the weapon pickup behaviour.

using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("Weapon Data")]
    [SerializeField] public WeaponData weaponData;

    public int ammo = -1;

    private Interactable interactable;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    private PlayerWeaponSlot cachedWeaponSlot;

    // -- AWAKE --
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        spriteRenderer.sortingOrder = -1000;

        interactable = GetComponent<Interactable>();
        if (interactable != null)
            interactable.OnInteract += HandlePickup;
    }

    // -- ON ENABLE --
    private void OnEnable()
    {
        if (col == null) return;
        col.enabled = false;
        Invoke(nameof(EnableCollider), 0.2f);
    }

    // -- ENABLE COLLIDER --
    private void EnableCollider()
    {
        col.enabled = true;
    }

    // -- HANDLE PICKUP --
    private void HandlePickup()
    {
        if (cachedWeaponSlot == null) return;
        cachedWeaponSlot.PickupWeapon();
    }

    // -- ON DESTROY --
    public void Destroy()
    {
        interactable?.Disable();
        Destroy(gameObject);
    }

    // -- ENTER --
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerWeaponSlot handler = other.GetComponentInParent<PlayerWeaponSlot>();
        if (handler == null) return;
        cachedWeaponSlot = handler;
        handler.SetNearbyPickup(this);
    }

    // -- EXIT --
    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerWeaponSlot handler = other.GetComponentInParent<PlayerWeaponSlot>();
        if (handler == null) return;
        cachedWeaponSlot = null;
        handler.ClearNearbyPickup(this);
    }
}