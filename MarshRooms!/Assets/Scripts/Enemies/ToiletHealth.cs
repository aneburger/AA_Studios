// The tutorial toilet dummy.
// Extends BaseHealth

using UnityEngine;
using System.Collections;

public class ToiletHealth : BaseHealth
{
    [Header("Toilet Settings")]
    [SerializeField] private EnemyHealthBar healthBar;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Hit Feedback")]
    [SerializeField] private float shakeIntensity = 0.08f;
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private AudioClip hurtClip;
    [Range(0f, 1f)] public float hurtVolume = 0.8f;

    [Header("Death")]
    [SerializeField] private AudioClip fixedClip;
    [Range(0f, 1f)] public float fixedVolume = 1f;
    [SerializeField] private float postDeathDelay = 1f;

    private Vector3 originalPosition;
    private bool isDying = false;
    private bool firstHit = false;

    // -- AWAKE --
    protected override void Awake()
    {
        base.Awake();
        originalPosition = transform.localPosition;
    }

    // -- START --
    private void Start()
    {
        healthBar?.UpdateHealth(currentHealth, maxHealth);
    }

    // -- TAKE DAMAGE --
    public override void TakeDamage(float amount)
    {
        if (isDying) return;

        base.TakeDamage(amount);
        healthBar?.UpdateHealth(currentHealth, maxHealth);

        // Damage numbers
        Vector2 topPos = GetTopPosition();
        VFXManager.Instance?.SpawnDamageNumber(amount * 10, topPos);

        if (!firstHit)
        {
            firstHit = true;
            TutorialDirector.Instance?.OnToiletShot();
        }
    }

    // -- HIT EFFECT --
    protected override void OnHitEffect()
    {
        if (IsDead()) return;
        AudioManager.Instance?.PlaySFXWithPitch(hurtClip, hurtVolume, 0.15f);
        anim?.SetTrigger("TakeDamage");
        StartCoroutine(ShakeRoutine());
    }

    // -- HIT ANITMATION --
    public void PlayHitAnimation()
    {
        if (IsDead()) return;
        anim?.SetTrigger("TakeDamage");
    }

    // -- DIE --
    protected override void Die()
    {
        if (isDying) return;
        isDying = true;

        base.Die();

        StartCoroutine(DeathRoutine());
    }

    // -- DEATH ROUTINE --
    private IEnumerator DeathRoutine()
    {
        VFXManager.Instance?.SpawnEnemyExplosion(GetTopPosition());
        
        anim?.SetBool("IsFixed", true);
        
        healthBar?.gameObject.SetActive(false);
        AudioManager.Instance?.PlaySFX(fixedClip, fixedVolume);

        yield return new WaitForSeconds(postDeathDelay);
        TutorialDirector.Instance?.OnToiletDead();
    }

    // -- SHAKE ROUTINE --
    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-shakeIntensity, shakeIntensity);
            float y = Random.Range(-shakeIntensity, shakeIntensity);
            transform.localPosition = originalPosition + new Vector3(x, y, 0f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        transform.localPosition = originalPosition;
    }

    // -- TOP POSITION --
    private Vector2 GetTopPosition()
    {
        if (spriteRenderer != null)
            return new Vector2(transform.position.x, spriteRenderer.bounds.max.y);
        return transform.position;
    }
}