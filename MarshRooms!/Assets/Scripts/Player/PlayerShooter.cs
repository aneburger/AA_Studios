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

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip shootClip;
    [SerializeField, Range(0f, 1f)] private float shootVolume = 1f;

    private bool isShooting = false;

    // -- START --
    private void Start()
    {
        UpdateWeaponVisuals();

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }
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

        if (sfxSource != null && shootClip != null)
        {
            sfxSource.PlayOneShot(shootClip, shootVolume);
        }
    }

    // -- SHOOT INPUT --
    public void OnShoot(InputAction.CallbackContext context)
    {
        if (!IsArmed) return;
        if (context.started)  isShooting = true;
        if (context.canceled) isShooting = false;
    }
}