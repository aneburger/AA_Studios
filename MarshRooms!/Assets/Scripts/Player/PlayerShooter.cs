// Extends BaseShooter
// Gets shoot direction from PlayerAim
// Handles input and screen shake

using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

using TopDown.Movement;

public class PlayerShooter : BaseShooter
{
    [Header("Player References")]
    [SerializeField] private PlayerAimer aim;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private PlayerMover playerMover;

    private bool isShooting = false;

    private float fireRateMultiplier = 1f;

    // -- START --
    private void Start()
    {
        UpdateWeaponVisuals();
    }

    // -- UPDATE --
    private void Update()
    {
        if (!IsArmed) return;
        if (playerMover.IsDodging) return;

        if (isShooting && Time.time >= nextFireTime)
        {
            BaseBullet bullet = Shoot();
            if (bullet != null && SporeManager.Instance.IsMutated)
            {
                GetComponent<PlayerMutatedVisuals>()?.PlayShootBurst(GetFirePosition());
            }
            nextFireTime = Time.time + (currentWeapon.fireRate * fireRateMultiplier);
        }
    }

    // -- SET FIRE RATE --
    public void SetFireRateMultiplier(float multiplier)
    {
        fireRateMultiplier = multiplier;
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
        impulseSource?.GenerateImpulse(currentWeapon.shakeForce * shakeMultiplier);
    }

    // -- SHOOT INPUT --
    public void OnShoot(InputAction.CallbackContext context)
    {
        if (context.started && IsArmed && !playerMover.IsDodging)
            isShooting = true;

        if (context.canceled)
            isShooting = false;
    }

    // -- STOP SHOOTING --
    public void StopShooting()
    {
        isShooting = false;
    }
}