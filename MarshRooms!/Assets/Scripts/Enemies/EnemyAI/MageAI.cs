// Mushroom Mage's behaviour - chases and fires the wand continuously,
// with an occasional chance to stop and cast Heal / Summon Minions / Magic Burst.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TopDown.Movement;

public class MageAI : EnemyAIBase
{
    private enum SpecialType { None, Heal, Summon, Burst }
    private enum EngagePhase { SpecialActive, Recover }

    private EngagePhase engagePhase;
    private SpecialType activeSpecial;

    private EnemyShooter shooter;
    private WeaponAimer weaponAimer;
    private Animator animator;

    [Header("Attack Range")]
    [SerializeField] private float attackRange = 6f;

    [Header("Special Attack Weights")]
    [Tooltip("Relative chance of picking Summon vs Burst when a special is due (Heal takes priority over both when it's available).")]
    [SerializeField] private float summonWeight = 1f;
    [SerializeField] private float burstWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float specialChance = 0.35f;
    [SerializeField] private float specialCheckInterval = 1.5f;
    private float nextSpecialCheckTime = 0f;

    [Header("Special Cooldowns")]
    [SerializeField] private float summonCooldown = 10f;
    [SerializeField] private float burstCooldown = 8f;
    private float nextSummonTime = 0f;
    private float nextBurstTime = 0f;

    [Header("Special Failsafe")]
    [SerializeField] private float specialActionTimeout = 5f;
    private float specialFailsafeTimer;

    [Header("Heal")]
    [SerializeField] private float healDelayAfterDamage = 4f;
    [SerializeField] private float healCooldown = 12f;
    [SerializeField] private float healAmount = 15f;
    [SerializeField, Range(0f, 1f)] private float healHpThreshold = 0.5f; // only eligible below this % of max HP
    [SerializeField] private string healTrigger = "Heal";
    [SerializeField] private AudioClip healWindupClip;
    [Range(0f, 1f)] [SerializeField] private float healWindupVolume;
    [SerializeField] private AudioClip healActionClip;
    [Range(0f, 1f)] [SerializeField] private float healActionVolume;
    private float lastDamageTime = -999f;
    private float nextHealTime = 0f;

    [Header("Summon Minions")]
    [SerializeField] private GameObject[] minionPrefabs;
    [SerializeField] private int minionCountMin = 2;
    [SerializeField] private int minionCountMax = 4;
    [SerializeField] private float minionSpawnRadius = 2f;
    [SerializeField] private float minionSpawnStaggerMin = 0.15f;
    [SerializeField] private float minionSpawnStaggerMax = 0.4f;
    [SerializeField] private string summonTrigger = "Summon";
    [SerializeField] private AudioClip summonWindupClip;
    [Range(0f, 1f)] [SerializeField] private float summonWindupVolume;
    [SerializeField] private AudioClip summonActionClip;
    [Range(0f, 1f)] [SerializeField] private float summonActionVolume;

    [Header("Magic Burst")]
    [SerializeField] private WeaponData magicBurstWeapon;
    [SerializeField] private string burstTrigger = "Burst";
    [SerializeField] private AudioClip burstWindupClip;
    [Range(0f, 1f)] [SerializeField] private float burstWindupVolume;
    [SerializeField] private AudioClip burstActionClip;
    [Range(0f, 1f)] [SerializeField] private float burstActionVolume;
    private WeaponData wandWeapon;

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

    // -- CHASE -- (keeps closing distance and firing every frame; only breaks off to cast a special)
    protected override void HandleChase(float distance)
    {
        mover.ClearFacingOverride();

        Vector2 moveDir = pathing != null ? pathing.GetDirectionToTarget(player.position) : DirectionToPlayer();
        mover.Move(moveDir);

        bool inRange = distance <= attackRange && HasLineOfSight();

        if (inRange)
        {
            AimAtPlayer();

            if (shooter != null && shooter.CanFire())
                shooter.TryShoot();

            if (Time.time >= nextSpecialCheckTime)
            {
                nextSpecialCheckTime = Time.time + specialCheckInterval;

                if (HasSpecialAvailable() && Random.value < specialChance)
                {
                    EnterEngage();
                    return;
                }
            }
        }

        CheckLeash(distance);
    }

    // -- SHOULD ENGAGE -- (unused: MageAI decides specials directly in HandleChase above)
    protected override bool ShouldEngage(float distance) => false;

    // -- ENTER ENGAGE --
    protected override void EnterEngage()
    {
        currentState = State.Engage;
        mover.Stop();
        DecideAction();
    }

    // -- HANDLE ENGAGE --
    protected override void HandleEngage(float distance)
    {
        switch (engagePhase)
        {
            case EngagePhase.SpecialActive: HandleSpecialActive(); break;
            case EngagePhase.Recover:       HandleRecover();       break;
        }
    }

    // -- HAS SPECIAL AVAILABLE --
    private bool HasSpecialAvailable()
    {
        bool canHeal = health != null
            && health.GetCurrentHealth() < health.GetMaxHealth() * healHpThreshold
            && Time.time - lastDamageTime >= healDelayAfterDamage
            && Time.time >= nextHealTime;

        bool canSummon = minionPrefabs != null && minionPrefabs.Length > 0 && Time.time >= nextSummonTime;
        bool canBurst = magicBurstWeapon != null && Time.time >= nextBurstTime;

        return canHeal || canSummon || canBurst;
    }

    // -- DECIDE WHICH SPECIAL TO CAST --
    private void DecideAction()
    {
        bool canHeal = health != null
            && health.GetCurrentHealth() < health.GetMaxHealth() * healHpThreshold
            && Time.time - lastDamageTime >= healDelayAfterDamage
            && Time.time >= nextHealTime;

        if (canHeal)
        {
            StartSpecial(SpecialType.Heal);
            return;
        }

        bool canSummon = minionPrefabs != null && minionPrefabs.Length > 0 && Time.time >= nextSummonTime;
        bool canBurst = magicBurstWeapon != null && Time.time >= nextBurstTime;

        float totalWeight = (canSummon ? summonWeight : 0f) + (canBurst ? burstWeight : 0f);
        float roll = totalWeight > 0f ? Random.value * totalWeight : 0f;

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

        // Shouldn't happen since HasSpecialAvailable() already checked, but bail safely.
        EnterChase();
    }

    // -- START SPECIAL -- (Animator drives Windup -> Action -> Idle; OnXAction fires via Animation Event)
    private void StartSpecial(SpecialType type)
    {
        activeSpecial = type;
        engagePhase = EngagePhase.SpecialActive;
        specialFailsafeTimer = specialActionTimeout;

        mover.Stop();
        AimAtPlayer();
        health?.SetSuppressHitAnimation(true);

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

        if (trigger != null) animator?.SetTrigger(trigger);
        AudioManager.Instance?.PlaySFXWithPitch(windupClip, windupVolume, 0.1f);
    }

    // -- HANDLE SPECIAL ACTIVE -- (specialFailsafeTimer forces recovery if the Action Animation Event never fires)
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

    // -- ON HEAL ACTION --
    public void OnHealAction()
    {
        AudioManager.Instance?.PlaySFXWithPitch(healActionClip, healActionVolume, 0.1f);
        health?.Heal(healAmount);
        nextHealTime = Time.time + healCooldown;
        EnterRecover();
    }

    // -- ON SUMMON ACTION --
    public void OnSummonAction()
    {
        AudioManager.Instance?.PlaySFXWithPitch(summonActionClip, summonActionVolume, 0.1f);
        StartCoroutine(SpawnMinions());
    }

    // -- ON BURST ACTION --
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

    // -- SPAWN MINIONS --
    private IEnumerator SpawnMinions()
    {
        int count = Random.Range(minionCountMin, minionCountMax + 1);

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
            Vector2 preferred = (Vector2)transform.position + Random.insideUnitCircle * minionSpawnRadius;
            Vector2 spawnPos = RoomManager.Current != null ? RoomManager.Current.GetSafeDropPosition(preferred) : preferred;

            EnemyManager.Instance?.SpawnEnemy(prefab, spawnPos);

            yield return new WaitForSeconds(Random.Range(minionSpawnStaggerMin, minionSpawnStaggerMax));
        }

        nextSummonTime = Time.time + summonCooldown;
        EnterRecover();
    }

    // -- ENTER RECOVER --
    private void EnterRecover()
    {
        health?.SetSuppressHitAnimation(false);
        engagePhase = EngagePhase.Recover;
    }

    // -- HANDLE RECOVER --
    private void HandleRecover()
    {
        EnterChase();
    }

    // -- RETURN --
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

    // -- ROTATE VECTOR --
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

    // -- HIT SLOW --
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