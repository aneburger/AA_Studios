// Spawns bullets and assigns direction from PlayerAim.

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private PlayerAim aim;

    [Header("Fire Settings")]
    [SerializeField] private float fireRate;

    private float nextFireTime = 0f;
    private bool isShooting = false;

    // -- UPDATE --
    private void Update()
    {   
        // Control fire rate
        if (isShooting && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    // -- SHOOT INPUT --
    public void OnShoot(InputAction.CallbackContext context)
    {
        if (context.started)  isShooting = true;
        if (context.canceled) isShooting = false;
    }

    // -- SHOOT --
    private void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.GetComponent<Bullet>().SetDirection(aim.AimDirection);
    }
}
