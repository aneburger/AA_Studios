// Makes an enemy detonate instead of dying instantly.

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyHealth))]
public class ExplodingModifier : MonoBehaviour
{
    [Header("Priming")]
    [SerializeField] private float primingDuration = 1.5f;
    [SerializeField] private float flickerStartInterval = 0.4f;
    [SerializeField] private float flickerEndInterval = 0.05f;

    [Header("Priming Tick Sound")]
    [SerializeField] private AudioClip tickClip;
    [Range(0f, 1f)] [SerializeField] private float tickStartVolume = 0.2f;
    [Range(0f, 1f)] [SerializeField] private float tickMaxVolume = 1f;
    [SerializeField] private float tickVolumeIncrement = 0.08f;

    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float explosionDamage = 20f;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private GameObject explosionVFXPrefab;
    [SerializeField] private AudioClip explosionClip;
    [Range(0f, 1f)] [SerializeField] private float explosionVolume = 1f;

    [Header("Outline")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private Color outlineColor = new Color(1f, 0.6f, 0f);
    [SerializeField] private float outlineThickness = 1f;
    [SerializeField] private SpriteRenderer[] targetRenderers;
    [SerializeField] private SpriteRenderer[] excludeRenderers;

    private static readonly int OutlineEnabledID = Shader.PropertyToID("_OutlineEnabled");
    private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineThicknessID = Shader.PropertyToID("_OutlineThickness");

    private MaterialPropertyBlock mpb;
    private TopDown.Movement.EnemyMover mover;
    private Behaviour aiBehaviour;
    private Transform player;
    private int flickerCount;

    public bool IsPriming { get; private set; }
    public bool HasDetonated { get; private set; }

    // -- AWAKE --
    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        mover = GetComponent<TopDown.Movement.EnemyMover>();
        aiBehaviour = GetComponent<IEnemyAI>() as Behaviour;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<SpriteRenderer>();

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
        ApplyOutlineMaterial();
        SetOutlineEnabled(true);
    }

    // -- BEGIN PRIMING --
    public void BeginPriming(System.Action onComplete)
    {
        if (IsPriming || HasDetonated) return;
        StartCoroutine(PrimingSequence(onComplete));
    }

    // -- PRIMING SEQUENCE --
    private IEnumerator PrimingSequence(System.Action onComplete)
    {
        IsPriming = true;
        flickerCount = 0;

        if (mover != null)
        {
            mover.DirectionalAnimator?.SetWalking(false);

            if (player != null)
            {
                Vector2 facing = ((Vector2)player.position - (Vector2)transform.position).normalized;
                mover.DirectionalAnimator?.SetDirection(facing);
            }

            mover.StopMovement();
            mover.enabled = false;
        }
        if (aiBehaviour != null) aiBehaviour.enabled = false;

        float elapsed = 0f;
        bool outlineOn = true;

        while (elapsed < primingDuration)
        {
            float t = elapsed / primingDuration;
            float interval = Mathf.Lerp(flickerStartInterval, flickerEndInterval, t);

            outlineOn = !outlineOn;
            SetOutlineEnabled(outlineOn);

            if (outlineOn)
                PlayTick();

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        SetOutlineEnabled(true);
        Detonate();
        onComplete?.Invoke();
    }

    // -- PLAY TICK --
    private void PlayTick()
    {
        float volume = Mathf.Min(tickStartVolume + tickVolumeIncrement * flickerCount, tickMaxVolume);
        AudioManager.Instance?.PlaySFXWithPitch(tickClip, volume, 0.05f);
        flickerCount++;
    }

    // -- DETONATE --
    private void Detonate()
    {
        HasDetonated = true;
        IsPriming = false;

        if (explosionVFXPrefab != null)
            Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);

        AudioManager.Instance?.PlaySFXWithPitch(explosionClip, explosionVolume);

        // Damage the player
        Collider2D[] playerHits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, playerMask);
        foreach (Collider2D hit in playerHits)
        {
            PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
            playerHealth?.TakeDamage(explosionDamage);
        }

        // Damage nearby enemies
        Collider2D[] enemyHits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, enemyMask);
        foreach (Collider2D hit in enemyHits)
        {
            if (hit.gameObject == gameObject) continue;

            EnemyHealth otherHealth = hit.GetComponentInParent<EnemyHealth>();
            if (otherHealth == null || otherHealth.gameObject == gameObject) continue;

            otherHealth.TakeDamage(explosionDamage);
        }
    }

    // -- OUTLINE TOGGLE --
    private void SetOutlineEnabled(bool enabled)
    {
        foreach (var sr in targetRenderers)
        {
            if (sr == null) continue;
            sr.GetPropertyBlock(mpb);
            mpb.SetFloat(OutlineEnabledID, enabled ? 1f : 0f);
            mpb.SetColor(OutlineColorID, outlineColor);
            mpb.SetFloat(OutlineThicknessID, outlineThickness);
            sr.SetPropertyBlock(mpb);
        }
    }

    // -- APPLY OUTLINE MATERIAL--
    private void ApplyOutlineMaterial()
    {
        if (outlineMaterial == null) return;

        foreach (var sr in targetRenderers)
        {
            if (sr == null) continue;
            sr.material = outlineMaterial;
        }
    }

    // -- GIZMO --
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }
}