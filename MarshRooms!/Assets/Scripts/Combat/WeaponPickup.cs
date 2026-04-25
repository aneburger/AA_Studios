using UnityEngine;

// When player walks over a weapon it is picked up

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponData weaponData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Shooter shooter = other.GetComponentInParent<Shooter>();

        if (shooter == null) return;

        shooter.EquipWeapon(weaponData);
        Destroy(gameObject);
    }
}