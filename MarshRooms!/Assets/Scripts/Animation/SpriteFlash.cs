// Hit flash for enemies and bosses.

using UnityEngine;
using System.Collections.Generic;
 
[DefaultExecutionOrder(500)]
public class SpriteFlash : MonoBehaviour
{
    [Header("Flash")]
    [SerializeField] private Material flashMaterial;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float flashStrength = 0.85f;
    [SerializeField, Range(0f, 0.9f)] private float holdFraction = 0.3f;
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField, Range(0f, 1f)] private float retriggerAfter = 0.4f;
 
    [Header("Sorting")]
    [SerializeField] private int sortingOrderOffset = 6;
 
    [Header("Renderers")]
    [SerializeField] private SpriteRenderer[] targetRenderers;
    [SerializeField] private SpriteRenderer[] excludeRenderers;
 
    [Header("Enemies Only")]
    [SerializeField] private bool disableHitAnimation = false;
 
    private BaseHealth health;
    private readonly List<SpriteRenderer> sources = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> overlays = new List<SpriteRenderer>();
 
    private float flashTimer = -1f;
 
    private static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");
    private MaterialPropertyBlock block;
 
    // -- AWAKE --
    private void Awake()
    {
        health = GetComponentInParent<BaseHealth>();
        block = new MaterialPropertyBlock();
 
        if (disableHitAnimation && health != null)
            health.SetSuppressHitAnimation(true);
    }
 
    // -- START --
    private void Start()
    {
        BuildOverlays();
    }
 
    // -- ON ENABLE --
    private void OnEnable()
    {
        if (health != null && health.onTakeDamage != null)
            health.onTakeDamage.AddListener(Flash);
    }
 
    // -- ON DISABLE --
    private void OnDisable()
    {
        if (health != null && health.onTakeDamage != null)
            health.onTakeDamage.RemoveListener(Flash);
 
        EndFlash();
    }
 
    // -- BUILD OVERLAYS --
    private void BuildOverlays()
    {
        SpriteRenderer[] found = targetRenderers != null && targetRenderers.Length > 0
            ? targetRenderers
            : System.Array.FindAll(GetComponentsInChildren<SpriteRenderer>(true), IsAutoTarget);
 
        foreach (SpriteRenderer source in found)
        {
            if (source == null) continue;
            if (excludeRenderers != null && System.Array.IndexOf(excludeRenderers, source) >= 0) continue;
 
            GameObject go = new GameObject("FlashOverlay");
            go.layer = source.gameObject.layer;
            go.transform.SetParent(source.transform, false);
 
            SpriteRenderer overlay = go.AddComponent<SpriteRenderer>();
            overlay.sharedMaterial = flashMaterial;
            overlay.enabled = false;
 
            sources.Add(source);
            overlays.Add(overlay);
        }
    }
 
    // -- IS AUTO TARGET --
    private bool IsAutoTarget(SpriteRenderer sr)
    {
        if (sr.GetComponent<LightReceiver>() != null) return false;
        if (sr.GetComponent<EnemyLightReceiver>() != null) return false;
        return true;
    }
 
    // -- FLASH --
    public void Flash()
    {
        if (overlays.Count == 0) return;
 
        if (flashTimer >= 0f && flashTimer < flashDuration * retriggerAfter) return;
 
        flashTimer = 0f;
    }
 
    // -- LATE UPDATE --
    private void LateUpdate()
    {
        if (flashTimer < 0f) return;

        flashTimer += Time.deltaTime;
        float t = flashTimer / flashDuration;
 
        if (t >= 1f)
        {
            EndFlash();
            return;
        }
 
        // hold at full strength for a moment, then fade out
        float fade = t <= holdFraction ? 1f : 1f - (t - holdFraction) / (1f - holdFraction);
        float amount = flashStrength * fade;
 
        for (int i = 0; i < overlays.Count; i++)
        {
            SpriteRenderer source = sources[i];
            SpriteRenderer overlay = overlays[i];
            if (source == null || overlay == null) continue;
 
            overlay.enabled = source.enabled;
            overlay.sprite = source.sprite;
            overlay.flipX = source.flipX;
            overlay.flipY = source.flipY;
            overlay.sortingLayerID = source.sortingLayerID;
            overlay.sortingOrder = source.sortingOrder + sortingOrderOffset;
            overlay.GetPropertyBlock(block);
            block.SetColor(FlashColorID, flashColor);
            block.SetFloat(FlashAmountID, amount * source.color.a);
            overlay.SetPropertyBlock(block);
        }
    }
 
    // -- END FLASH --
    private void EndFlash()
    {
        flashTimer = -1f;
 
        foreach (SpriteRenderer overlay in overlays)
        {
            if (overlay != null) overlay.enabled = false;
        }
    }
}
 