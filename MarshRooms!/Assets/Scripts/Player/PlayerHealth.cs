// Player health extends BaseHealth

using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

using TopDown.Movement;

public class PlayerHealth : BaseHealth
{   
    [Header("Damage Settings")]
    [SerializeField] private float damageCooldown = 0.5f;
    [SerializeField] private float flickerInterval = 0.08f;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private float hurtVolume = 1f;

    private float damageCooldownTimer;
    private bool isInvincible = false;

    public static event System.Action OnPlayerDeath;

    // References
    private PlayerMover mover;

    // -- Update -- 
    private void Update()
    {
        if (damageCooldownTimer > 0f)
            damageCooldownTimer -= Time.deltaTime;
    }

    // -- AWAKE -- 
    protected override void Awake()
    {
        base.Awake();
        mover = GetComponent<PlayerMover>();
    }

    // -- SET INVINCIBILITY -- 
    public void SetInvincible(bool value)
    {
        isInvincible = value;

        // Delay speed when invincible
        if (mover != null)
        {
            mover.SetSpeed(value ? mover.OriginalSpeed * 0.7f : mover.OriginalSpeed);
            mover.DirectionalAnimator.SetAnimationSpeed(value ? 0.7f : 1f);
        }

        gameObject.layer = LayerMask.NameToLayer(value ? "PlayerInvincible" : "Player");
    }

    // -- TAKE DAMAGE -- 
    public override void TakeDamage(float amount)
    {   
        // Check if invinsible or on cooldown
        if (isInvincible) return;
        if (damageCooldownTimer > 0f) return;

        damageCooldownTimer = damageCooldown;
        base.TakeDamage(amount);
    }

    // -- DIE -- 
    protected override void Die()
    {
        base.Die();
        OnPlayerDeath?.Invoke();
        gameObject.SetActive(false);

        SceneManager.LoadScene("Floor_01");
    }

    // -- IS ON COOLDOWN -- 
    public bool IsOnCooldown()
    {
        return damageCooldownTimer > 0f || isInvincible;
    }

    // -- HIT EFFECT --
    protected override void OnHitEffect()
    {
        if (IsDead()) return;

        AudioManager.Instance.PlaySFX(hurtClip, hurtVolume);
        StartCoroutine(Flicker());
    }

    // -- FLICKER --
    private IEnumerator Flicker()
    {
        // Wait for hurt animation
        yield return new WaitForSeconds(0.2f);

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        float elapsed = 0f;

        while (elapsed < damageCooldown)
        {
            if (IsDead()) yield break;

            // Toggle between transparent and opaque
            float alpha = Mathf.PingPong(elapsed, flickerInterval) < flickerInterval / 2f ? 0f : 1f;

            foreach (var sr in renderers)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }

            yield return new WaitForSeconds(flickerInterval);
            elapsed += flickerInterval;
        }

        // Restore full opacity
        foreach (var sr in renderers)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
    }
}