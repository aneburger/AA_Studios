// Player health extends BaseHealth

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private AudioClip dieClip;
    [Range(0f, 1f)] public float dieVolume;
    [SerializeField] private AudioClip healClip;
    [Range(0f, 1f)] public float healVolume;

    [Header("Low Health Audio")]
    [SerializeField] private AudioClip lowHealthClip;
    [Range(0f, 1f)] public float lowHealthVolume = 0.5f;
    [SerializeField] private float lowHealthPitch = 1f;

    [Header("Respawn Settings")]
    [SerializeField] private float respawnInvincibilityDuration = 1.5f;

    private bool isLowHealthActive = false;

    private float damageCooldownTimer;
    private bool isInvincible = false;

    private float bonusIFrameDuration = 0f;
    private float dodgeDamageChance = 0f;

    private string lastAttackerName = "Unknown";

    public static event System.Action OnPlayerDeath;

    private Coroutine flickerCoroutine;
    private SpriteRenderer playerRenderer;
    private PlayerMutatedVisuals mutatedVisuals;
    private PlayerSporeReadyVisuals sporeReadyVisuals;

    public int MaxHealth => (int)maxHealth;
    public int CurrentHealth => (int)currentHealth;

    // References
    private PlayerShooter shooter;
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
        mutatedVisuals = GetComponent<PlayerMutatedVisuals>();
        sporeReadyVisuals = GetComponent<PlayerSporeReadyVisuals>();

        // Update HUD on start

        shooter = GetComponent<PlayerShooter>();
        playerRenderer = transform.Find("Visuals").GetComponent<SpriteRenderer>();
    }
    
    // -- START -- 
    private void Start()
    {
        UpdateHUD();
    }

    // -- SET BONUS I-FRAME DURATION --
    public void SetBonusIFrameDuration(float bonus)
    {
        bonusIFrameDuration = bonus;
    }

    // -- SET DODGE DAMAGE CHANCE --
    public void SetDodgeDamageChance(float chance)
    {
        dodgeDamageChance = chance;
    }

    // -- SET LAST ATTACKER --
    public void SetLastAttacker(string attackerName)
    {
        if (!string.IsNullOrEmpty(attackerName))
            lastAttackerName = attackerName;
    }

    // -- INCREASE MAX HEALTH --
    public override void IncreaseMaxHealth(float amount, bool healToFull = false)
    {
        base.IncreaseMaxHealth(amount, healToFull);
        HUDManager.Instance?.RefreshHearts();
    }

    // -- SET INVINCIBILITY -- 
    public void SetInvincible(bool value, bool affectSpeed = true)
    {
        isInvincible = value;

        if (affectSpeed && mover != null)
        {
            mover.SetSpeed(value ? mover.OriginalSpeed * 0.3f : mover.OriginalSpeed);
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

        // Change to not take damage
        if (Random.value <= dodgeDamageChance)
            return;

        damageCooldownTimer = damageCooldown + bonusIFrameDuration;
        base.TakeDamage(amount);
        UpdateHUD();

        if (!IsDead())
            UpdateLowHealthEffect();
    }

    // -- HEAL --
    public void Heal(float amount)
    {
        if (IsDead() && TutorialDirector.Instance == null) return;

        AudioManager.Instance.PlaySFXWithPitch(healClip, healVolume, 0.1f);
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        anim.SetTrigger("Heal");
        UpdateHUD();

        UpdateLowHealthEffect();
    }

    // -- RESTORE FROM SAVE --
    public void RestoreHealth(int savedMaxHealth, int savedCurrentHealth)
    {
        maxHealth = savedMaxHealth;
        currentHealth = Mathf.Clamp(savedCurrentHealth, 0, maxHealth);

        HUDManager.Instance?.RefreshHearts();
        UpdateHUD();
        UpdateLowHealthEffect();
    }

    // -- RESET FOR RESPAWN --
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateHUD();
        UpdateLowHealthEffect();
    }

    // -- Respawn Invincibility --
    private IEnumerator RespawnInvincibility()
    {
        SetInvincible(true, affectSpeed: false);
        yield return new WaitForSeconds(respawnInvincibilityDuration);
        if (!IsDead())
            SetInvincible(false, affectSpeed: false);
    }

    // -- IS ON COOLDOWN -- 
    public bool IsOnCooldown()
    {
        return damageCooldownTimer > 0f || isInvincible;
    }

    // -- UPDATE HUD --
    public void UpdateHUD()
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
        AudioManager.Instance.PlaySFXWithPitch(hurtClip, hurtVolume, 0.1f);
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
        float totalDuration = damageCooldown + bonusIFrameDuration;

        while (elapsed < totalDuration)
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

            mutatedVisuals?.SetEffectsVisible(alpha > 0f);

            yield return new WaitForSeconds(flickerInterval);
            elapsed += flickerInterval;
        }

        // Restore full opacity
        Color restored = playerRenderer.color;
        restored.a = 1f;
        playerRenderer.color = restored;

        // Restore outline and light
        if (SporeManager.Instance.IsMutated)
            mutatedVisuals?.SetEffectsVisible(true);

        flickerCoroutine = null;
    }

    // -- LOW HEALTH EFFECT -- 
    public void UpdateLowHealthEffect()
    {
        SetLowHealthState(currentHealth < 4f);
    }

    // -- STOP LOW HEALTH EFFECT --
    public void StopLowHealthEffect()
    {
        SetLowHealthState(false);
    }

    // -- SET LOW HEALTH STATE --
    private void SetLowHealthState(bool isLow)
    {
        if (isLow == isLowHealthActive) return;
        isLowHealthActive = isLow;

        ScreenEffects.Instance?.SetLowHealth(isLow);
        AudioManager.Instance?.SetLowHealthActive(isLow, lowHealthClip, lowHealthVolume, lowHealthPitch);
    }

    // -- SET LOW HEALTH AUDIO PAUSED --
    public void SetLowHealthAudioPaused(bool paused)
    {
        AudioManager.Instance?.SetLowHealthAudioPaused(paused);
    }

    // -- REVIVE --
    public void Revive()
    {   
        Heal(maxHealth);
        anim.SetTrigger("Revive");
        shooter.HideWeapon(false);
        ScreenEffects.Instance?.SetLowHealth(false);
        StartCoroutine(RespawnInvincibility());
    }

    // -- DIE -- 
    protected override void Die()
    {   
        if (TutorialDirector.Instance != null)
        {   
            SetLowHealthState(false);

            anim.SetTrigger("Die");
            AudioManager.Instance.PlaySFX(dieClip, dieVolume);
            TutorialDirector.Instance.HandlePlayerDeath(this);
            return;
        }

        base.Die();
        OnPlayerDeath?.Invoke();

        AudioManager.Instance.StopMusic();
        AudioManager.Instance.PlaySFX(dieClip, dieVolume);

        if (SporeManager.Instance.IsMutated)
        {
            mutatedVisuals?.SetEffectsVisible(false);
            mutatedVisuals?.ResetStateSilently();
            GetComponent<PlayerMutatedStats>()?.ResetStateSilently();
        }

        SporeManager.Instance.ResetSpores();

        GetComponent<PlayerInput>().enabled = false;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        shooter.HideWeapon(true);

        anim.SetTrigger("Die");
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        SetLowHealthState(false);

        RunStatsTracker.Instance?.RegisterDeath();
        RunStatsTracker.Instance?.PauseTimer();

        yield return new WaitForSeconds(2.5f);

        bool fadeComplete = false;
        ScreenEffects.Instance?.FadeToBlack(1f, () => fadeComplete = true);
        yield return new WaitUntil(() => fadeComplete);

        yield return new WaitForSeconds(0.5f);

        DeathScreenManager.Instance?.Show(BuildDeathScreenStats());
    }

    // -- RESTORE AFTER RETRY --
    public void RestoreAfterRetry()
    {
        GetComponent<PlayerInput>().enabled = true;
        GetComponent<PlayerWeaponSlot>()?.ResetToDefaultWeapon();
        shooter.HideWeapon(false);
        mover.ForceIdleAnimation();

        LevelLoader.Instance?.SaveCurrentLevel();

        ResetHealth();

        StartCoroutine(RespawnInvincibility());
    }

    // -- BUILD DEATH SCREEN STATS --
    private DeathScreenStats BuildDeathScreenStats()
    {
        PlayerWeaponSlot weaponSlot = GetComponent<PlayerWeaponSlot>();
        Sprite[] lostWeapons = new Sprite[2];

        if (weaponSlot != null)
        {
            for (int i = 1; i <= 2; i++)
            {
                WeaponData weapon = weaponSlot.GetWeaponAtSlot(i);
                lostWeapons[i - 1] = weapon != null ? weapon.hudSprite : null;
            }
        }

        List<Sprite> cardSprites = new List<Sprite>();
        if (RunStatsTracker.Instance != null)
        {
            foreach (BoonCardData card in RunStatsTracker.Instance.CollectedCards)
            {
                if (card == null) continue;
                cardSprites.Add(card.icon);
            }
        }

        return new DeathScreenStats
        {
            deathCount = RunStatsTracker.Instance != null ? RunStatsTracker.Instance.DeathCount : 0,
            killerEnemyName = lastAttackerName,
            floorName = LevelLoader.Instance != null ? LevelLoader.Instance.GetCurrentFloorDisplayName() : "",
            timePlayedSeconds = RunStatsTracker.Instance != null ? RunStatsTracker.Instance.ElapsedPlayTime : 0f,
            killCount = RunStatsTracker.Instance != null ? RunStatsTracker.Instance.KillCount : 0,
            lostWeaponSprites = lostWeapons,
            collectedCardSprites = cardSprites.ToArray()
        };
    }

    // -- ON DISABLE --
    private void OnDisable()
    {
        AudioManager.Instance?.SetLowHealthActive(false);
        isLowHealthActive = false;
    }
}