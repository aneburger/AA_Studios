// Button Mushroom's own behaviour - patrol/idle, chase, windup, attack, return to spawn
// Replaces generic EnemyAI on this enemy type

using UnityEngine;
using TopDown.Movement;

public class ButtonMushroomAI : MonoBehaviour, IEnemyAI
{
    private enum State { Idle, Patrol, Chase, Windup, Attack, Return }

    private State currentState = State.Idle;
    private bool playerDead = false;
    private bool isAlerted = false;

    private EnemyController enemy;
    private EnemyMover mover;
    private EnemyShooter shooter;
    private WeaponAimer weaponAimer;
    private EnemyPathing pathing;
    private EnemyHealth health;
    private Transform player;

    private Vector2 spawnPosition;

    [Header("Idle / Patrol")]
    [SerializeField] private float idleDurationMin = 2f;
    [SerializeField] private float idleDurationMax = 5f;
    [SerializeField] private float patrolDurationMin = 1f;
    [SerializeField] private float patrolDurationMax = 2f;
    [SerializeField] private float patrolRadius = 2f;
    [SerializeField] private float patrolChance = 0.5f;

    [Header("Leash")]
    [SerializeField] private float leashRadius = 6f;

    [Header("Windup")]
    [SerializeField] private float windupDurationMin = 0.4f;
    [SerializeField] private float windupDurationMax = 0.8f;

    [Header("Line of Sight")]
    [SerializeField] private LayerMask wallMask;

    [Header("Windup Visual")]
    [SerializeField] private AudioClip windupClip;
    [Range(0f, 1f)] [SerializeField] private float windupVolume;

    [Header("Accuracy")]
    [SerializeField] private float missChance = 0.2f;
    [SerializeField] private float missAngleMin = 12f;
    [SerializeField] private float missAngleMax = 30f;

    [Header("Hit Reaction")]
    [SerializeField] private float hitSlowMultiplier = 0.5f;
    [SerializeField] private float hitSlowDuration = 0.15f;

    [Header("Spawn")]
    [SerializeField] private float spawnGraceMin = 1f;
    [SerializeField] private float spawnGraceMax = 2f;

    [Header("Attack Range Variance")]
    [SerializeField] private float attackRangeVarianceMin = 0.85f;
    [SerializeField] private float attackRangeVarianceMax = 1.15f;

    [Header("Reposition On Miss")]
    [SerializeField] private float moveCloserChance = 0.5f;
    [SerializeField] private float moveCloserAmount = 0.7f;
    [SerializeField] private float minAttackRange = 1.5f;

    [Header("Fire Rate Variance")]
    [SerializeField] private float fireRateVarianceMin = 0.85f;
    [SerializeField] private float fireRateVarianceMax = 1.2f;

    [Header("Shooting Style")]
    [Range(0f, 1f)] [SerializeField] private float chanceToShootWhileMoving = 0.3f;

    private bool shootsWhileMoving;
    private float effectiveAttackRange;

    private Coroutine hitSlowCoroutine;
    private Coroutine windupVisualCoroutine;

    private float stateTimer;
    private Vector2 patrolTarget;
    private float windupTimer;

    private float spawnGraceTimer;
    private bool skipGracePending = false;

    private bool isPrimed = false;

    private bool pendingMiss;
    private float pendingMissAngle;
    private float windupTotalDuration;

    // -- AWAKE --
    private void Awake()
    {
        enemy = GetComponent<EnemyController>();
        mover = GetComponent<EnemyMover>();
        shooter = GetComponent<EnemyShooter>();
        weaponAimer = GetComponentInChildren<WeaponAimer>();
        pathing = GetComponent<EnemyPathing>();
        health = GetComponent<EnemyHealth>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        mover.SetSpeed(enemy.Data.moveSpeed * Random.Range(0.9f, 1.1f));
    }

    // -- START --
    private void Start()
    {
        spawnPosition = transform.position;
        spawnGraceTimer = skipGracePending ? 0f : Random.Range(spawnGraceMin, spawnGraceMax);

        effectiveAttackRange = enemy.Data.attackRange * Random.Range(attackRangeVarianceMin, attackRangeVarianceMax);
        shootsWhileMoving = Random.value < chanceToShootWhileMoving;
        shooter?.SetPermanentFireRateMultiplier(Random.Range(fireRateVarianceMin, fireRateVarianceMax));

        EnterIdle();
    }

    // -- ENABLE--
    private void OnEnable()
    {
        PlayerHealth.OnPlayerDeath += HandlePlayerDeath;
        if (health != null) health.OnTookDamage += HandleTookDamage;
    }

    // -- DISABLE -- 
    private void OnDisable()
    {
        PlayerHealth.OnPlayerDeath -= HandlePlayerDeath;
        if (health != null) health.OnTookDamage -= HandleTookDamage;
    }

    // -- UPDATE --
    private void Update()
    {
        if (player == null || playerDead) return;

        float distance = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:    HandleIdle(distance);    break;
            case State.Patrol:  HandlePatrol(distance);  break;
            case State.Chase:   HandleChase(distance);   break;
            case State.Windup:  HandleWindup(distance);  break;
            case State.Attack:  HandleAttack(distance);  break;
            case State.Return:  HandleReturn(distance);  break;
        }
    }

    // -- SKIP SPAWN GRACE --
    public void SkipSpawnGrace()
    {
        skipGracePending = true;
        spawnGraceTimer = 0f;
    }

    // -- DIRECTION TO PLAYER --
    private Vector2 DirectionToPlayer()
    {
        return ((Vector2)player.position - (Vector2)transform.position).normalized;
    }

    // -- AIM AT PLAYER --
    private void AimAtPlayer()
    {
        Vector2 dir = DirectionToPlayer();
        weaponAimer?.SetAimDirection(dir);
        mover.SetFacingOverride(dir);
    }

    // ==================== IDLE ====================
    private void EnterIdle()
    {
        currentState = State.Idle;
        stateTimer = Random.Range(idleDurationMin, idleDurationMax);
    }

    private void HandleIdle(float distance)
    {
        mover.Stop();
        mover.ClearFacingOverride();

        if (spawnGraceTimer > 0f)
            spawnGraceTimer -= Time.deltaTime;

        if (spawnGraceTimer <= 0f && distance <= enemy.Data.detectionRange && HasLineOfSight())
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
    }

    private void HandlePatrol(float distance)
    {
        if (spawnGraceTimer > 0f)
            spawnGraceTimer -= Time.deltaTime;

        if (spawnGraceTimer <= 0f && distance <= enemy.Data.detectionRange && HasLineOfSight())
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
    }

    private void HandleChase(float distance)
    {
        mover.ClearFacingOverride();

        bool inRange = distance <= effectiveAttackRange && HasLineOfSight();

        if (inRange)
        {
            if (!shootsWhileMoving)
            {
                mover.Stop();
            }
            else
            {
                Vector2 approachDirection = pathing != null ? pathing.GetDirectionToTarget(player.position) : DirectionToPlayer();
                mover.Move(approachDirection);
            }

            AimAtPlayer();

            if (shooter == null || shooter.CanFire())
                EnterWindup();

            return;
        }

        Vector2 direction = pathing != null ? pathing.GetDirectionToTarget(player.position) : DirectionToPlayer();
        mover.Move(direction);

        float distanceFromSpawn = Vector2.Distance(transform.position, spawnPosition);
        bool lostInterest = isAlerted ? (distanceFromSpawn > leashRadius)
                                    : (distance > enemy.Data.detectionRange || distanceFromSpawn > leashRadius);

        if (lostInterest)
            EnterReturn();
    }

    // ==================== WINDUP ====================
    private void EnterWindup()
    {
        currentState = State.Windup;
        windupTimer = Random.Range(windupDurationMin, windupDurationMax);
        windupTotalDuration = windupTimer;

        pendingMiss = Random.value < missChance;
        pendingMissAngle = pendingMiss
            ? Random.Range(missAngleMin, missAngleMax) * (Random.value < 0.5f ? -1f : 1f)
            : 0f;

        if (!isPrimed)
        {
            if (windupVisualCoroutine != null) StopCoroutine(windupVisualCoroutine);
            windupVisualCoroutine = StartCoroutine(PlayWindupVisual(windupTimer));
        }
    }

    private void HandleWindup(float distance)
    {
        if (!shootsWhileMoving)
        {
             mover.Stop();
        } 
        else
        {
            Vector2 direction = pathing != null ? pathing.GetDirectionToTarget(player.position) : DirectionToPlayer();
            mover.Move(direction);
        }

        mover.Stop();

        Vector2 trueDirection = DirectionToPlayer();
        mover.SetFacingOverride(trueDirection);

        Vector2 aimDirection = trueDirection;
        if (pendingMiss)
        {
            float elapsedFraction = 1f - (windupTimer / windupTotalDuration);
            if (elapsedFraction >= 0.7f)
            {
                float slipT = Mathf.InverseLerp(0.7f, 1f, elapsedFraction);
                Vector2 missedDirection = RotateVector(trueDirection, pendingMissAngle);
                aimDirection = Vector2.Lerp(trueDirection, missedDirection, slipT);
            }
        }
        weaponAimer?.SetAimDirection(aimDirection);

        if (distance > effectiveAttackRange * 1.2f || !HasLineOfSight())
        {
            CancelWindupVisual();
            EnterChase();
            return;
        }

        windupTimer -= Time.deltaTime;
        if (windupTimer <= 0f)
            EnterAttack();
    }

    private void CancelWindupVisual()
    {
        if (windupVisualCoroutine != null)
        {
            StopCoroutine(windupVisualCoroutine);
            windupVisualCoroutine = null;
        }
    }

    private System.Collections.IEnumerator PlayWindupVisual(float duration)
    {
        AudioManager.Instance?.PlaySFXWithPitch(windupClip, windupVolume, 0.1f);
        isPrimed = true;

        int pulseCount = 2;
        float interval = duration / pulseCount;

        for (int i = 0; i < pulseCount; i++)
        {
            shooter?.SquishEffect();
            yield return new WaitForSeconds(interval);
        }

        windupVisualCoroutine = null;
    }

    // ==================== ATTACK ====================
    private void EnterAttack()
    {
        currentState = State.Attack;
    }

    private void HandleAttack(float distance)
    {
        Vector2 trueDirection = DirectionToPlayer();
        Vector2 shootDirection = pendingMiss ? RotateVector(trueDirection, pendingMissAngle) : trueDirection;

        mover.SetFacingOverride(trueDirection);
        weaponAimer?.SetAimDirection(shootDirection);

        shooter?.TryShoot();

        if (pendingMiss && Random.value < moveCloserChance)
            effectiveAttackRange = Mathf.Max(minAttackRange, effectiveAttackRange * moveCloserAmount);

        isPrimed = false;
        EnterChase();
    }

    // -- ROTATE VECTOR  --
    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // ==================== RETURN ====================
   private void EnterReturn()
    {
        currentState = State.Return;
        isAlerted = false;
    }

    private void HandleReturn(float distance)
    {
        mover.ClearFacingOverride();

        if (distance <= enemy.Data.detectionRange)
        {
            EnterChase();
            return;
        }

        Vector2 direction = pathing != null ? pathing.GetDirectionToTarget(spawnPosition) : Vector2.zero;
        mover.Move(direction);

        if (Vector2.Distance(transform.position, spawnPosition) < 0.3f)
            EnterIdle();
    }

    // -- TOOK DAMAGE --
    private void HandleTookDamage()
    {
        isAlerted = true;

        if (currentState != State.Windup && currentState != State.Attack)
            EnterChase();

        if (hitSlowCoroutine != null) StopCoroutine(hitSlowCoroutine);
        hitSlowCoroutine = StartCoroutine(HitSlow());
    }

    private System.Collections.IEnumerator HitSlow()
    {
        mover.SetSpeedMultiplier(hitSlowMultiplier);
        yield return new WaitForSeconds(hitSlowDuration);
        mover.SetSpeedMultiplier(1f);
        hitSlowCoroutine = null;
    }

    // -- PLAYER DEATH --
    private void HandlePlayerDeath()
    {
        playerDead = true;
        CancelWindupVisual();
        mover.Stop();
        currentState = State.Idle;
    }

    // -- HAS LINE OF SIGHT --
    private bool HasLineOfSight()
    {
        Vector2 origin = transform.position;
        Vector2 target = player.position;
        float distance = Vector2.Distance(origin, target);

        RaycastHit2D hit = Physics2D.Raycast(origin, (target - origin).normalized, distance, wallMask);
        return hit.collider == null;
    }
}