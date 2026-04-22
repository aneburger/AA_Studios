using UnityEngine;
using UnityEngine.UI;

public sealed class ShootingController : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform player;

    // transform that will rotate around player to represent weapon
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private SpriteRenderer weaponRenderer;
    [SerializeField] private Image aimCursorImage;
    [SerializeField] private Animator playerAnimator;

    // how far from the player the weapon should float
    [SerializeField] private float weaponRadius = 0.35f;
    // how smooth weapon movement is.. higher nr = less smooth.
    [SerializeField] private float weaponFollowSpeed = 25f;
    // offset used to align the weapon art with the computed aim angle.
    [SerializeField] private float weaponRotationOffsetDegrees = 0f;

    // player faces direction of cursor
    [SerializeField] private bool driveAnimatorFacingFromAim = true;

    // minimum time between shots in seconds
    [SerializeField] private float shootCooldownSeconds = 0.15f;
    [SerializeField] private float debugShotLength = 2.0f;

    private float _shootCooldownRemaining;

    private void Reset()
    {
        worldCamera = Camera.main;
        player = transform;
        playerAnimator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (player == null)
        {
            player = transform;
        }

        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Animator>();
        }
    }

    private void OnEnable()
    {
        // hides the arrow cursor so we can use our aim-cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void OnDisable()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        if (worldCamera == null || player == null)
        {
            return;
        }

        // mouse position in screen coordinates.. (0,0) = bottom left
        Vector3 mouseScreen = Input.mousePosition;

        if (aimCursorImage != null)
        {
            aimCursorImage.rectTransform.position = mouseScreen;
        }

        Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = player.position.z;

        // aim direction from player to mouse
        Vector2 aimDir = (mouseWorld - player.position);
        // avoid zero-length vectors when mouse is exactly on the player.
        if (aimDir.sqrMagnitude < 0.0001f)
        {
            aimDir = Vector2.down;
        }

        aimDir.Normalize();

        // rotate the weapon pivot.
        UpdateWeapon(aimDir);
        // face character in cursor direction.
        UpdateFacingAnimation(aimDir);
        // handle left-click shooting
        UpdateShooting(aimDir);
    }

    private void UpdateWeapon(Vector2 aimDir)
    {
        if (weaponPivot == null)
        {
            return;
        }

        // target position is an offset from the player in the aim direction.
        Vector3 targetPos = player.position + (Vector3)(aimDir * weaponRadius);
        // move pivot toward target position
        // Exp makes smooth movement independent from frame rate
        weaponPivot.position = Vector3.Lerp(
            weaponPivot.position,
            targetPos,
            1f - Mathf.Exp(-weaponFollowSpeed * Time.deltaTime));

        // convert aim direction to an angle in degrees
        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg + weaponRotationOffsetDegrees;
        // apply rotation around Z axis... 2D.
        weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);

        if (weaponRenderer != null)
        {
            weaponRenderer.flipY = aimDir.x < 0f;
        }
    }

    private void UpdateFacingAnimation(Vector2 aimDir)
    {
        if (!driveAnimatorFacingFromAim || playerAnimator == null)
        {
            return;
        }

        // character can only face 4 directions
        Vector2 facing = QuantizeTo4Directions(aimDir);
        playerAnimator.SetFloat("x", facing.x);
        playerAnimator.SetFloat("y", facing.y);
    }

    private void UpdateShooting(Vector2 aimDir)
    {
        if (_shootCooldownRemaining > 0f)
        {
            _shootCooldownRemaining -= Time.deltaTime;
        }

        if (_shootCooldownRemaining > 0f)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        _shootCooldownRemaining = shootCooldownSeconds;

        Vector3 start = weaponPivot != null ? weaponPivot.position : player.position;
        Vector3 end = start + (Vector3)(aimDir * debugShotLength);

        // draw a visible line in the Scene view for a short duration (placeholder for projectile)
        Debug.DrawLine(start, end, Color.yellow, 0.2f);
    }

    // converts direction vector to one of 4 directions
    private static Vector2 QuantizeTo4Directions(Vector2 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
        {
            return v.x >= 0f ? Vector2.right : Vector2.left;
        }

        return v.y >= 0f ? Vector2.up : Vector2.down;
    }
}
