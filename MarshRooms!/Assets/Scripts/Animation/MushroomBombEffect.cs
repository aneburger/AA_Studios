using UnityEngine;

public class MushroomBombEffect : MonoBehaviour
{
    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionDamage = 10f;
    [SerializeField] private GameObject explosionVFXPrefab;
    [SerializeField] private LayerMask enemyMask;


    [Header("Audio")]
    [SerializeField] private AudioClip explodeClip;
    [Range(0f, 1f)] [SerializeField] private float explodeVolume;

    private void Start()
    {
        SporeManager.Instance.OnMutatedActivated += OnActivated;
    }

    private void OnDisable()
    {
        if (SporeManager.Instance != null)
            SporeManager.Instance.OnMutatedActivated -= OnActivated;
    }

    private void OnActivated()
    {
        if (!BoonManager.Instance.Stats.hasMushroomBomb) return;

        if (explosionVFXPrefab != null)
            Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);

        AudioManager.Instance?.PlaySFXWithPitch(explodeClip, explodeVolume, 0.15f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, enemyMask);
        foreach (var hit in hits)
        {
            BaseHealth health = hit.GetComponentInParent<BaseHealth>();
            if (health != null)
                health.TakeDamage(explosionDamage);
        }
    }
}