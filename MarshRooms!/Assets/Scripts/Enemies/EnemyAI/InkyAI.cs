// Inky - patrols/idles, chases and rolls toward the player

using UnityEngine;
using TopDown.Movement;

public class InkyAI : MonoBehaviour, IEnemyAI
{
    private enum State { Idle, Patrol, Chase, Roll, Return }
    private enum RollPhase { Windup, Looping, Recovery }

    [Header("Idle / Patrol")]
    [SerializeField] private float idleDurationMin = 1.5f;
    [SerializeField] private float idleDurationMax = 4f;
    [SerializeField] private float patrolDurationMin = 1f;
    [SerializeField] private float patrolDurationMax = 2f;
    [SerializeField] private float patrolRadius = 2f;
    [SerializeField] private float patrolChance = 0.5f;

    [Header("Line of Sight")]
    [SerializeField] private LayerMask wallMask;

    [Header("Roll Trigger")]
    [SerializeField] private float rollRange = 6f;
    [SerializeField] private float rollTimerMin = 2f;
    [SerializeField] private float rollTimerMax = 5f;

    [Header("Roll Accuracy")]
    [SerializeField] private float rollInaccuracyMin = 15f;
    [SerializeField] private float rollInaccuracyMax = 40f;
    [SerializeField] private float rollDirectionRefreshInterval = 0.5f;
    [SerializeField] private float rollDirectionTurnSpeed = 4f;

    [Header("Roll Speed Ramp")]
    [SerializeField] private float rollSpeedStart = 1.5f;
    [SerializeField] private float rollSpeedMax = 3.5f;
    [SerializeField] private float rollSpeedRampDuration = 1.5f;

    [Header("Roll Duration")]
    [SerializeField] private float rollDurationMin = 2f;
    [SerializeField] private float rollDurationMax = 4f;

    [Header("Roll Recovery")]
    [SerializeField] private float maxRecoveryDuration = 0.6f;

    [Header("Leash")]
    [SerializeField] private float leashRadius = 6f;

    [Header("Passive Speed")]
    [SerializeField] private float passiveSpeedMultiplier = 0.5f;

    [Header("Spawn")]
    [SerializeField] private float spawnGraceMin = 0.3f;
    [SerializeField] private float spawnGraceMax = 0.8f;

    [Header("Audio")]
    [SerializeField] private AudioClip bumpClip;
    [Range(0f, 1f)] [SerializeField] private float bumpVolume = 0.6f;
    [SerializeField] private float bumpCooldown = 0.3f;
    [SerializeField] private AudioClip rollLoopClip;
    [Range(0f, 1f)] [SerializeField] private float rollLoopVolume = 0.6f;
    [SerializeField] private float rollLoopPitchVariation = 0.1f;

    private float lastBumpTime = -999f;
    private AudioSource rollLoopSource;

    private EnemyController enemy;
    private EnemyMover mover;
    private EnemyPathing pathing;
    private EnemyHealth health;
    private Animator anim;
    private Transform player;

    private Vector2 spawnPosition;
    private Vector2 patrolTarget;
    private State currentState = State.Idle;
    private RollPhase rollPhase;

    private Vector2 rollDirection;
    private Vector2 rollTargetDirection;
    private float rollDirectionRefreshTimer;
    private float rollElapsed;
    private float rollPlannedDuration;
    private float rollTimer;
    private float recoveryTimer;

    private float stateTimer;
    private float spawnGraceTimer;
    private bool isAlerted;
    private bool playerDead;

    // -- AWAKE --
    private void Awake()
    {
        enemy = GetComponent<EnemyController>();
        mover = GetComponent<EnemyMover>();
        pathing = GetComponent<EnemyPathing>();
        health = GetComponent<EnemyHealth>();
        anim = GetComponentInChildren<Animator>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    // -- START --
    private void Start()
    {
        spawnPosition = transform.position;
        spawnGraceTimer = Random.Range(spawnGraceMin, spawnGraceMax);
        rollTimer = Random.Range(rollTimerMin, rollTimerMax);
        EnterIdle();
    }

    private void OnEnable()
    {
        PlayerHealth.OnPlayerDeath += HandlePlayerDeath;
        if (health != null) health.OnTookDamage += HandleTookDamage;
    }

    private void OnDisable()
    {
        PlayerHealth.OnPlayerDeath -= HandlePlayerDeath;
        if (health != null) health.OnTookDamage -= HandleTookDamage;
    }

    // -- UPDATE --
    private void Update()
    {
        if (player == null || playerDead) return;

        if (spawnGraceTimer > 0f)
        {
            spawnGraceTimer -= Time.deltaTime;
            mover.Stop();
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:    HandleIdle(distance);    break;
            case State.Patrol:  HandlePatrol(distance);  break;
            case State.Chase:   HandleChase(distance);   break;
            case State.Roll:    HandleRoll(distance);    break;
            case State.Return:  HandleReturn(distance);  break;
        }
    }

    // -- HELPERS --
    private Vector2 DirectionToPlayer() => ((Vector2)player.position - (Vector2)transform.position).normalized;

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private bool HasLineOfSight()
    {
        Vector2 origin = transform.position;
        Vector2 target = player.position;
        float distance = Vector2.Distance(origin, target);

        RaycastHit2D hit = Physics2D.Raycast(origin, (target - origin).normalized, distance, wallMask);
        return hit.collider == null;
    }

    // ==================== IDLE ====================
    private void EnterIdle()
    {
        currentState = State.Idle;
        stateTimer = Random.Range(idleDurationMin, idleDurationMax);
        mover.SetSpeedMultiplier(passiveSpeedMultiplier);
    }

    private void HandleIdle(float distance)
    {
        mover.Stop();
        mover.ClearFacingOverride();

        if (distance <= enemy.Data.detectionRange && HasLineOfSight())
        {
            EnterChase();
            return;
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            if (Random.value < patrolChance)
                EnterPatrol();
            else
                EnterIdle();
        }
    }

    // ==================== PATROL ====================
    private void EnterPatrol()
    {
        currentState = State.Patrol;
        patrolTarget = spawnPosition + Random.insideUnitCircle * patrolRadius;
        stateTimer = Random.Range(patrolDurationMin, patrolDurationMax);
        mover.SetSpeedMultiplier(passiveSpeedMultiplier);
    }

    private void HandlePatrol(float distance)
    {
        if (distance <= enemy.Data.detectionRange && HasLineOfSight())
        {
            EnterChase();
            return;
        }

        Vector2 direction = pathing != null ? pathing.GetDirectionToTarget(patrolTarget) : Vector2.zero;
        mover.Move(direction);

        stateTimer -= Time.deltaTime;
        bool reachedTarget = Vector2.Distance(transform.position, patrolTarget) < 0.2f;

        if (stateTimer <= 0f || reachedTarget)
            EnterIdle();
    }

    // ==================== CHASE ====================
    private void EnterChase()
    {
        currentState = State.Chase;
        mover.SetSpeedMultiplier(1f);
    }

    private void HandleChase(float distance)
    {
        mover.ClearFacingOverride();

        Vector2 direction = pathing != null ? pathing.GetDirectionToTarget(player.position) : DirectionToPlayer();
        mover.Move(direction);

        rollTimer -= Time.deltaTime;

        if (distance <= rollRange && rollTimer <= 0f)
        {
            EnterRoll();
            return;
        }

        float distanceFromSpawn = Vector2.Distance(transform.position, spawnPosition);
        bool lostInterest = isAlerted ? (distanceFromSpawn > leashRadius)
                                       : (distance > enemy.Data.detectionRange || distanceFromSpawn > leashRadius);

        if (lostInterest)
            EnterReturn();
    }

    // ==================== ROLL ====================
    private void EnterRoll()
    {
        currentState = State.Roll;
        rollPhase = RollPhase.Windup;
        rollElapsed = 0f;
        rollPlannedDuration = Random.Range(rollDurationMin, rollDurationMax);

        mover.Stop();
        mover.SetFacingOverride(DirectionToPlayer());

        anim?.SetTrigger("Roll");
    }

    private void HandleRoll(float distance)
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
            anim?.SetTrigger("CancelRoll");
            EnterChase();
        }
        else
        {
            rollPhase = RollPhase.Recovery;
            recoveryTimer = 0f;
            anim?.SetTrigger("EndRoll");
        }
    }

    // -- COMPLETE ROLL --
    public void OnRollComplete()
    {
        rollTimer = Random.Range(rollTimerMin, rollTimerMax);
        EnterChase();
    }

    // ==================== RETURN ====================
    private void EnterReturn()
    {
        currentState = State.Return;
        isAlerted = false;
        mover.SetSpeedMultiplier(passiveSpeedMultiplier);
    }

    private void HandleReturn(float distance)
    {
        mover.ClearFacingOverride();

        if (distance <= enemy.Data.detectionRange && HasLineOfSight())
        {
            EnterChase();
            return;
        }

        Vector2 direction = pathing != null ? pathing.GetDirectionToTarget(spawnPosition) : Vector2.zero;
        mover.Move(direction);

        if (Vector2.Distance(transform.position, spawnPosition) < 0.3f)
            EnterIdle();
    }

    // -- COLLISION --
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & wallMask) == 0) return;
        if (Time.time - lastBumpTime < bumpCooldown) return;

        lastBumpTime = Time.time;
        AudioManager.Instance?.PlaySFXWithPitch(bumpClip, bumpVolume, 0.15f);
    }

    // -- TOOK DAMAGE --
    private void HandleTookDamage()
    {
        isAlerted = true;

        if (currentState == State.Roll && rollPhase == RollPhase.Looping)
        {
            EndRoll(instant: true);
            return;
        }

        if (currentState != State.Roll)
            EnterChase();
    }

    // -- PLAYER DEATH --
    private void HandlePlayerDeath()
    {
        playerDead = true;
        AudioManager.Instance?.StopLoopingSFX(ref rollLoopSource);
        mover.Stop();
        currentState = State.Idle;
    }

    // -- ON DESTROY --
    private void OnDestroy()
    {
        AudioManager.Instance?.StopLoopingSFX(ref rollLoopSource);
    }
}