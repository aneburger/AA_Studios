// Inky - patrols/idles, chases and rolls toward the player

using UnityEngine;
using TopDown.Movement;

public class InkyAI : EnemyAIBase
{
    private enum RollPhase { Windup, Looping, Recovery }
    private RollPhase rollPhase;

    private Animator anim;

    [Header("Roll Trigger")]
    [SerializeField] private float rollRange;
    [SerializeField] private float rollTimerMin;
    [SerializeField] private float rollTimerMax;

    [Header("Roll Accuracy")]
    [SerializeField] private float rollInaccuracyMin;
    [SerializeField] private float rollInaccuracyMax;
    [SerializeField] private float rollDirectionRefreshInterval;
    [SerializeField] private float rollDirectionTurnSpeed;
    [SerializeField] protected LayerMask bumpMask;

    [Header("Roll Speed Ramp")]
    [SerializeField] private float rollSpeedStart;
    [SerializeField] private float rollSpeedMax;
    [SerializeField] private float rollSpeedRampDuration;

    [Header("Roll Duration")]
    [SerializeField] private float rollDurationMin;
    [SerializeField] private float rollDurationMax;

    [Header("Roll Recovery")]
    [SerializeField] private float maxRecoveryDuration;

    [Header("Audio")]
    [SerializeField] private AudioClip bumpClip;
    [Range(0f, 1f)] [SerializeField] private float bumpVolume;
    [SerializeField] private float bumpCooldown;
    [SerializeField] private AudioClip rollLoopClip;
    [Range(0f, 1f)] [SerializeField] private float rollLoopVolume;
    [SerializeField] private float rollLoopPitchVariation;

    private float lastBumpTime = -999f;
    private AudioSource rollLoopSource;

    private Vector2 rollDirection;
    private Vector2 rollTargetDirection;
    private float rollDirectionRefreshTimer;
    private float rollElapsed;
    private float rollPlannedDuration;
    private float rollTimer;
    private float recoveryTimer;

    // -- AWAKE --
    protected override void Awake()
    {
        base.Awake();
        anim = GetComponentInChildren<Animator>();
    }

    // -- START --
    protected override void Start()
    {
        base.Start();
        rollTimer = Random.Range(rollTimerMin, rollTimerMax);
    }

    protected override bool HandleSpawnGrace()
    {
        if (spawnGraceTimer > 0f)
        {
            spawnGraceTimer -= Time.deltaTime;
            mover.Stop();
            return true;
        }
        return false;
    }

    // ==================== CHASE ====================
    protected override void HandleChase(float distance)
    {
        rollTimer -= Time.deltaTime;
        base.HandleChase(distance);
    }

    protected override bool ShouldEngage(float distance) => distance <= rollRange && rollTimer <= 0f;

    // ==================== ENGAGE: ROLL ====================
    protected override void EnterEngage()
    {
        currentState = State.Engage;
        rollPhase = RollPhase.Windup;
        rollElapsed = 0f;
        rollPlannedDuration = Random.Range(rollDurationMin, rollDurationMax);

        mover.Stop();
        mover.SetFacingOverride(DirectionToPlayer());

        anim?.ResetTrigger("EndRoll");
        anim?.ResetTrigger("CancelRoll");
        anim?.SetTrigger("Roll");
    }

    protected override void HandleEngage(float distance)
    {
        switch (rollPhase)
        {
            case RollPhase.Windup:
                mover.Stop();
                break;

            case RollPhase.Looping:
                rollElapsed += Time.deltaTime;
                rollDirectionRefreshTimer -= Time.deltaTime;

                if (rollDirectionRefreshTimer <= 0f)
                {
                    RefreshRollTargetDirection();
                    rollDirectionRefreshTimer = rollDirectionRefreshInterval;
                }

                rollDirection = Vector2.Lerp(
                    rollDirection,
                    rollTargetDirection,
                    1f - Mathf.Exp(-rollDirectionTurnSpeed * Time.deltaTime)
                ).normalized;

                float rampT = Mathf.Clamp01(rollElapsed / rollSpeedRampDuration);
                mover.SetSpeedMultiplier(Mathf.Lerp(rollSpeedStart, rollSpeedMax, rampT));
                mover.Move(rollDirection);

                if (rollElapsed >= rollPlannedDuration)
                    EndRoll(instant: false);
                break;

            case RollPhase.Recovery:
                mover.Stop();
                recoveryTimer += Time.deltaTime;
                if (recoveryTimer >= maxRecoveryDuration)
                    OnRollComplete();
                break;
        }
    }

    private void RefreshRollTargetDirection()
    {
        Vector2 baseDirection = pathing != null ? pathing.GetDirectionToTarget(player.position) : DirectionToPlayer();
        float inaccuracy = Random.Range(rollInaccuracyMin, rollInaccuracyMax) * (Random.value < 0.5f ? -1f : 1f);
        rollTargetDirection = RotateVector(baseDirection, inaccuracy);
    }

    // -- ANIMATION EVENT --
    public void OnRollBurstStart()
    {
        rollPhase = RollPhase.Looping;
        RefreshRollTargetDirection();
        rollDirection = rollTargetDirection;
        rollDirectionRefreshTimer = rollDirectionRefreshInterval;
        mover.SetSpeedMultiplier(rollSpeedStart);

        float pitch = 1f + Random.Range(-rollLoopPitchVariation, rollLoopPitchVariation);
        AudioManager.Instance?.PlayLoopingSFX(ref rollLoopSource, rollLoopClip, rollLoopVolume, pitch);
    }

    // -- END ROLL --
    private void EndRoll(bool instant)
    {
        mover.SetSpeedMultiplier(1f);
        mover.Stop();
        rollTimer = Random.Range(rollTimerMin, rollTimerMax);

        AudioManager.Instance?.StopLoopingSFX(ref rollLoopSource);

        if (instant)
        {
            anim?.ResetTrigger("EndRoll");
            anim?.SetTrigger("CancelRoll");
            EnterChase();
        }
        else
        {
            rollPhase = RollPhase.Recovery;
            recoveryTimer = 0f;
            anim?.ResetTrigger("CancelRoll");
            anim?.SetTrigger("EndRoll");
        }
    }

    // -- COMPLETE ROLL --
    public void OnRollComplete()
    {
        rollTimer = Random.Range(rollTimerMin, rollTimerMax);
        EnterChase();
    }

    // -- COLLISION --
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & bumpMask) == 0) return;

        if (currentState == State.Engage && rollPhase == RollPhase.Looping)
        {
            Vector2 normal = collision.GetContact(0).normal;
            rollDirection = Vector2.Reflect(rollDirection, normal).normalized;
            rollTargetDirection = rollDirection;
        }

        if (Time.time - lastBumpTime < bumpCooldown) return;
        lastBumpTime = Time.time;
        AudioManager.Instance?.PlaySFXWithPitch(bumpClip, bumpVolume, 0.15f);
    }

    // -- TOOK DAMAGE --
    protected override void HandleTookDamage()
    {
        isAlerted = true;

        if (currentState == State.Engage && rollPhase == RollPhase.Looping)
        {
            EndRoll(instant: true);
            return;
        }

        if (currentState != State.Engage)
            EnterChase();
    }

    // -- PLAYER DEATH --
    protected override void HandlePlayerDeath()
    {
        AudioManager.Instance?.StopLoopingSFX(ref rollLoopSource);
        base.HandlePlayerDeath();
    }

    // -- ON DESTROY --
    private void OnDestroy()
    {
        AudioManager.Instance?.StopLoopingSFX(ref rollLoopSource);
    }
}