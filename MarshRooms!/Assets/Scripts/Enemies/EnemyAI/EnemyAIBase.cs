// Shared enemy AI infrastructure

using UnityEngine;
using TopDown.Movement;

public abstract class EnemyAIBase : MonoBehaviour, IEnemyAI
{
    protected enum State { Idle, Patrol, Chase, Engage, Return }
    protected State currentState = State.Idle;

    [Header("Idle / Patrol")]
    [SerializeField] protected float idleDurationMin;
    [SerializeField] protected float idleDurationMax;
    [SerializeField] protected float patrolDurationMin;
    [SerializeField] protected float patrolDurationMax;
    [SerializeField] protected float patrolRadius;
    [SerializeField] protected float patrolChance;

    [Header("Leash")]
    [SerializeField] protected float leashRadius;

    [Header("Line of Sight")]
    [SerializeField] protected LayerMask wallMask;

    [Header("Passive Speed")]
    [SerializeField] protected float passiveSpeedMultiplier;

    [Header("Spawn")]
    [SerializeField] protected float spawnGraceMin;
    [SerializeField] protected float spawnGraceMax ;

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

   // -- SPAWN GRACE --
    protected virtual bool HandleSpawnGrace() => false;

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

        RaycastHit2D hit = Physics2D.Raycast(origin, (target - origin).normalized, distance, wallMask);
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

    // -- DEFAULT CHASE --
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