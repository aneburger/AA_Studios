// This script calculates what direction from the AimOrigin to the mouse
// Gives direction vector used by weapons and bullets

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAimer : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform aimOrigin;
    [SerializeField] private WeaponAimer weaponAimer;

    public Vector2 AimDirection { get; private set; }

    // -- Awake --
    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    // -- Update --
    private void Update()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(mouseScreen);

        mouseWorld.z = 0f;
        Vector2 dir = mouseWorld - aimOrigin.position;

        AimDirection = dir.normalized;

        if (weaponAimer != null)
            weaponAimer.SetAimDirection(AimDirection);

        // -- DEBUG --
        Debug.DrawRay(aimOrigin.position, AimDirection * 2f, Color.red);
    }
}