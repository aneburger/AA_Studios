// This script calculates what direction the mouse is relative to the player

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAim : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform aimOrigin;

    public Vector2 AimDirection { get; private set; }

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void Update()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(mouseScreen);

        mouseWorld.z = 0f;
        Vector2 dir = mouseWorld - aimOrigin.position;

        AimDirection = dir.normalized;

        // Comment out if you want to seee the aim direction for debugging
        Debug.DrawRay(aimOrigin.position, AimDirection * 2f, Color.red);
    }
}