// Player health extends BaseHealth

using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

using TopDown.Movement;

public class PlayerHealth : BaseHealth
{   
    [Header("Damage Settings")]
    [SerializeField] private float damageCooldown = 0.5f;
    [SerializeField] private float flickerInterval = 0.08f;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtClip;
    [Range(0f, 1f)] public float hurtVolume;

    private float damageCooldownTimer;
    private bool isInvincible = false;

    public static event System.Action OnPlayerDeath;

    private Coroutine flickerCoroutine;
    private SpriteRenderer playerRenderer;

    private PlayerShooter shooter;

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


        // Update HUD on start

        shooter = GetComponent<PlayerShooter>();
        playerRenderer = transform.Find("Visuals").GetComponent<SpriteRenderer>();
    }
    
    private void Start()
    {
        UpdateHUD();
    }

    // -- SET INVINCIBILITY -- 
    public void SetInvincible(bool value)
    {
        isInvincible = value;

        // Delay speed when invincible
        if (mover != null)
        {
            mover.SetSpeed(value ? mover.OriginalSpeed * 0.5f : mover.OriginalSpeed);
            mover.DirectionalAnimator.SetAnimationSpeed(value ? 0.5f : 1f);
        }

        gameObject.layer = LayerMask.NameToLayer(value ? "PlayerInvincible" : "Player");
    }

    // -- TAKE DAMAGE -- 
    public override void TakeDamage(float amount)
    {   
        if (IsDead()) return;
        if (isInvincible) return;
        if (damageCooldownTimer > 0f) return;

        damageCooldownTimer = damageCooldown;
        base.TakeDamage(amount);
        UpdateHUD();

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

    // -- UPDATE HUD --
    private void UpdateHUD()
    {
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateHealthDisplay((int)currentHealth, (int)maxHealth);
        }
    }

    // -- HIT EFFECT --
    protected override void OnHitEffect()
    {
        if (IsDead()) return;
        
        ScreenEffects.Instance.FlashDamage();
        AudioManager.Instance.PlaySFXWithPitch(hurtClip, hurtVolume);
        AudioManager.Instance.DampenAudio(1f);

        if (flickerCoroutine != null) StopCoroutine(flickerCoroutine);
        flickerCoroutine = StartCoroutine(Flicker());
    }

    // -- FLICKER --
    private IEnumerator Flicker()
    {
        // Wait for hurt animation
        yield return new WaitForSeconds(0.2f);

        float elapsed = 0f;

        while (elapsed < damageCooldown)
        {
            if (IsDead())
            {
                playerRenderer.color = Color.white;
                yield break;
            }

            float alpha = Mathf.PingPong(elapsed, flickerInterval) < flickerInterval / 2f ? 0f : 1f;
            Color c = playerRenderer.color;
            c.a = alpha;
            playerRenderer.color = c;

            yield return new WaitForSeconds(flickerInterval);
            elapsed += flickerInterval;
        }

        // Restore full opacity
        Color restored = playerRenderer.color;
        restored.a = 1f;
        playerRenderer.color = restored;
        flickerCoroutine = null;
    }

        // -- DIE -- 
        protected override void Die()
        {
            base.Die();
            OnPlayerDeath?.Invoke();

            AudioManager.Instance.StopRunningSFX();

            // Disable input and physics
            GetComponent<PlayerInput>().enabled = false;
            GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            shooter.HideWeapon(true);

            anim.SetTrigger("Die");
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            // Wait for death animation to finish
            yield return new WaitForSeconds(5f);
            
            gameObject.SetActive(false);
            SceneManager.LoadScene("Floor_01");
        }

        // -- ON DISABLE --
        private void OnDisable()
        {
            AudioManager.Instance.StopRunningSFX();
        }
}