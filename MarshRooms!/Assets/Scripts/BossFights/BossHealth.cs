// Boss health. Extends BaseHealth directly

using System;
using System.Collections.Generic;
using UnityEngine;

public class BossHealth : BaseHealth
{
    public const string ReasonIntro = "Intro";
    public const string ReasonHidden = "Hidden";
    public const string ReasonTransition = "Transition";
    public const string ReasonDeath = "Death";

    [Header("Phases")]
    [Range(0.05f, 0.95f)] [SerializeField] private float phaseTwoThreshold = 0.66f;

    [Header("Start State")]
    [SerializeField] private bool startInvulnerable = true;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtClip;
    [Range(0f, 1f)] [SerializeField] private float hurtVolume = 1f;

    public event Action<float, float> OnHealthChanged;
    public event Action OnTookDamage;
    public event Action OnPhaseTwoThreshold;
    public event Action OnDied;

    private readonly HashSet<string> invulnerableReasons = new HashSet<string>();
    private bool phaseTwoTriggered;

    public bool IsInvulnerable => invulnerableReasons.Count > 0;
    public bool PhaseTwoTriggered => phaseTwoTriggered;
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

    // -- TAKE DAMAGE --
    public override void TakeDamage(float amount)
    {
        if (IsDead()) return;
        if (IsInvulnerable) return;

        base.TakeDamage(amount);

        VFXManager.Instance?.SpawnDamageNumber(amount * 10, GetTopPosition());

        if (IsDead()) return;

        OnTookDamage?.Invoke();
        CheckPhaseThreshold();
    }

    // -- HIT EFFECT --
    protected override void OnHitEffect()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (!IsDead())
            AudioManager.Instance?.PlaySFXWithPitch(hurtClip, hurtVolume, 0.2f);
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

    // -- DIE --
    protected override void Die()
    {
        SetInvulnerable(ReasonDeath, true);
        base.Die();
        OnDied?.Invoke();
    }

    // -- TOP POSITION --
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
        invulnerableReasons.Remove(ReasonDeath);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
