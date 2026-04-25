using UnityEngine;

// When player walks over a weapon it is picked up

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] public WeaponData weaponData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        BaseShooter shooter = other.GetComponentInParent<BaseShooter>();

        if (shooter == null) return;

        shooter.EquipWeapon(weaponData);
        Destroy(gameObject);
    }
}