/*using UnityEngine;

public class CursorController : MonoBehaviour
{
    public Texture2D cursorTexture;
    public Vector2 hotspot = Vector2.zero;

    void Start()
    {
        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
    }
}*/

using UnityEngine;
using UnityEngine.UI;

public sealed class CursorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform player;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private SpriteRenderer weaponRenderer;
    [SerializeField] private Image aimCursorImage;
    [SerializeField] private Animator playerAnimator;

    [Header("Weapon Float")]
    [SerializeField] private float weaponRadius = 0.35f;
    [SerializeField] private float weaponFollowSpeed = 25f;

    [Tooltip("If the plunger art points 'up' by default, try 90 / -90.")]
    [SerializeField] private float weaponRotationOffsetDegrees = 0f;

    [Header("Facing / Animation")]
    [SerializeField] private bool driveAnimatorFacingFromAim = true;

    [Header("Shooting (placeholder)")]
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

        Vector3 mouseScreen = Input.mousePosition;

        if (aimCursorImage != null)
        {
            aimCursorImage.rectTransform.position = mouseScreen;
        }

        Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = player.position.z;

        Vector2 aimDir = (mouseWorld - player.position);
        if (aimDir.sqrMagnitude < 0.0001f)
        {
            aimDir = Vector2.down;
        }

        aimDir.Normalize();

        UpdateWeapon(aimDir);
        UpdateFacingAnimation(aimDir);
        UpdateShooting(aimDir);
    }

    private void UpdateWeapon(Vector2 aimDir)
    {
        if (weaponPivot == null)
        {
            return;
        }

        Vector3 targetPos = player.position + (Vector3)(aimDir * weaponRadius);
        weaponPivot.position = Vector3.Lerp(
            weaponPivot.position,
            targetPos,
            1f - Mathf.Exp(-weaponFollowSpeed * Time.deltaTime));

        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg + weaponRotationOffsetDegrees;
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

        if (!Input.GetMouseButtonDown(1))
        {
            return;
        }

        _shootCooldownRemaining = shootCooldownSeconds;

        Vector3 start = weaponPivot != null ? weaponPivot.position : player.position;
        Vector3 end = start + (Vector3)(aimDir * debugShotLength);

        Debug.DrawLine(start, end, Color.yellow, 0.2f);
    }

    private static Vector2 QuantizeTo4Directions(Vector2 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
        {
            return v.x >= 0f ? Vector2.right : Vector2.left;
        }

        return v.y >= 0f ? Vector2.up : Vector2.down;
    }
}
