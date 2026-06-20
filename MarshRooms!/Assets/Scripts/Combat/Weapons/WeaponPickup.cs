// Shows an outline when the player enters pickup range
// Hides the outline when the player leaves

using UnityEngine;

public class WeaponPickup : MonoBehaviour
{   
    [Header("Weapon Data")]
    [SerializeField] public WeaponData weaponData;

    [Header("Indicator")]
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField] private float indicatorHeight = 0.5f;
    
    public int ammo = -1;

    private InteractIndicator indicator;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private MaterialPropertyBlock mpb;

    private static readonly int OutlineEnabledID   = Shader.PropertyToID("_OutlineEnabled");
    private static readonly int OutlineThicknessID = Shader.PropertyToID("_OutlineThickness");

    // -- AWAKE --
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        mpb = new MaterialPropertyBlock();
        spriteRenderer.sortingOrder = -1000;
        SetOutline(false);

        if (indicatorPrefab != null)
        {
            GameObject instance = Instantiate(indicatorPrefab, transform);
            instance.transform.localPosition = new Vector3(0f, indicatorHeight, 0f);
            indicator = instance.GetComponent<InteractIndicator>();
            indicator.SetBasePosition(new Vector3(0f, indicatorHeight, 0f));
            indicator.Hide();
        }
    }

    // -- TRIGGER ENTER --
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerWeaponSlot handler = other.GetComponentInParent<PlayerWeaponSlot>();
        if (handler == null) return;

        handler.SetNearbyPickup(this);
        SetOutline(true);
    }

    // -- TRIGGER EXIT --
    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerWeaponSlot handler = other.GetComponentInParent<PlayerWeaponSlot>();
        if (handler == null) return;

        handler.ClearNearbyPickup(this);
        SetOutline(false);
    }

    // -- ON ENABLE --
    private void OnEnable()
    {
        if (col == null) return;
        col.enabled = false;
        Invoke(nameof(EnableCollider), 0.2f);
    }

    // -- ENABLE COLLIDER --
    private void EnableCollider()
    {
        col.enabled = true;
    }

    // -- SET OUTLINE --
    public void SetOutline(bool enabled)
    {
        if (spriteRenderer == null) return;

        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(OutlineEnabledID,   enabled ? 1f : 0f);
        mpb.SetFloat(OutlineThicknessID, 1f);
        spriteRenderer.SetPropertyBlock(mpb);

        if (enabled) indicator?.Show();
        else indicator?.Hide();
    }

    // -- DESTROY --
    public void Destroy()
    {
        Destroy(gameObject);
    }
}