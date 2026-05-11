// Extends BaseShooter
// Gets shoot direction from PlayerAim
// Handles input and screen shake

using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerShooter : BaseShooter
{
    [Header("Player References")]
    [SerializeField] private PlayerAimer aim;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    private bool isShooting = false;

    // -- START --
    private void Start()
    {
        UpdateWeaponVisuals();
    }

    // -- UPDATE --
    private void Update()
    {
        if (!IsArmed) return;

        if (isShooting && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + currentWeapon.fireRate;
        }
    }

    // -- GET SHOOT DIRECTION --
    protected override Vector2 GetShootDirection()
    {
        return aim.AimDirection;
    }

    // -- SHOOT EFFECTS --
    protected override void OnShootEffects(Vector2 direction)
    {
        base.OnShootEffects(direction);
        impulseSource?.GenerateImpulse(currentWeapon.shakeForce);
    }

    // -- SHOOT INPUT --
    public void OnShoot(InputAction.CallbackContext context)
    {
        if (!IsArmed) return;
        if (context.started)  isShooting = true;
        if (context.canceled) isShooting = false;
    }
}