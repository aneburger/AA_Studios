using System.Collections;
using UnityEngine;

public class DestructibleProp : MonoBehaviour, IBreakable
{
    public enum AfterBreak { ShowBrokenSprite, Disappear }

    [Header("Visuals")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] AfterBreak afterBreak = AfterBreak.ShowBrokenSprite;
    [SerializeField] Sprite brokenSprite;

    [Header("Material")]
    [SerializeField] BreakMaterial breakMaterial;

    [Header("Debris")]
    [SerializeField] Sprite[] debrisSprites;
    [SerializeField] float debrisSpawnRadius = 0.2f;
    [SerializeField] string debrisSortingLayer = "Background";
    [SerializeField] int debrisSortingOrder = 3;

    [Header("Colliders")]
    [SerializeField] bool removeColliderOnBreak = false;
    [SerializeField] Collider2D brokenCollider;

    [Header("Stacking")]
    [SerializeField] float childBreakDelay = 0.05f;

    [Header("Optional pickup drop")]
    [Range(0f, 1f)] [SerializeField] float dropChance = 0f;
    [SerializeField] GameObject[] possiblePickups;

    public bool IsBroken { get; private set; }

    bool queued;
    bool vanishRequested;
    bool hidden;

    // Room drop zone, keeps debris and pickups inside the walls
    static RoomDropZone cachedDropZone;

    const float DefaultMinSpeed = 1.5f, DefaultMaxSpeed = 4f;
    const float DefaultMinSettle = 0.25f, DefaultMaxSettle = 0.5f, DefaultSpin = 540f;

    void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Broken collider stays off until the prop breaks
        if (brokenCollider != null)
            brokenCollider.enabled = false;
    }

    // -- SAFE POSITION --
    static Vector2 SafePosition(Vector2 pos)
    {
        if (cachedDropZone == null)
            cachedDropZone = FindFirstObjectByType<RoomDropZone>();

        return cachedDropZone != null ? cachedDropZone.GetSafeDropPosition(pos) : pos;
    }

    // -- BREAK -- 
    public void Break()
    {
        if (IsBroken) return;
        DoBreak();
    }

    // -- BREAK DELAYED --
    public void BreakDelayed(float delay)
    {
        if (IsBroken || queued) return;
        if (delay <= 0f) { Break(); return; }
        queued = true;
        StartCoroutine(BreakAfter(delay));
    }

    // -- BREAK AS CHILD --
    public void BreakAsChild(float delay)
    {
        vanishRequested = true;

        if (IsBroken)
        {
            if (!hidden) StartCoroutine(HideAfter(delay));
            return;
        }

        BreakDelayed(delay);
    }

    // -- BREAK AFTER --
    IEnumerator BreakAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        Break();
    }

    // -- HIDE AFTER --
    IEnumerator HideAfter(float delay)
    {
        hidden = true;
        if (delay > 0f) yield return new WaitForSeconds(delay);
        HideNow();
    }

    // -- HIDE NOW --
    void HideNow()
    {   
        hidden = true;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        DisableColliders();
    }  

    // -- DISABLE COLLIDERS -- 
    void DisableColliders()
    {
        foreach (var c in GetComponents<Collider2D>())
            c.enabled = false;
    }

    // -- SWAP TO BROKEN COLLIDER --
    void SwapToBrokenCollider()
    {
        foreach (var c in GetComponents<Collider2D>())
            c.enabled = (c == brokenCollider);
    }

    // -- DO BREAK -- 
    void DoBreak()
    {
        IsBroken = true;

        bool vanish = vanishRequested || afterBreak == AfterBreak.Disappear;

        if (vanish)
        {
            HideNow();
        }
        else
        {
            if (spriteRenderer != null && brokenSprite != null)
                spriteRenderer.sprite = brokenSprite;

            if (brokenCollider != null)
                SwapToBrokenCollider();
            else if (removeColliderOnBreak)
                DisableColliders();
        }

        SpawnDebris();
        PlaySound();
        SpawnDust();
        TryDropPickup();

        RunStatsTracker.Instance.RegisterDestroyed();

        foreach (var child in GetComponentsInChildren<DestructibleProp>(true))
        {
            if (child != this) child.BreakAsChild(childBreakDelay);
        }
    }

    // -- SPAWN DEBRIS --
    void SpawnDebris()
    {
        if (debrisSprites == null || debrisSprites.Length == 0) return;

        float minSpeed = breakMaterial != null ? breakMaterial.minSpeed : DefaultMinSpeed;
        float maxSpeed = breakMaterial != null ? breakMaterial.maxSpeed : DefaultMaxSpeed;
        float minSettle = breakMaterial != null ? breakMaterial.minSettleTime : DefaultMinSettle;
        float maxSettle = breakMaterial != null ? breakMaterial.maxSettleTime : DefaultMaxSettle;
        float spin = breakMaterial != null ? breakMaterial.maxSpinDegPerSec : DefaultSpin;

        foreach (var sprite in debrisSprites)
        {
            if (sprite == null) continue;

            Vector2 offset = Random.insideUnitCircle * debrisSpawnRadius;
            Vector2 start = SafePosition((Vector2)transform.position + offset);

            var go = new GameObject("Debris");
            go.transform.position = start;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.flipX = Random.value > 0.5f;
            sr.sortingLayerName = debrisSortingLayer;
            sr.sortingOrder = debrisSortingOrder;

            Vector2 dir = Random.insideUnitCircle;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;

            float settle = Random.Range(minSettle, maxSettle);
            float distance = Random.Range(minSpeed, maxSpeed) * settle / 3f;
            Vector2 end = SafePosition(start + dir.normalized * distance);

            go.AddComponent<DebrisPiece>().Launch(start, end, Random.Range(-spin, spin), settle);
        }
    }

    // -- PLAY SOUND-- 
    void PlaySound()
    {
        if (breakMaterial == null || breakMaterial.breakSounds == null || breakMaterial.breakSounds.Length == 0) return;

        var clip = breakMaterial.breakSounds[Random.Range(0, breakMaterial.breakSounds.Length)];
        AudioManager.Instance?.PlaySFXWithPitch(clip, breakMaterial.volume, breakMaterial.pitchVariation);
    }

    // -- SPAWN DUST -- 
    void SpawnDust()
    {
        if (breakMaterial == null || breakMaterial.dustPrefab == null) return;
        var dust = Instantiate(breakMaterial.dustPrefab, transform.position, Quaternion.identity);
        Destroy(dust, 3f);
    }

    // -- TRY DROP PICKUP -- 
    void TryDropPickup()
    {
        if (dropChance <= 0f || possiblePickups == null || possiblePickups.Length == 0) return;
        if (Random.value > dropChance) return;
        
        var prefab = possiblePickups[Random.Range(0, possiblePickups.Length)];
        if (prefab != null)
            Instantiate(prefab, SafePosition(transform.position), Quaternion.identity);
    }
}