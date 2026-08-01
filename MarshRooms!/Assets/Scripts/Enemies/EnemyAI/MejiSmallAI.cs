// Mini Shemeji mushroom - no weapon. Spawns disoriented and scrambles

using UnityEngine;
using TopDown.Movement;

public class MejiSmallAI : EnemyAIBase
{
    private enum EngagePhase { Dash, Stagger }
    private EngagePhase engagePhase;

    [Header("Panic (spawn grace)")]
    [SerializeField] private float panicRadius = 3f;
    [SerializeField] private float panicTargetRefreshMin = 0.3f;
    [SerializeField] private float panicTargetRefreshMax = 0.7f;
    [SerializeField] private float panicSpeedMultiplier = 1.6f;

    private Vector2 panicTarget;
    private float panicTargetTimer;

    [Header("Dash Trigger")]
    [SerializeField] private float dashRange = 1.5f;

    [Header("Dash")]
    [SerializeField] private float dashDuration = 0.35f;
    [SerializeField] private float dashSpeedMultiplier = 2.2f;
    [SerializeField] private float dashInaccuracyMin = 20f;
    [SerializeField] private float dashInaccuracyMax = 50f;

    [Header("Stagger Recovery")]
    [SerializeField] private float staggerDuration = 0.5f;
    [SerializeField] private float staggerSpeedMultiplier = 0.25f;

    [Header("Squash Stretch (optional - leave sprite empty to skip)")]
    [SerializeField] private Transform spriteTransform;
    [SerializeField] private float squashScaleX = 1.3f;
    [SerializeField] private float squashScaleY = 0.7f;
    [SerializeField] private float squashDuration = 0.15f;

    private float engageTimer;
    private Vector2 dashDirection;
    private Coroutine squashCoroutine;

    // -- AWAKE --
    protected override void Awake()
    {
        base.Awake();

        if (spriteTransform == null)
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) spriteTransform = sr.transform;
        }
    }

    // -- START -- 
    protected override void Start()
    {
        spawnPosition = transform.position;
        spawnGraceTimer = skipGracePending ? 0f : Random.Range(spawnGraceMin, spawnGraceMax);
        PickNewPanicTarget();
    }

    private bool panicHandled;

    // ==================== PANIC ====================
    protected override bool HandleSpawnGrace()
    {
        if (spawnGraceTimer > 0f)
        {
            spawnGraceTimer -= Time.deltaTime;
            HandlePanicMovement();
            return true;
        }

        if (!panicHandled)
        {
            panicHandled = true;
            EnterChase();
        }

        return false;
    }

    private void HandlePanicMovement()
    {
        mover.ClearFacingOverride();
        mover.SetSpeedMultiplier(panicSpeedMultiplier);

        panicTargetTimer -= Time.deltaTime;
        bool reachedTarget = Vector2.Distance(transform.position, panicTarget) < 0.3f;

        if (panicTargetTimer <= 0f || reachedTarget)
            PickNewPanicTarget();

        Vector2 direction = pathing != null ? pathing.GetDirectionToTarget(panicTarget) : Vector2.zero;
        mover.Move(direction);
    }

    private void PickNewPanicTarget()
    {
        panicTarget = spawnPosition + Random.insideUnitCircle * panicRadius;
        panicTargetTimer = Random.Range(panicTargetRefreshMin, panicTargetRefreshMax);
    }

    // ==================== CHASE ====================
    protected override bool ShouldEngage(float distance) => distance <= dashRange && HasLineOfSight();

    // ==================== ENGAGE ====================
    protected override void EnterEngage()
    {
        currentState = State.Engage;
        engagePhase = EngagePhase.Dash;
        engageTimer = dashDuration;

        Vector2 trueDirection = DirectionToPlayer();
        float missAngle = Random.Range(dashInaccuracyMin, dashInaccuracyMax) * (Random.value < 0.5f ? -1f : 1f);
        dashDirection = RotateVector(trueDirection, missAngle);

        mover.SetFacingOverride(dashDirection);
        mover.SetSpeedMultiplier(dashSpeedMultiplier);

        if (spriteTransform != null)
        {
            if (squashCoroutine != null) StopCoroutine(squashCoroutine);
            squashCoroutine = StartCoroutine(SquashStretch());
        }
    }

    protected override void HandleEngage(float distance)
    {
        switch (engagePhase)
        {
            case EngagePhase.Dash:
                mover.Move(dashDirection);
                engageTimer -= Time.deltaTime;
                if (engageTimer <= 0f)
                    EnterStagger();
                break;

            case EngagePhase.Stagger:
                mover.Stop();
                engageTimer -= Time.deltaTime;
                if (engageTimer <= 0f)
                    EnterChase();
                break;
        }
    }

    private void EnterStagger()
    {
        engagePhase = EngagePhase.Stagger;
        engageTimer = staggerDuration;
        mover.ClearFacingOverride();
        mover.SetSpeedMultiplier(staggerSpeedMultiplier);
    }

    private System.Collections.IEnumerator SquashStretch()
    {
        Vector3 original = spriteTransform.localScale;
        Vector3 squished = new Vector3(original.x * squashScaleX, original.y * squashScaleY, original.z);

        float t = 0f;
        while (t < squashDuration)
        {
            t += Time.deltaTime;
            spriteTransform.localScale = Vector3.Lerp(squished, original, t / squashDuration);
            yield return null;
        }

        spriteTransform.localScale = original;
        squashCoroutine = null;
    }

    // -- TOOK DAMAGE --
    protected override void HandleTookDamage()
    {
        isAlerted = true;
        spawnGraceTimer = 0f;

        if (currentState != State.Engage)
            EnterChase();
    }

    // -- PLAYER DEATH --
    protected override void HandlePlayerDeath()
    {
        if (squashCoroutine != null)
        {
            StopCoroutine(squashCoroutine);
            squashCoroutine = null;
        }
        base.HandlePlayerDeath();
    }
}