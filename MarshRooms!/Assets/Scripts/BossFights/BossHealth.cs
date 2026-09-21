using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BossHealth : BaseHealth
{
    public const string ReasonIntro = "Intro";
    public const string ReasonHidden = "Hidden";
    public const string ReasonTransition = "Transition";
    public const string ReasonDeath = "Death";

    [Header("Phases")]
    [Range(0.05f, 0.95f)] [SerializeField] private float phaseTwoThreshold = 0.4f;
    [SerializeField] private bool protectUntilPhaseTwo = true;

    [Header("Start State")]
    [SerializeField] private bool startInvulnerable = true;

    [Header("Hit Reaction")]
    [SerializeField] private float flinchCooldown = 0.4f;

    [Header("Spore Drops (on hit)")]
    [SerializeField] private GameObject sporePrefab;
    [Range(0f, 1f)] [SerializeField] private float sporeDropChance = 0.12f;
    [SerializeField] private int minSporesPerDrop = 1;
    [SerializeField] private int maxSporesPerDrop = 1;
    [SerializeField] private float sporeScatterRadius = 1f;
    [SerializeField] private float sporeDropCooldown = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtClip;
    [Range(0f, 1f)] [SerializeField] private float hurtVolume = 1f;

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

    public bool IsInvulnerable => invulnerableReasons.Count > 0;
    public bool PhaseTwoTriggered => phaseTwoTriggered;
    public bool PhaseTwoStarted => phaseTwoStarted;
    public float HealthFraction => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

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
        if (IsInvulnerable) return;

        // Can't die before phase two has started, however hard the hit is
        if (protectUntilPhaseTwo && !phaseTwoStarted)
        {
            amount = Mathf.Min(amount, currentHealth - 1f);
            if (amount <= 0f) return;
        }

        // Hurt animation only if allowed right now and not on cooldown
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
        if (HealthFraction > phaseTwoThreshold) return;

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