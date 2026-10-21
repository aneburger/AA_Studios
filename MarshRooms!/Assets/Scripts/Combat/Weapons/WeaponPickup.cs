// Handles the weapon pickup behaviour.

using UnityEngine;
using System.Collections;

public class WeaponPickup : MonoBehaviour
{
    [Header("Weapon Data")]
    [SerializeField] public WeaponData weaponData;
    [SerializeField] public bool setsAsDefault = false;
    [SerializeField] public bool isMinigunTutorialPickup = false;

    [Header("Drop Animation")]
    [SerializeField] private bool playDropAnimation = true;
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float hopHeight = 0.35f;
    [SerializeField] private float hopDuration = 0.3f;
    [SerializeField] private float landSquash = 0.2f;
    [SerializeField] private float landSquashDuration = 0.1f;
    [SerializeField] private float pickupDelay = 0.1f;
    [SerializeField] private bool snapHopToPixels = true;
    [SerializeField] private float pixelsPerUnit = 16f;

    private Vector3 baseScale;
    private Vector3 basePosition;
    private Coroutine dropRoutine;

    public int ammo = -1;

    private Interactable interactable;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    private PlayerWeaponSlot cachedWeaponSlot;

    // -- AWAKE --
    private void Awake()
    {   
        baseScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        spriteRenderer.sortingOrder = -1000;

        interactable = GetComponent<Interactable>();
        if (interactable != null)
            interactable.OnInteract += HandlePickup;
    }

    // -- ENABLE COLLIDER --
    private void EnableCollider()
    {
        col.enabled = true;
    }

    // -- ON ENABLE --
    private void OnEnable()
    {
        if (col == null) return;
        col.enabled = false;
        Invoke(nameof(EnableCollider), pickupDelay);

        if (playDropAnimation)
        {
            basePosition = transform.position;
            dropRoutine = StartCoroutine(DropAnimation());
        }
    }

    // -- ON DISABLE --
    private void OnDisable()
    {
        CancelInvoke();
        if (dropRoutine != null)
        {
            StopCoroutine(dropRoutine);
            dropRoutine = null;
            transform.position = basePosition;
            transform.localScale = baseScale;
        }
    }

    // -- HANDLE PICKUP --
    private void HandlePickup()
    {
        if (cachedWeaponSlot == null) return;
        cachedWeaponSlot.PickupWeapon();

        if (setsAsDefault)
            TutorialDirector.Instance?.OnPlungerPickedUp();

        if (isMinigunTutorialPickup)
            TutorialDirector.Instance?.OnMinigunPickedUp();
    }

    // -- ON DESTROY --
    public void Destroy()
    {
        interactable?.Disable();
        Destroy(gameObject);
    }

    // -- ENTER --
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerWeaponSlot handler = other.GetComponentInParent<PlayerWeaponSlot>();
        if (handler == null) return;
        cachedWeaponSlot = handler;
        handler.SetNearbyPickup(this);
    }

    // -- EXIT --
    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerWeaponSlot handler = other.GetComponentInParent<PlayerWeaponSlot>();
        if (handler == null) return;
        cachedWeaponSlot = null;
        handler.ClearNearbyPickup(this);
    }

    // -- DROP ANIMATION --
    private IEnumerator DropAnimation()
    {
        transform.localScale = Vector3.zero;

        float total = Mathf.Max(popDuration, hopDuration);
        float t = 0f;

        while (t < total)
        {
            t += Time.deltaTime;

            // Scale pop with overshoot
            float p = Mathf.Clamp01(t / popDuration);
            transform.localScale = baseScale * EaseOutBack(p);

            // Hop arc (parabola)
            float h = Mathf.Clamp01(t / hopDuration);
            float y = 4f * hopHeight * h * (1f - h);
            if (snapHopToPixels) y = Mathf.Round(y * pixelsPerUnit) / pixelsPerUnit;
            transform.position = basePosition + Vector3.up * y;

            yield return null;
        }

        transform.position = basePosition;

        // Landing squash
        float s = 0f;
        while (s < landSquashDuration)
        {
            s += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(s / landSquashDuration) * Mathf.PI) * landSquash;
            transform.localScale = new Vector3(baseScale.x * (1f + k), baseScale.y * (1f - k), baseScale.z);
            yield return null;
        }

        transform.localScale = baseScale;
        dropRoutine = null;
    }

    // -- SKIP DROP ANIMATION --
    public void SkipDropAnimation()
    {
        if (dropRoutine == null) return;

        StopCoroutine(dropRoutine);
        dropRoutine = null;
        transform.position = basePosition;
        transform.localScale = baseScale;
    }

    // -- EASE OUT BACK --
    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}