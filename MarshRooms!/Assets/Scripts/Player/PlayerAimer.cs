// This script calculates what direction from the AimOrigin to the mouse
// Gives direction vector used by weapons and bullets

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAimer : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform aimOrigin;
    [SerializeField] private WeaponAimer weaponAimer;

    private Vector2? aimOverride = null;

    public Vector2 AimDirection { get; private set; }

    // -- AWAKE --
    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    // -- UPDATE --
    private void Update()
    {
        if (aimOverride.HasValue)
        {
            AimDirection = aimOverride.Value;
            weaponAimer?.SetAimDirection(AimDirection);
            return;
        }

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(mouseScreen);

        mouseWorld.z = 0f;
        Vector2 dir = mouseWorld - aimOrigin.position;

        AimDirection = dir.normalized;

        if (weaponAimer != null)
            weaponAimer.SetAimDirection(AimDirection);
    }

    // -- SET AIM OVERRIDE --
    public void SetAimOverride(Vector2 direction)
    {
        aimOverride = direction.normalized;
        AimDirection = aimOverride.Value;
        weaponAimer?.SetAimDirection(AimDirection);
    }

    // -- CLEAR AIM OVERRIDE --
    public void ClearAimOverride()
    {
        aimOverride = null;
    }
}