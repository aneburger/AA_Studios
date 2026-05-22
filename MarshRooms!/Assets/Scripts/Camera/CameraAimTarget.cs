using UnityEngine;
using UnityEngine.InputSystem;

public class CameraAimTarget : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float aimDistance = 2f;
    [SerializeField] private float deadZoneRadius = 1f;

    private Vector3 velocity;
    private Camera cam;

    // -- AWAKE --
    private void Awake()
    {
        cam = Camera.main;
    }

    // -- UPDATE --
    private void Update()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
        Vector2 toMouse = mouseWorld - (Vector2)player.position;
        float distance = toMouse.magnitude;

        if (distance < deadZoneRadius)
        {
            transform.position = player.position;
            return;
        }

        Vector2 offset = Vector2.ClampMagnitude(toMouse, aimDistance);
        transform.position = (Vector2)player.position + offset;
    }
}