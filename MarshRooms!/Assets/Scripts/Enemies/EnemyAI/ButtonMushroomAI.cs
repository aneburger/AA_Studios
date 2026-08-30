// Button Mushroom's own behaviour - patrol/idle, chase, windup, attack, return to spawn

using UnityEngine;
using TopDown.Movement;

public class ButtonMushroomAI : EnemyAIBase
{
    private enum EngagePhase { Windup, Attack }
    private EngagePhase engagePhase;

    private EnemyShooter shooter;
    private WeaponAimer weaponAimer;

    [Header("Windup")]
    [SerializeField] private float windupDurationMin = 0.4f;
    [SerializeField] private float windupDurationMax = 0.8f;

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

    private float windupTimer;
    private float windupTotalDuration;
    private bool isPrimed;

    private bool pendingMiss;
    private float pendingMissAngle;

    // -- AWAKE --
    protected override void Awake()
    {
        base.Awake();
        shooter = GetComponent<EnemyShooter>();
        weaponAimer = GetComponentInChildren<WeaponAimer>();
        mover.SetSpeed(enemy.Data.moveSpeed * Random.Range(0.9f, 1.1f));
    }

    // -- START --
    protected override void Start()
    {
        base.Start();

        effectiveAttackRange = enemy.Data.attackRange * Random.Range(attackRangeVarianceMin, attackRangeVarianceMax);
        shootsWhileMoving = Random.value < chanceToShootWhileMoving;
        shooter?.SetPermanentFireRateMultiplier(Random.Range(fireRateVarianceMin, fireRateVarianceMax));
    }

    // -- AIM AT PLAYER --
    private void AimAtPlayer()
    {
        Vector2 dir = DirectionToPlayer();
        weaponAimer?.SetAimDirection(dir);
        mover.SetFacingOverride(dir);
    }

    // ==================== IDLE  ====================
    protected override void HandleIdle(float distance)
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
    protected override void HandlePatrol(float distance)
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
    protected override void HandleChase(float distance)
    {
        mover.ClearFacingOverride();

        if (ForceChaseOnly)
        {
            Vector2 chaseDirection = pathing != null ? pathing.GetDirectionToTarget(player.position) : DirectionToPlayer();
            mover.Move(chaseDirection);
            return;
        }

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
                EnterEngage();

            return;
        }

        Vector2 direction = pathing != null ? pathing.GetDirectionToTarget(player.position) : DirectionToPlayer();
        mover.Move(direction);

        CheckLeash(distance);
    }

    protected override bool ShouldEngage(float distance) => false;

    // ==================== ENGAGE ====================
    protected override void EnterEngage()
    {
        currentState = State.Engage;
        EnterWindup();
    }

    protected override void HandleEngage(float distance)
    {
        switch (engagePhase)
        {
            case EngagePhase.Windup: HandleWindup(distance); break;
            case EngagePhase.Attack: HandleAttack(distance); break;
        }
    }

    private void EnterWindup()
    {
        engagePhase = EngagePhase.Windup;
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
            engagePhase = EngagePhase.Attack;
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

    // ==================== RETURN ====================
    protected override void HandleReturn(float distance)
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

    // -- DEBUG --
    protected override string GetExtraDebugInfo()
    {
        float distance = Vector2.Distance(transform.position, player.position);
        bool inRange = distance <= effectiveAttackRange && HasLineOfSight();
        bool canFire = shooter == null || shooter.CanFire();
        return $" attackRange={effectiveAttackRange:F2} inRange={inRange} shootsWhileMoving={shootsWhileMoving} canFire={canFire} engagePhase={engagePhase}";
    }

    // -- TOOK DAMAGE --
    protected override void HandleTookDamage()
    {
        isAlerted = true;

        if (currentState != State.Engage)
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
    protected override void HandlePlayerDeath()
    {
        CancelWindupVisual();
        base.HandlePlayerDeath();
    }
}