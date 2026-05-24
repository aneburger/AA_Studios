using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Spore : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite[] variations;

    [Header("Attraction")]
    [SerializeField] private float attractRadius = 3f;
    [SerializeField] private float attractSpeed = 5f;

    [Header("Burst")]
    [SerializeField] private float burstForceX = 2f;
    [SerializeField] private float burstForceY = 4f;
    [SerializeField] private float gravity = 8f;
    [SerializeField] private float bounceDecay = 0.4f;
    [SerializeField] private float groundY;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 8f;
    [SerializeField] private float flickerStart = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip sporeCollectClip;
    [Range(0f, 1f)] public float sporeCollectVolume;

    private SpriteRenderer sr;
    private Transform player;
    private bool isAttracting = false;
    private Vector2 velocity;
    private bool hasLanded = false;
    private Light2D sporeLight;

    // -- AWAKE --
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sporeLight = GetComponentInChildren<Light2D>();
        sr.sprite = variations[Random.Range(0, variations.Length)];
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        groundY = transform.position.y;

        // Burst upward and sideways
        float randomX = Random.Range(-burstForceX, burstForceX);
        velocity = new Vector2(randomX, burstForceY);

        StartCoroutine(LifetimeTimer());
    }

    // -- UPDATE --
    private void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= attractRadius)
            isAttracting = true;

        if (isAttracting)
        {
            transform.position = Vector2.MoveTowards(transform.position, player.position, attractSpeed * Time.deltaTime);
            if (distance < 0.2f) Collect();
        }
        else if (!hasLanded)
        {
            // Apply gravity
            velocity.y -= gravity * Time.deltaTime;
            transform.position += (Vector3)velocity * Time.deltaTime;

            // Bounce when hitting ground
            if (transform.position.y <= groundY)
            {
                Vector3 pos = transform.position;
                pos.y = groundY;
                transform.position = pos;

                velocity.y = Mathf.Abs(velocity.y) * bounceDecay;
                velocity.x = Mathf.Lerp(velocity.x, 0f, 0.4f);

                // Stop bouncing
                if (velocity.y < 0.5f)
                {
                    velocity = Vector2.zero;
                    hasLanded = true;
                }
            }
        }
    }

    // -- COLLECT --
    private void Collect()
    {
        StopAllCoroutines();

        // notify SporeManager to count spore
        if (SporeManager.Instance != null)
            SporeManager.Instance.CollectSpore();

        Destroy(gameObject);
        AudioManager.Instance.PlaySFXWithPitch(sporeCollectClip, sporeCollectVolume, 0.1f);
    }

    // -- LIFETIME TIMER --
    private IEnumerator LifetimeTimer()
    {
        // Wait until flicker starts
        yield return new WaitForSeconds(flickerStart);

        // Flicker
        float elapsed = 0f;
        float flickerInterval = 0.15f;
        float flickerDuration = lifetime - flickerStart;

        while (elapsed < flickerDuration)
        {
            sr.enabled = !sr.enabled;
            sporeLight.enabled = sr.enabled;
            yield return new WaitForSeconds(flickerInterval);
            elapsed += flickerInterval;

            // Flicker faster as it gets closer to despawning
            flickerInterval = Mathf.Lerp(0.15f, 0.04f, elapsed / flickerDuration);
        }

        Destroy(gameObject);
    }
}