// Mushroom Mage's behaviour - kites at range, uses a default wand attack,
// and randomly weaves in Heal / Summon Minions / Magic Burst specials.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TopDown.Movement;

public class MageAI : EnemyAIBase
{
    private enum SpecialType { None, Heal, Summon, Burst }
    private enum EngagePhase { WandWindup, WandAttack, SpecialActive, Retreat, Recover }

    private EngagePhase engagePhase;
    private SpecialType activeSpecial;

    private EnemyShooter shooter;
    private WeaponAimer weaponAimer;
    private Animator animator;
    
    private static readonly int IsCastingHash = Animator.StringToHash("IsCasting");

    // ==================== KITING ====================
    [Header("Kiting")]
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float minKeepDistance = 3.5f;
    [SerializeField] private float retreatWallCheckDistance = 0.4f;

    [Header("Post-Attack Spacing")]
    [SerializeField] private float retreatStepDuration = 0.35f;
    private float retreatTimer;

    // ==================== WAND ATTACK (default) ====================
    [Header("Wand Attack")]
    [SerializeField] private float windupDurationMin = 0.4f;
    [SerializeField] private float windupDurationMax = 0.8f;

    private float windupTimer;

    // ==================== SPECIAL SELECTION ====================
    [Header("Special Attack Weights")]
    [SerializeField] private float summonWeight = 1f;
    [SerializeField] private float burstWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float specialChance = 0.35f;

    [Header("Special Cooldowns")]
    [SerializeField] private float summonCooldown = 10f;
    [SerializeField] private float burstCooldown = 8f;
    private float nextSummonTime = 0f;
    private float nextBurstTime = 0f;

    [Header("Special Failsafe")]
    [SerializeField] private float specialActionTimeout = 5f;
    private float specialFailsafeTimer;

    // ==================== HEAL ====================
    [Header("Heal")]
    [SerializeField] private float healDelayAfterDamage = 4f;
    [SerializeField] private float healCooldown = 12f;
    [SerializeField] private float healAmount = 15f;
    [SerializeField] private string healTrigger = "Heal";
    [SerializeField] private AudioClip healWindupClip;
    [Range(0f, 1f)] [SerializeField] private float healWindupVolume;
    [SerializeField] private AudioClip healActionClip;
    [Range(0f, 1f)] [SerializeField] private float healActionVolume;
    private float lastDamageTime = -999f;
    private float nextHealTime = 0f;

    // ==================== SUMMON MINIONS ====================
    [Header("Summon Minions")]
    [SerializeField] private GameObject[] minionPrefabs;
    [SerializeField] private int minionCountMin = 2;
    [SerializeField] private int minionCountMax = 4;
    [SerializeField] private float minionSpawnRadius = 2f;
    [SerializeField] private float minionSpawnStaggerMin = 0.15f;
    [SerializeField] private float minionSpawnStaggerMax = 0.4f;
    [SerializeField] private float minionSpawnClearance = 0.3f;
    [SerializeField] private string summonTrigger = "Summon";
    [SerializeField] private AudioClip summonWindupClip;
    [Range(0f, 1f)] [SerializeField] private float summonWindupVolume;
    [SerializeField] private AudioClip summonActionClip;
    [Range(0f, 1f)] [SerializeField] private float summonActionVolume;

    // ==================== MAGIC BURST ====================
    [Header("Magic Burst")]
    [SerializeField] private WeaponData magicBurstWeapon;
    [SerializeField] private string burstTrigger = "Burst";
    [SerializeField] private AudioClip burstWindupClip;
    [Range(0f, 1f)] [SerializeField] private float burstWindupVolume;
    [SerializeField] private AudioClip burstActionClip;
    [Range(0f, 1f)] [SerializeField] private float burstActionVolume;
    private WeaponData wandWeapon;

    // ==================== HIT REACTION ====================
    [Header("Hit Reaction")]
    [SerializeField] private float hitSlowMultiplier = 0.6f;
    [SerializeField] private float hitSlowDuration = 0.15f;
    private Coroutine hitSlowCoroutine;

    // -- AWAKE --
    protected override void Awake()
    {
        base.Awake();
        shooter = GetComponent<EnemyShooter>();
        weaponAimer = GetComponentInChildren<WeaponAimer>();
        animator = GetComponentInChildren<Animator>();
    }

    // -- START --
    protected override void Start()
    {
        base.Start();
        wandWeapon = shooter != null ? shooter.currentWeapon : null;
    }

    // -- AIM AT PLAYER --
    private void AimAtPlayer()
    {
        Vector2 dir = DirectionToPlayer();
        weaponAimer?.SetAimDirection(dir);
        mover.SetFacingOverride(dir);
    }

    // -- WALL-AWARE DIRECTION CHECK --
    private bool CanMoveDirection(Vector2 direction, float checkDistance)
    {
        if (direction.sqrMagnitude < 0.0001f) return false;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, checkDistance, wallMask);
        return hit.collider == null;
    }

    // ==================== CHASE / KITING ====================
    protected override void HandleChase(float distance)
    {
        mover.ClearFacingOverride();

        if (ForceChaseOnly)
        {
            Vector2 chaseDir = pathing != null ? pathing.GetDirectionToTarget(player.position) : DirectionToPlayer();
            mover.Move(chaseDir);
            return;
        }

        bool hasLos = HasLineOfSight();

        if (distance < minKeepDistance)
        {
            Vector2 away = ((Vector2)transform.position - (Vector2)player.position).normalized;
            if (CanMoveDirection(away, retreatWallCheckDistance))
                mover.Move(away);
            else
                mover.Stop();
            AimAtPlayer();
        }
        else if (distance > attackRange || !hasLos)
        {
            Vector2 approach = pathing != null ? pathing.GetDirectionToTarget(player.position) : DirectionToPlayer();
            mover.Move(approach);
        }
        else
        {
            mover.Stop();
            AimAtPlayer();

            if (shooter == null || shooter.CanFire())
                EnterEngage();
            return;
        }

        CheckLeash(distance);
    }

    protected override bool ShouldEngage(float distance) => false;

    // ==================== ENGAGE ====================
    protected override void EnterEngage()
    {
        currentState = State.Engage;
        DecideAction();
    }

    protected override void HandleEngage(float distance)
    {
        switch (engagePhase)
        {
            case EngagePhase.WandWindup:    HandleWandWindup(distance);  break;
            case EngagePhase.WandAttack:    HandleWandAttack(distance);  break;
            case EngagePhase.SpecialActive: HandleSpecialActive();       break;
            case EngagePhase.Retreat:       HandleRetreatStep(distance); break;
            case EngagePhase.Recover:       HandleRecover();             break;
        }
    }

    // -- DECIDE WHICH ACTION TO TAKE --
    private void DecideAction()
    {
        bool canHeal = health != null
            && health.GetCurrentHealth() < health.GetMaxHealth()
            && Time.time - lastDamageTime >= healDelayAfterDamage
            && Time.time >= nextHealTime;

        if (canHeal)
        {
            StartSpecial(SpecialType.Heal);
            return;
        }

        bool canSummon = minionPrefabs != null && minionPrefabs.Length > 0 && Time.time >= nextSummonTime;
        bool canBurst = magicBurstWeapon != null && Time.time >= nextBurstTime;

        if ((canSummon || canBurst) && Random.value < specialChance)
        {
            float totalWeight = (canSummon ? summonWeight : 0f) + (canBurst ? burstWeight : 0f);
            float roll = Random.value * totalWeight;

            if (canSummon && roll < summonWeight)
            {
                StartSpecial(SpecialType.Summon);
                return;
            }
            if (canBurst)
            {
                StartSpecial(SpecialType.Burst);
                return;
            }
        }

        // Default: wand attack
        StartWandAttack();
    }

    // ==================== WAND ATTACK ====================
    private void StartWandAttack()
    {
        activeSpecial = SpecialType.None;
        engagePhase = EngagePhase.WandWindup;
        windupTimer = Random.Range(windupDurationMin, windupDurationMax);
    }

    private void HandleWandWindup(float distance)
    {
        mover.Stop();
        AimAtPlayer();

        if (distance > attackRange * 1.2f || !HasLineOfSight())
        {
            EnterChase();
            return;
        }

        windupTimer -= Time.deltaTime;
        if (windupTimer <= 0f)
            engagePhase = EngagePhase.WandAttack;
    }

    private void HandleWandAttack(float distance)
    {
        AimAtPlayer();

        if (shooter != null && shooter.CanFire())
            shooter.TryShoot();

        if (shooter != null && shooter.IsBursting)
            return;

        EnterRecover();
    }

    // ==================== SPECIAL: SHARED FLOW ====================
    private void StartSpecial(SpecialType type)
    {
        activeSpecial = type;
        engagePhase = EngagePhase.SpecialActive;
        specialFailsafeTimer = specialActionTimeout;

        mover.Stop();
        AimAtPlayer();

        string trigger = null;
        AudioClip windupClip = null;
        float windupVolume = 0f;

        switch (type)
        {
            case SpecialType.Heal:
                trigger = healTrigger;
                windupClip = healWindupClip;
                windupVolume = healWindupVolume;
                break;
            case SpecialType.Summon:
                trigger = summonTrigger;
                windupClip = summonWindupClip;
                windupVolume = summonWindupVolume;
                break;
            case SpecialType.Burst:
                trigger = burstTrigger;
                windupClip = burstWindupClip;
                windupVolume = burstWindupVolume;
                break;
        }

        animator?.SetBool(IsCastingHash, true);
        if (trigger != null) animator?.SetTrigger(trigger);
        AudioManager.Instance?.PlaySFXWithPitch(windupClip, windupVolume, 0.1f);
    }

    private void HandleSpecialActive()
    {
        AimAtPlayer();

        specialFailsafeTimer -= Time.deltaTime;
        if (specialFailsafeTimer <= 0f)
        {
            if (debugLogging)
                Debug.LogWarning($"[{name}] {activeSpecial} special timed out before its Action Animation Event fired - forcing recovery.");
            EnterRecover();
        }
    }

    // -- ANIMATION EVENT CALLBACKS --
    public void OnHealAction()
    {
        AudioManager.Instance?.PlaySFXWithPitch(healActionClip, healActionVolume, 0.1f);
        health?.Heal(healAmount);
        nextHealTime = Time.time + healCooldown;
        EnterRecover();
    }

    public void OnSummonAction()
    {
        AudioManager.Instance?.PlaySFXWithPitch(summonActionClip, summonActionVolume, 0.1f);
        StartCoroutine(SpawnMinions());
    }

    public void OnBurstAction()
    {
        AudioManager.Instance?.PlaySFXWithPitch(burstActionClip, burstActionVolume, 0.1f);

        if (shooter != null && magicBurstWeapon != null)
        {
            float randomAngle = Random.Range(0f, 360f);
            weaponAimer?.SetAimDirection(RotateVector(Vector2.right, randomAngle));

            shooter.EquipWeapon(magicBurstWeapon, isPickup: true, playSound: false);
            shooter.Shoot();
            shooter.EquipWeapon(wandWeapon, isPickup: true, playSound: false);
        }

        nextBurstTime = Time.time + burstCooldown;
        EnterRecover();
    }

    // ==================== SUMMON MINIONS ====================
    private IEnumerator SpawnMinions()
    {
        int count = Random.Range(minionCountMin, minionCountMax + 1);

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
            Vector2 spawnPos = GetSafeSpawnPosition();

            EnemyManager.Instance?.SpawnEnemy(prefab, spawnPos);

            yield return new WaitForSeconds(Random.Range(minionSpawnStaggerMin, minionSpawnStaggerMax));
        }

        nextSummonTime = Time.time + summonCooldown;
        EnterRecover();
    }

    // -- SAFE SPAWN POSITION --
    private Vector2 GetSafeSpawnPosition()
    {
        const int maxAttempts = 8;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(minionSpawnRadius * 0.4f, minionSpawnRadius);
            Vector2 candidate = (Vector2)transform.position + offset;

            if (Physics2D.OverlapCircle(candidate, minionSpawnClearance, wallMask) == null)
                return candidate;
        }

        return transform.position;
    }

    // ==================== RECOVER / RETREAT ====================
    private void EnterRecover()
    {
        animator?.SetBool(IsCastingHash, false);

        if (player != null && Vector2.Distance(transform.position, player.position) < minKeepDistance)
        {
            engagePhase = EngagePhase.Retreat;
            retreatTimer = retreatStepDuration;
        }
        else
        {
            engagePhase = EngagePhase.Recover;
        }
    }

    private void HandleRecover()
    {
        EnterChase();
    }

    private void HandleRetreatStep(float distance)
    {
        AimAtPlayer();

        Vector2 away = ((Vector2)transform.position - (Vector2)player.position).normalized;
        if (CanMoveDirection(away, retreatWallCheckDistance))
            mover.Move(away);
        else
            mover.Stop();

        retreatTimer -= Time.deltaTime;

        if (retreatTimer <= 0f || distance >= minKeepDistance)
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

    // -- HELPERS --
    private static Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // -- TOOK DAMAGE --
    protected override void HandleTookDamage()
    {
        isAlerted = true;
        lastDamageTime = Time.time;

        if (currentState != State.Engage)
            EnterChase();

        if (hitSlowCoroutine != null) StopCoroutine(hitSlowCoroutine);
        hitSlowCoroutine = StartCoroutine(HitSlow());
    }

    private IEnumerator HitSlow()
    {
        mover.SetSpeedMultiplier(hitSlowMultiplier);
        yield return new WaitForSeconds(hitSlowDuration);
        mover.SetSpeedMultiplier(1f);
        hitSlowCoroutine = null;
    }

    // -- DEBUG --
    protected override string GetExtraDebugInfo()
    {
        return $" engagePhase={engagePhase} activeSpecial={activeSpecial} hp={(health != null ? health.GetCurrentHealth() : -1)}";
    }
}