// Makes any enemy an elite varient.

using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EliteModifier : MonoBehaviour
{
    [Header("Stat Multipliers")]
    [SerializeField] private float healthMultiplier = 2f;
    [SerializeField] private float damageMultiplier = 1.5f;
    [SerializeField] private float speedMultiplier = 1.15f;

    [Header("Weapon Burst Override (optional)")]
    [SerializeField] private int burstCountOverride = 0;
    [SerializeField] private float burstIntervalOverride = 0f;

    [Header("Outline")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private Color outlineColor = new Color(0.4f, 1f, 0.4f);
    [SerializeField] private float outlineThickness = 1f;

    [SerializeField] private SpriteRenderer[] targetRenderers;
    [SerializeField] private SpriteRenderer[] excludeRenderers;

    [Header("Optional Glow / Trail")]
    [SerializeField] private Light2D glow;
    [SerializeField] private ParticleSystem eliteTrail;

    private static readonly int OutlineEnabledID = Shader.PropertyToID("_OutlineEnabled");
    private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineThicknessID = Shader.PropertyToID("_OutlineThickness");

    private MaterialPropertyBlock mpb;

        // -- AWAKE --
    private void Awake()
    {
        mpb = new MaterialPropertyBlock();

        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (excludeRenderers != null && excludeRenderers.Length > 0)
        {
            targetRenderers = System.Array.FindAll(
                targetRenderers,
                sr => System.Array.IndexOf(excludeRenderers, sr) < 0
            );
        }
    }

    // -- START --
    private void Start()
    {
        ApplyOutline();

        if (glow != null) glow.enabled = true;
        if (eliteTrail != null) eliteTrail.Play();

        EnemyHealthBar healthBar = GetComponentInChildren<EnemyHealthBar>(true);
        healthBar?.SetElite(true, outlineColor);
    }

    // -- APPLY STAT MODIFIERS --
    public EnemyData ApplyStatModifiers(EnemyData original)
    {
        if (original == null) return null;

        EnemyData modified = Instantiate(original);
        modified.maxHealth *= healthMultiplier;
        modified.contactDamage *= damageMultiplier;
        modified.moveSpeed *= speedMultiplier;
        return modified;
    }

    // -- APPLY WEAPON MODIFIERS --
    public WeaponData ApplyWeaponModifiers(WeaponData original)
    {
        if (original == null) return null;

        WeaponData modified = Instantiate(original);
        modified.minDamage *= damageMultiplier;
        modified.maxDamage *= damageMultiplier;

        if (burstCountOverride > 0)
            modified.burstCount = burstCountOverride;

        if (burstIntervalOverride > 0f)
            modified.burstInterval = burstIntervalOverride;

        return modified;
    }

    // -- OUTLINE --
    private void ApplyOutline()
    {
        if (outlineMaterial == null)
        {
            return;
        }

        foreach (var sr in targetRenderers)
        {
            if (sr == null) continue;

            sr.material = outlineMaterial;

            sr.GetPropertyBlock(mpb);
            mpb.SetFloat(OutlineEnabledID, 1f);
            mpb.SetFloat(OutlineThicknessID, outlineThickness);
            mpb.SetColor(OutlineColorID, outlineColor);
            sr.SetPropertyBlock(mpb);
        }
    }
}