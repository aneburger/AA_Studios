using UnityEngine;
using TopDown.Movement;

public class BananaBullet : BaseBullet
{
    [Header("Banana Settings")]
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float splashRadius;
    [SerializeField] private float splashDamageMultiplier;
    [SerializeField] private float maxRange;

    [Header("Splat VFX")]
    [SerializeField] private Sprite[] splatSprites;
    [SerializeField] private int minSplats;
    [SerializeField] private int maxSplats;
    [SerializeField] private float splatRadius;
    [SerializeField] private float splatLifetime;

    [Header("Audio")]
    [SerializeField] private AudioClip splashClip;
    [Range(0f, 1f)] public float splashVolume;

    [Header("Drop Zone")]
    private RoomDropZone dropZone;

    [Header("Mutated Settings")]
    [SerializeField] private float mutatedSplashRadiusMultiplier;
    [SerializeField] private int mutatedSplatMultiplier;

    private float rotationDirection;
    private Vector2 spawnPosition;

    protected override void Start()
    {
        base.Start();
        dropZone = FindObjectOfType<RoomDropZone>();

        // Random spin direction and speed
        rotationDirection = Random.value > 0.5f ? 1f : -1f;
        spawnPosition = transform.position;
    }

    protected override void Update()
    {
        base.Update();

        // Spin the bullet
        transform.Rotate(0f, 0f, rotationDirection * rotationSpeed * Time.deltaTime);

        // Splash when max range is reached
        if (Vector2.Distance(transform.position, spawnPosition) >= maxRange)
            Splash();
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        BaseHealth health = collision.GetComponentInParent<BaseHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);

            BaseMover mover = collision.GetComponentInParent<BaseMover>();
            if (mover != null)
                mover.ApplyKnockback(direction * knockback);
        }

        Splash();
    }

    private void Splash()
    {
        AudioManager.Instance.PlaySFXWithPitch(splashClip, splashVolume, 0.1f);

        bool mutated = SporeManager.Instance != null && SporeManager.Instance.IsMutated;

        float currentSplashRadius = mutated ? splashRadius * mutatedSplashRadiusMultiplier : splashRadius;
        int currentMinSplats = mutated ? minSplats * mutatedSplatMultiplier : minSplats;
        int currentMaxSplats = mutated ? maxSplats * mutatedSplatMultiplier : maxSplats;

        Vector2 splashCenter = dropZone != null 
            ? dropZone.GetSafeDropPosition(transform.position) 
            : (Vector2)transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(splashCenter, currentSplashRadius);
        foreach (var hit in hits)
        {
            EnemyHealth health = hit.GetComponentInParent<EnemyHealth>();
            if (health != null)
            {
                health.TakeDamage(damage * splashDamageMultiplier);
            }
        }

        int splatCount = Random.Range(currentMinSplats, currentMaxSplats + 1);
        for (int i = 0; i < splatCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * splatRadius;
            Vector2 rawPos = splashCenter + offset;

            Vector2 splatPos = dropZone != null 
                ? dropZone.GetSafeDropPosition(rawPos) 
                : rawPos;

            GameObject splat = new GameObject("Splat");
            splat.transform.position = splatPos;
            splat.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            SpriteRenderer sr = splat.AddComponent<SpriteRenderer>();
            sr.sprite = splatSprites[Random.Range(0, splatSprites.Length)];
            sr.sortingLayerName = "Background";
            sr.sortingOrder = 2;

            float scale = Random.Range(0.8f, 1.4f);
            splat.transform.localScale = Vector3.one * scale;

            FadeOutSprite fade = splat.AddComponent<FadeOutSprite>();
            fade.Setup(splatLifetime, 0.5f);
        }

        VFXManager.Instance.SpawnHitVFX(hitVFX, transform.position, weaponSortingOrder);
        Destroy(gameObject);
    }
}