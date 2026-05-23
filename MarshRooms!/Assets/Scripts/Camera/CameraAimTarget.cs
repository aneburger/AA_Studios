using UnityEngine;
using UnityEngine.InputSystem;

public class CameraAimTarget : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float aimDistance;
    [SerializeField] private float smoothSpeed;
    [SerializeField] private float maxOffset;
    [SerializeField] private float deadZoneRadius;

    private Camera cam;
    private Vector2 currentPos;

    // -- AWAKE --
    private void Awake()
    {
        cam = Camera.main;
        currentPos = player.position;
    }

    // -- UPDATE --
    private void Update()
    {
        Vector2 mouseWorld = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 toMouse = mouseWorld - (Vector2)player.position;

        float distance = toMouse.magnitude;
        float adjustedDistance = Mathf.Max(0f, distance - deadZoneRadius);
        float clampedDistance = Mathf.Clamp(adjustedDistance / maxOffset, 0f, 1f);
        float curvedDistance = Mathf.Pow(clampedDistance, 1.5f);

        Vector2 targetPos = (Vector2)player.position + toMouse.normalized * curvedDistance * aimDistance;
        currentPos = Vector2.Lerp(currentPos, targetPos, Time.deltaTime * smoothSpeed);
        transform.position = currentPos;
    }
}