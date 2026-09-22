// Boss health. Extends BaseHealth directly

using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BossHealth : BaseHealth
{
    public const string ReasonIntro = "Intro";
    public const string ReasonHidden = "Hidden";
    public const string ReasonTransition = "Transition";
    public const string ReasonCharging = "Charging";
    public const string ReasonDeath = "Death";

    [Header("Phases")]
    [Range(0.05f, 0.95f)] [SerializeField] private float phaseTwoThreshold = 0.4f;
    [SerializeField] private bool protectUntilPhaseTwo = true;
    [SerializeField] private bool phaseBars = true;

    [Header("Start State")]
    [SerializeField] private bool startInvulnerable = true;

    [Header("Hit Reaction")]
    [SerializeField] private float flinchCooldown = 0.4f;

    [Header("Spore Drops")]
    [SerializeField] private GameObject sporePrefab;
    [Range(0f, 1f)] [SerializeField] private float sporeDropChance = 0.12f;
    [SerializeField] private int minSporesPerDrop = 1;
    [SerializeField] private int maxSporesPerDrop = 1;
    [SerializeField] private float sporeScatterRadius = 1f;
    [SerializeField] private float sporeDropCooldown = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtClip;
    [Range(0f, 1f)] [SerializeField] private float hurtVolume = 1f;

    [Header("Invulnerable Feedback")]
    [SerializeField] private AudioClip deflectClip;
    [Range(0f, 1f)] [SerializeField] private float deflectVolume = 1f;
    [SerializeField] private float deflectCooldown = 0.08f;

    public event Action<float, float> OnHealthChanged;
    public event Action OnTookDamage; 
    public event Action OnPhaseTwoThreshold;
    public event Action OnDied;

    private readonly HashSet<string> invulnerableReasons = new HashSet<string>();
    private bool phaseTwoTriggered;
    private bool phaseTwoStarted;
    private bool flinchEnabled = true;
    private float nextFlinchTime;
    private float nextSporeDropTime;
    private float nextDeflectTime;

    public bool IsInvulnerable => invulnerableReasons.Count > 0;
    public bool PhaseTwoTriggered => phaseTwoTriggered;
    public bool PhaseTwoStarted => phaseTwoStarted;
    public float HealthFraction => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
    public float ThresholdHealth => maxHealth * phaseTwoThreshold;

    public float PhaseFraction
    {
        get
        {
            if (!phaseBars) return HealthFraction;
            if (maxHealth <= 0f) return 0f;

            float threshold = ThresholdHealth;

            if (phaseTwoStarted)
                return threshold > 0f ? Mathf.Clamp01(currentHealth / threshold) : 0f;

            float span = maxHealth - threshold;
            return span > 0f ? Mathf.Clamp01((currentHealth - threshold) / span) : 0f;
        }
    }

    // -- AWAKE --
    protected override void Awake()
    {
        base.Awake();
        if (startInvulnerable) invulnerableReasons.Add(ReasonIntro);
    }

    // -- SET INVULNERABLE --
    public void SetInvulnerable(string reason, bool value)
    {
        if (value) invulnerableReasons.Add(reason);
        else invulnerableReasons.Remove(reason);
    }

    // -- FLINCH --
    public void SetFlinchEnabled(bool value)
    {
        flinchEnabled = value;
    }

    // -- PHASE TWO STARTED --
    public void NotifyPhaseTwoStarted()
    {
        phaseTwoStarted = true;
    }

    // -- TAKE DAMAGE --
    public override void TakeDamage(float amount)
    {
        if (IsDead()) return;

        // The bullet is still absorbed by the boss,
        if (IsInvulnerable)
        {
            PlayDeflect();
            return;
        }

        if (protectUntilPhaseTwo && !phaseTwoStarted)
        {
            amount = Mathf.Min(amount, currentHealth - ThresholdHealth);

            if (amount <= 0f)
            {
                PlayDeflect();
                return;
            }
        }

        bool canFlinch = flinchEnabled && Time.time >= nextFlinchTime;
        SetSuppressHitAnimation(!canFlinch);
        if (canFlinch) nextFlinchTime = Time.time + flinchCooldown;

        base.TakeDamage(amount);

        VFXManager.Instance?.SpawnDamageNumber(amount * 10, GetTopPosition());

        if (IsDead()) return;

        OnTookDamage?.Invoke();
        TryDropSpores();
        CheckPhaseThreshold();
    }

    // -- HIT EFFECT --
    protected override void OnHitEffect()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (!IsDead())
            AudioManager.Instance?.PlaySFXWithPitch(hurtClip, hurtVolume, 0.2f);
    }

    // -- DEFLECT SOUND --
    private void PlayDeflect()
    {
        if (deflectClip == null || Time.time < nextDeflectTime) return;

        nextDeflectTime = Time.time + deflectCooldown;
        AudioManager.Instance?.PlaySFXWithPitch(deflectClip, deflectVolume, 0.1f);
    }

    // -- SPORE DROPS --
    private void TryDropSpores()
    {
        if (sporePrefab == null || sporeDropChance <= 0f) return;
        if (Time.time < nextSporeDropTime) return;
        if (Random.value > sporeDropChance) return;

        nextSporeDropTime = Time.time + sporeDropCooldown;

        int amount = Random.Range(minSporesPerDrop, Mathf.Max(minSporesPerDrop, maxSporesPerDrop) + 1);
        RoomManager room = RoomManager.Current;

        for (int i = 0; i < amount; i++)
        {
            Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * sporeScatterRadius;

            if (room != null)
                spawnPos = room.GetSafeDropPosition(spawnPos);

            Instantiate(sporePrefab, spawnPos, Quaternion.identity);
        }
    }

    // -- PHASE THRESHOLD --
    private void CheckPhaseThreshold()
    {
        if (phaseTwoTriggered) return;
        if (currentHealth > ThresholdHealth + 0.001f) return;

        phaseTwoTriggered = true;
        OnPhaseTwoThreshold?.Invoke();
    }

    // -- DIE --
    protected override void Die()
    {
        SetInvulnerable(ReasonDeath, true);
        base.Die();
        OnDied?.Invoke();
    }

    // -- TOP POSITION -- (for damage numbers)
    private Vector2 GetTopPosition()
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            return new Vector2(transform.position.x, sr.bounds.max.y);

        return transform.position;
    }

    // -- DEBUG --
    [ContextMenu("Debug: Reset Health")]
    private void DebugResetHealth()
    {
        Initialise(maxHealth);
        phaseTwoTriggered = false;
        phaseTwoStarted = false;
        invulnerableReasons.Remove(ReasonDeath);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}