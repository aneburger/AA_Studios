// Boss health. Extends BaseHealth directly (NOT EnemyHealth, which destroys the object and drops loot).
// The boss must NOT have a BaseMover/EnemyMover, otherwise player bullets will knock him back.

using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BossHealth : BaseHealth
{
    // Invulnerability is tracked by reason so overlapping windows (e.g. hidden + phase transition)
    // can't accidentally clear each other.
    public const string ReasonIntro = "Intro";
    public const string ReasonHidden = "Hidden";
    public const string ReasonTransition = "Transition";
    public const string ReasonCharging = "Charging";
    public const string ReasonDeath = "Death";

    [Header("Phases")]
    [Range(0.05f, 0.95f)] [SerializeField] private float phaseTwoThreshold = 0.4f;
    [Tooltip("Health can't drop below 1 until phase two has actually started, so a big hit can't skip the counter phase and the transition.")]
    [SerializeField] private bool protectUntilPhaseTwo = true;

    [Header("Start State")]
    [Tooltip("Boss can't be hurt until the intro finishes (stops shots from the hallway).")]
    [SerializeField] private bool startInvulnerable = true;

    [Header("Hit Reaction")]
    [Tooltip("Minimum time between hurt animations, so rapid fire can't lock him in the flinch.")]
    [SerializeField] private float flinchCooldown = 0.4f;

    [Header("Spore Drops (on hit)")]
    [SerializeField] private GameObject sporePrefab;
    [Range(0f, 1f)] [SerializeField] private float sporeDropChance = 0.12f;
    [SerializeField] private int minSporesPerDrop = 1;
    [SerializeField] private int maxSporesPerDrop = 1;
    [SerializeField] private float sporeScatterRadius = 1f;
    [Tooltip("Minimum time between drops, so fast weapons can't flood the floor.")]
    [SerializeField] private float sporeDropCooldown = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtClip;
    [Range(0f, 1f)] [SerializeField] private float hurtVolume = 1f;

    [Header("Invulnerable Feedback")]
    [Tooltip("Played when a hit lands while he's invulnerable, so it's clear the shot did nothing.")]
    [SerializeField] private AudioClip deflectClip;
    [Range(0f, 1f)] [SerializeField] private float deflectVolume = 1f;
    [Tooltip("Minimum time between deflect sounds, so rapid fire can't spam it.")]
    [SerializeField] private float deflectCooldown = 0.08f;

    public event Action<float, float> OnHealthChanged;   // current, max
    public event Action OnTookDamage;                     // same name as EnemyHealth's
    public event Action OnPhaseTwoThreshold;              // fires once
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

    // -- FLINCH -- (the brain turns this off during attacks so the hurt animation can't interrupt them)
    public void SetFlinchEnabled(bool value)
    {
        flinchEnabled = value;
    }

    // -- PHASE TWO STARTED -- (called by the brain when the transition finishes; lifts the 1 HP floor)
    public void NotifyPhaseTwoStarted()
    {
        phaseTwoStarted = true;
    }

    // -- TAKE DAMAGE --
    public override void TakeDamage(float amount)
    {
        if (IsDead()) return;

        // The bullet is still absorbed by the boss, but the player gets audio feedback that it did nothing
        if (IsInvulnerable)
        {
            PlayDeflect();
            return;
        }

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

        base.TakeDamage(amount);      // subtracts, hurt animation trigger, OnHitEffect, Die() if dead

        VFXManager.Instance?.SpawnDamageNumber(amount * 10, GetTopPosition());

        if (IsDead()) return;

        OnTookDamage?.Invoke();
        TryDropSpores();
        CheckPhaseThreshold();
    }

    // -- HIT EFFECT -- (called by base after health is reduced, before Die)
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
        if (HealthFraction > phaseTwoThreshold) return;

        phaseTwoTriggered = true;
        Debug.Log($"[BossHealth] Phase threshold crossed at {HealthFraction:P0}.");
        OnPhaseTwoThreshold?.Invoke();
    }

    // -- DIE -- (no Destroy: the boss plays his death animation and the outro takes over)
    protected override void Die()
    {
        SetInvulnerable(ReasonDeath, true);
        base.Die();
        Debug.Log("[BossHealth] Boss died.");
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