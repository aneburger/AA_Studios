// Shared enemy AI infrastructure - idle/patrol/chase/return, line of sight,

using UnityEngine;
using TopDown.Movement;

public abstract class EnemyAIBase : MonoBehaviour, IEnemyAI
{
    protected enum State { Idle, Patrol, Chase, Engage, Return }
    protected State currentState = State.Idle;

    [Header("Idle / Patrol")]
    [SerializeField] protected float idleDurationMin = 2f;
    [SerializeField] protected float idleDurationMax = 5f;
    [SerializeField] protected float patrolDurationMin = 1f;
    [SerializeField] protected float patrolDurationMax = 2f;
    [SerializeField] protected float patrolRadius = 2f;
    [SerializeField] protected float patrolChance = 0.5f;

    [Header("Leash")]
    [SerializeField] protected float leashRadius = 6f;

    [Header("Line of Sight")]
    [SerializeField] protected LayerMask wallMask;
    [SerializeField] protected float losSkin = 0.15f;

    [Header("Passive Speed")]
    [SerializeField] protected float passiveSpeedMultiplier = 0.5f;

    [Header("Spawn")]
    [SerializeField] protected float spawnGraceMin = 1f;
    [SerializeField] protected float spawnGraceMax = 2f;

    protected EnemyController enemy;
    protected EnemyMover mover;
    protected EnemyPathing pathing;
    protected EnemyHealth health;
    protected Transform player;

    protected Vector2 spawnPosition;
    protected Vector2 patrolTarget;
    protected float stateTimer;
    protected float spawnGraceTimer;
    protected bool skipGracePending;
    protected bool isAlerted;
    protected bool playerDead;

    // -- AWAKE --
    protected virtual void Awake()
    {
        enemy = GetComponent<EnemyController>();
        mover = GetComponent<EnemyMover>();
        pathing = GetComponent<EnemyPathing>();
        health = GetComponent<EnemyHealth>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    // -- START --
    protected virtual void Start()
    {
        spawnPosition = transform.position;
        spawnGraceTimer = skipGracePending ? 0f : Random.Range(spawnGraceMin, spawnGraceMax);
        EnterIdle();
    }

    // -- ENABLE --
    protected virtual void OnEnable()
    {
        PlayerHealth.OnPlayerDeath += HandlePlayerDeath;
        if (health != null) health.OnTookDamage += HandleTookDamage;
    }

    // -- DISABLE --
    protected virtual void OnDisable()
    {
        PlayerHealth.OnPlayerDeath -= HandlePlayerDeath;
        if (health != null) health.OnTookDamage -= HandleTookDamage;
    }

    // -- UPDATE --
    private void Update()
    {
        if (player == null || playerDead) return;
        if (HandleSpawnGrace()) return;

        float distance = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:   HandleIdle(distance);   break;
            case State.Patrol: HandlePatrol(distance); break;
            case State.Chase:  HandleChase(distance);  break;
            case State.Engage: HandleEngage(distance); break;
            case State.Return: HandleReturn(distance); break;
        }
    }

    protected virtual bool HandleSpawnGrace() => false;

    [Header("Debug")]
    [Tooltip("Logs state/distance/LOS to the console a few times a second while enabled.")]
    [SerializeField] protected bool debugLogging = false;
    private float debugLogTimer;

    private void LateUpdate()
    {
        if (!debugLogging || player == null) return;

        debugLogTimer -= Time.deltaTime;
        if (debugLogTimer > 0f) return;
        debugLogTimer = 0.25f;

        float distance = Vector2.Distance(transform.position, player.position);
        Debug.Log($"[{name}] state={currentState} dist={distance:F2} LOS={HasLineOfSight()}{GetExtraDebugInfo()}");
    }

    protected virtual string GetExtraDebugInfo() => "";

    // -- SKIP SPAWN GRACE --
    public virtual void SkipSpawnGrace()
    {
        skipGracePending = true;
        spawnGraceTimer = 0f;
    }

    // -- HELPERS --
    protected Vector2 DirectionToPlayer() => ((Vector2)player.position - (Vector2)transform.position).normalized;

    protected static Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    protected bool HasLineOfSight()
    {
        Vector2 origin = transform.position;
        Vector2 target = player.position;
        float distance = Vector2.Distance(origin, target);

        float castDistance = Mathf.Max(0f, distance - losSkin);

        RaycastHit2D hit = Physics2D.Raycast(origin, (target - origin).normalized, castDistance, wallMask);
        return hit.collider == null;
    }

    // ==================== IDLE ====================
    protected virtual void EnterIdle()
    {
        currentState = State.Idle;
        stateTimer = Random.Range(idleDurationMin, idleDurationMax);
        mover.SetSpeedMultiplier(passiveSpeedMultiplier);
    }

    protected virtual void HandleIdle(float distance)
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
    protected virtual void EnterPatrol()
    {
        currentState = State.Patrol;
        patrolTarget = spawnPosition + Random.insideUnitCircle * patrolRadius;
        stateTimer = Random.Range(patrolDurationMin, patrolDurationMax);
        mover.SetSpeedMultiplier(passiveSpeedMultiplier);
    }

    protected virtual void HandlePatrol(float distance)
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
    protected virtual void EnterChase()
    {
        currentState = State.Chase;
        mover.SetSpeedMultiplier(1f);
    }

    protected virtual void HandleChase(float distance)
    {
        mover.ClearFacingOverride();

        Vector2 direction = pathing != null ? pathing.GetDirectionToTarget(player.position) : DirectionToPlayer();
        mover.Move(direction);

        if (ShouldEngage(distance))
        {
            EnterEngage();
            return;
        }

        CheckLeash(distance);
    }

    protected abstract bool ShouldEngage(float distance);

    protected virtual void CheckLeash(float distance)
    {
        float distanceFromSpawn = Vector2.Distance(transform.position, spawnPosition);
        bool lostInterest = isAlerted ? (distanceFromSpawn > leashRadius)
                                       : (distance > enemy.Data.detectionRange || distanceFromSpawn > leashRadius);

        if (lostInterest)
            EnterReturn();
    }

    // ==================== ENGAGE ====================
    protected abstract void EnterEngage();
    protected abstract void HandleEngage(float distance);

    // ==================== RETURN ====================
    protected virtual void EnterReturn()
    {
        currentState = State.Return;
        isAlerted = false;
        mover.SetSpeedMultiplier(passiveSpeedMultiplier);
    }

    protected virtual void HandleReturn(float distance)
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

    // -- TOOK DAMAGE / PLAYER DEATH --
    protected abstract void HandleTookDamage();

    protected virtual void HandlePlayerDeath()
    {
        playerDead = true;
        mover.Stop();
        currentState = State.Idle;
    }
}