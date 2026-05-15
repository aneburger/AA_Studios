using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] public WeaponData weaponData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerWeaponSlot handler = other.GetComponentInParent<PlayerWeaponSlot>();
        if (handler == null) return;
        handler.SetNearbyPickup(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerWeaponSlot handler = other.GetComponentInParent<PlayerWeaponSlot>();
        if (handler == null) return;
        handler.ClearNearbyPickup(this);
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }
}