// Bubble bullet - drifts and slows over time with a sideways wobble, random size chosen at spawn,

using UnityEngine;
using TopDown.Movement;

public class BubbleBullet : BaseBullet
{
    private enum BubbleSize { Small, Medium, Large }

    [Header("Size Sprites")]
    [SerializeField] private Sprite[] smallSprites;
    [SerializeField] private Sprite[] mediumSprites;
    [SerializeField] private Sprite[] largeSprites;

    [Header("Size Pop VFX")]
    [SerializeField] private GameObject smallPopVFX;
    [SerializeField] private GameObject mediumPopVFX;
    [SerializeField] private GameObject largePopVFX;

    [Header("Lifetime")]
    [SerializeField] private float minLifetime = 1.5f;
    [SerializeField] private float maxLifetime = 3f;

    [Header("Pop Audio")]
    [SerializeField] private AudioClip[] popClips;
    [Range(0f, 1f)] [SerializeField] private float popVolume = 0.8f;

    [Header("Drift")]
    [SerializeField] private float dragDecay = 2f;
    [SerializeField] private float wobbleAmplitude = 0.5f;
    [SerializeField] private float wobbleFrequency = 2f;

    [SerializeField] private float mutatedDragDecay = 1f;

    private SpriteRenderer sr;
    private GameObject popVFX;

    private float currentSpeed;
    private float traveledDistance;
    private Vector2 spawnPosition;
    private Vector2 perpendicular;
    private float wobblePhase;
    private float lifetime;
    private float elapsed;
    private bool hasPopped;

    protected override void Start()
    {
        base.Start();

        sr = GetComponent<SpriteRenderer>();
        PickRandomSize();

        currentSpeed = speed;
        traveledDistance = 0f;
        spawnPosition = transform.position;
        perpendicular = new Vector2(-direction.y, direction.x);
        wobblePhase = Random.Range(0f, Mathf.PI * 2f);

        lifetime = Random.Range(minLifetime, maxLifetime);
        elapsed = 0f;
    }

    // -- PICK RANDOM SIZE --
    private void PickRandomSize()
    {
        bool mutated = SporeManager.Instance != null && SporeManager.Instance.IsMutated;
        int roll = mutated ? Random.Range(1, 3) : Random.Range(0, 3);
        Sprite[] pool;

        switch (roll)
        {
            case 0:
                pool = smallSprites;
                popVFX = smallPopVFX;
                break;
            case 1:
                pool = mediumSprites;
                popVFX = mediumPopVFX;
                break;
            default:
                pool = largeSprites;
                popVFX = largePopVFX;
                break;
        }

        if (sr != null && pool != null && pool.Length > 0)
            sr.sprite = pool[Random.Range(0, pool.Length)];
    }

    // -- UPDATE --
    protected override void Update()
    {
        elapsed += Time.deltaTime;

        bool mutated = SporeManager.Instance != null && SporeManager.Instance.IsMutated;
        float currentDragDecay = mutated ? mutatedDragDecay : dragDecay;

        currentSpeed = Mathf.Lerp(currentSpeed, 0f, 1f - Mathf.Exp(-currentDragDecay * Time.deltaTime));
        traveledDistance += currentSpeed * Time.deltaTime;

        // Sideways wobble around the forward path
        float wobbleOffset = Mathf.Sin(elapsed * wobbleFrequency + wobblePhase) * wobbleAmplitude;

        transform.position = spawnPosition + direction * traveledDistance + perpendicular * wobbleOffset;

        if (elapsed >= lifetime && !hasPopped)
            Pop();
    }

    // -- BUBBLE HIT --
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasPopped) return;

        BaseHealth health = collision.GetComponentInParent<BaseHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);

            BaseMover mover = collision.GetComponentInParent<BaseMover>();
            if (mover != null)
                mover.ApplyKnockback(direction * knockback);
        }
        else
        {
            AudioManager.Instance?.PlaySFXWithPitch(wallHitClip, wallHitVolume, 0.1f);
        }

        Pop();
    }

        // -- POP --
    private void Pop()
    {
        if (hasPopped) return;
        hasPopped = true;

        GameObject vfxToPlay = popVFX != null ? popVFX : hitVFX;
        VFXManager.Instance.SpawnHitVFX(vfxToPlay, transform.position, weaponSortingOrder);

        PlayRandomPopSound();

        Destroy(gameObject);
    }

    // -- PLAY RANDOM POP SOUND --
    private void PlayRandomPopSound()
    {
        if (popClips == null || popClips.Length == 0) return;

        AudioClip clip = popClips[Random.Range(0, popClips.Length)];
        AudioManager.Instance?.PlaySFXWithPitch(clip, popVolume, 0.1f);
    }
}