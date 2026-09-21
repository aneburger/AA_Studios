// Chef Puffs' brain.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChefAttack { Roll, Croissant, Knives }

[System.Serializable]
public class ChefPhaseSettings
{
    [Header("Attack Weights (0 = never picked)")]
    public float rollWeight = 1f;
    public float croissantWeight = 1f;
    public float knivesWeight = 1f;

    [Header("Timing")]
    public float downtimeMin = 1.5f;
    public float downtimeMax = 2.5f;

    [Header("Animation")]
    public float animSpeed = 1f;

    [Header("Counter Phase (placeholder timings)")]
    public float summonChargeTime = 1.2f;
    public float counterHoldTime = 3f;

    [Header("Roll")]
    public float rollChaseDuration = 3.5f;
    public float rollSpeedMax = 7f;
    public float finalRollPause = 0.7f;
    public float finalRollSpeed = 11f;
    public float dazeDuration = 3f;

    [Header("Croissant Cannon")]
    public int croissantVolleys = 1;
    public float croissantWindup = 0.7f;
    public int croissantShots = 0;
    public float croissantShotInterval = 0f;
    public float croissantVolleyPause = 0f;
    public int croissantBulletsPerShot = 0;
    public float croissantSpread = 10f;
    public float croissantBulletSpeedMultiplier = 1f;
    public float croissantAimTurnRate = 0f;
}

public class ChefPuffsBoss : BossBrain
{
    // Animator trigger names
    private const string TrigDespawn = "Despawn";
    private const string TrigSpawn = "Spawn";
    private const string TrigSummonCharge = "SummonCharge";
    private const string TrigSummon = "Summon";
    private const string TrigRollWindup = "RollWindup";
    private const string TrigStartRoll = "StartRoll";
    private const string TrigFallIntoDaze = "FallIntoDaze";
    private const string TrigGetUp = "GetUp";

    [Header("Director")]
    [Tooltip("Same in both phases. Phase 2 only turns up the intensity.")]
    [SerializeField] private int attacksBeforeCounterMin = 2;
    [SerializeField] private int attacksBeforeCounterMax = 3;

    [Header("Phase Settings")]
    [SerializeField] private ChefPhaseSettings phase1 = new ChefPhaseSettings();
    [SerializeField] private ChefPhaseSettings phase2 = new ChefPhaseSettings
    {
        downtimeMin = 0.8f,
        downtimeMax = 1.5f,
        animSpeed = 1.25f,
        summonChargeTime = 0.8f,
        rollSpeedMax = 9f,
        finalRollPause = 0.45f,
        finalRollSpeed = 13.5f,
        dazeDuration = 2.4f,
        croissantVolleys = 2,
        croissantWindup = 0.5f,
        croissantShots = 14,
        croissantShotInterval = 0.16f,
        croissantVolleyPause = 0.6f,
        croissantBulletSpeedMultiplier = 1.15f
    };

    [Header("Counter Phase")]
    [SerializeField] private Transform counterPoint;
    [SerializeField] private Transform arenaPoint;
    [SerializeField] private float summonEventTimeout = 3f;

    [Header("Roll: Chase")]
    [SerializeField] private float rollSpeedStart = 3f;
    [SerializeField] private float rollRampDuration = 1.2f;
    [SerializeField] private float rollTurnRate = 110f;
    [SerializeField] private float rollInaccuracyMin = 5f;
    [SerializeField] private float rollInaccuracyMax = 20f;
    [SerializeField] private float rollDirectionRefresh = 0.6f;
    [SerializeField] private float bounceSteerLock = 0.3f;

    [SerializeField] private float rollBrakeTime = 0.35f;
    [SerializeField] private float windupEventTimeout = 3f;

    [Header("Roll: Charge Vibrate")]
    [SerializeField] private float chargeShakeStart = 0.02f;
    [SerializeField] private float chargeShakeEnd = 0.08f;
    [SerializeField] private float chargeShakeInterval = 0.03f;

    [Header("Roll: Final + Impact")]
    [SerializeField] private float finalRollRamp = 0.2f;
    [SerializeField] private float finalRollTimeout = 2.5f;
    [SerializeField] private float impactFallbackSpeed = 4f;
    [SerializeField] private float impactFallbackTime = 0.35f;
    [SerializeField] private float impactShake = 0.7f;
    [SerializeField] private float bumpShake = 0.15f;

    [Header("Contact Damage")]
    [SerializeField] private float idleContactDamage = 2f;
    [SerializeField] private float idleContactKnockback = 5f;
    [SerializeField] private float rollContactDamage = 4f;
    [SerializeField] private float rollContactKnockback = 10f;

    [Header("Roll Audio")]
    [SerializeField] private AudioClip windupClip;
    [Range(0f, 1f)] [SerializeField] private float windupVolume = 1f;
    [SerializeField] private AudioClip rollLoopClip;
    [Range(0f, 1f)] [SerializeField] private float rollLoopVolume = 0.7f;
    [SerializeField] private AudioClip bumpClip;
    [Range(0f, 1f)] [SerializeField] private float bumpVolume = 0.8f;
    [SerializeField] private AudioClip impactClip;
    [Range(0f, 1f)] [SerializeField] private float impactVolume = 1f;
    [SerializeField] private AudioClip getUpClip;
    [Range(0f, 1f)] [SerializeField] private float getUpVolume = 0.8f;

    [Header("Croissant Cannon")]
    [SerializeField] private WeaponData croissantCannon;
    [SerializeField] private Transform[] shootSpots;
    [SerializeField] private float shootSpotMinPlayerDistance = 2.5f;
    [SerializeField] private float shootSpotMinMoveDistance = 1.5f;
    [SerializeField] private float croissantWeaponShowDelay = 0.2f;
    [SerializeField] private AudioClip croissantWindupClip;
    [Range(0f, 1f)] [SerializeField] private float croissantWindupVolume = 1f;
    [SerializeField] private int croissantWindupPulses = 2;

    [Header("Stubs (Knives until the next step)")]
    [SerializeField] private float stubAttackDuration = 1.5f;

    private ChefPhaseSettings CurrentSettings => phase <= 1 ? phase1 : phase2;

    private ChefAttack? lastAttack;
    private ChefAttack nextAttack;
    private string currentActionName = "-";
    private int attacksBeforeCounter;
    private int attacksSinceCounter;
    private bool summonActionFired;

    private Rigidbody2D body;
    private BossRollMover rollMover;
    private BossContactDamage contact;
    private EnemyShooter shooter;
    private WeaponAimer weaponAimer;

    // Croissant state
    private Vector2 currentAim = Vector2.right;
    private int lastShootSpot = -1;

    // Roll state
    private bool rollWindupEnded;
    private bool finalRollActive;
    private bool finalRollHit;
    private float steerLockUntil;
    private Vector2 lastBumpNormal = Vector2.up;
    private Vector2 lockedRollDirection;
    private AudioSource rollLoopSource;

    // -- AWAKE --
    protected override void Awake()
    {
        base.Awake();
        body = GetComponent<Rigidbody2D>();
        rollMover = GetComponent<BossRollMover>();
        contact = GetComponent<BossContactDamage>();
        shooter = GetComponent<EnemyShooter>();
        weaponAimer = GetComponentInChildren<WeaponAimer>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (relay != null)
        {
            relay.SummonAction += HandleSummonAction;
            relay.RollWindupEnded += HandleRollWindupEnded;
        }

        if (rollMover != null) rollMover.Bumped += HandleBumped;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (relay != null)
        {
            relay.SummonAction -= HandleSummonAction;
            relay.RollWindupEnded -= HandleRollWindupEnded;
        }

        if (rollMover != null) rollMover.Bumped -= HandleBumped;
        StopRollLoopSound();
    }

    private void HandleSummonAction()
    {
        summonActionFired = true;
        Log("Animation event: OnSummonAction");
    }

    private void HandleRollWindupEnded()
    {
        rollWindupEnded = true;
    }

    // Called by the mover on every bump against the bump mask
    private void HandleBumped(Vector2 normal, Vector2 incoming)
    {
        lastBumpNormal = normal;

        if (finalRollActive)
        {
            finalRollHit = true;
            return;
        }

        // Chase roll: a proper bounce
        steerLockUntil = Time.time + bounceSteerLock;
        AudioManager.Instance?.PlaySFXWithPitch(bumpClip, bumpVolume, 0.15f);
        ScreenEffects.Instance?.ShakeScreen(bumpShake);
        VFXManager.Instance?.SpawnWalkDust(transform.position);
    }

    // -- PHASE --
    protected override void ApplyPhase(int newPhase)
    {
        SetAnimSpeed(CurrentSettings.animSpeed);
        Log($"Phase {newPhase} settings applied (animSpeed {CurrentSettings.animSpeed}).");
    }

    protected override void OnFightCancelled()
    {
        currentActionName = "Cancelled";
        facePlayer = true;
        finalRollActive = false;

        StopRollLoopSound();
        if (rollMover != null) rollMover.StopRolling();
        if (health != null) health.SetInvulnerable(BossHealth.ReasonCharging, false);
        if (health != null) health.SetInvulnerable(BossHealth.ReasonHidden, false);

        if (shooter != null)
        {
            shooter.HideWeapon(true);
            ClearCroissantSettings();
        }

        DisableContact();
    }

    // ==================== DIRECTOR ====================
    protected override IEnumerator FightLoop()
    {
        yield return new WaitForSeconds(openingDelay);

        SetIdleContact();
        attacksBeforeCounter = RollAttacksBeforeCounter();
        attacksSinceCounter = 0;
        lastAttack = null;
        nextAttack = PickNextAttack();

        while (true)
        {
            // A queued phase transition always goes through a counter phase first
            if (transitionQueued || attacksSinceCounter >= attacksBeforeCounter)
            {
                yield return CounterPhase();

                attacksSinceCounter = 0;
                attacksBeforeCounter = RollAttacksBeforeCounter();

                if (transitionQueued)
                    yield return PlayPhaseTransition();

                nextAttack = PickNextAttack();
                yield return Downtime();
                continue;
            }

            ChefAttack attack = nextAttack;
            yield return RunAttack(attack);

            lastAttack = attack;
            attacksSinceCounter++;
            nextAttack = PickNextAttack();

            yield return Downtime();
        }
    }

    private int RollAttacksBeforeCounter()
    {
        int min = Mathf.Max(1, attacksBeforeCounterMin);
        int max = Mathf.Max(min, attacksBeforeCounterMax);
        return Random.Range(min, max + 1);
    }

    // Weighted pick that never repeats the previous attack
    private ChefAttack PickNextAttack()
    {
        ChefPhaseSettings s = CurrentSettings;
        float[] weights = { s.rollWeight, s.croissantWeight, s.knivesWeight };

        if (lastAttack.HasValue) weights[(int)lastAttack.Value] = 0f;

        float total = weights[0] + weights[1] + weights[2];
        if (total <= 0f)
        {
            // Only the previous attack has any weight, so allow it rather than stalling
            weights = new[] { s.rollWeight, s.croissantWeight, s.knivesWeight };
            total = weights[0] + weights[1] + weights[2];
            if (total <= 0f) return ChefAttack.Roll;
        }

        float pick = Random.value * total;
        int fallback = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] <= 0f) continue;

            fallback = i;
            if (pick < weights[i]) return (ChefAttack)i;
            pick -= weights[i];
        }

        return (ChefAttack)fallback;
    }

    private IEnumerator Downtime()
    {
        currentActionName = "Downtime";
        health.SetFlinchEnabled(true);
        facePlayer = true;
        SetIdleContact();

        float wait = Random.Range(CurrentSettings.downtimeMin, CurrentSettings.downtimeMax);
        float t = 0f;

        // A queued transition skips the rest of the downtime
        while (t < wait && !transitionQueued)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator RunAttack(ChefAttack attack)
    {
        currentActionName = attack.ToString();
        health.SetFlinchEnabled(false);
        Log($"Attack: {attack}");

        switch (attack)
        {
            case ChefAttack.Roll: yield return RollAttack(); break;
            case ChefAttack.Croissant: yield return CroissantAttack(); break;
            case ChefAttack.Knives: yield return KnivesAttack(); break;
        }
    }

    // ==================== ROLL ====================
    private IEnumerator RollAttack()
    {
        if (rollMover == null || player == null)
        {
            Debug.LogWarning($"[{name}] Roll needs a BossRollMover on the boss root (and a player in the scene). Skipping.", this);
            yield return new WaitForSeconds(stubAttackDuration);
            yield break;
        }

        ChefPhaseSettings s = CurrentSettings;
        ResetAnimTriggers();   // no stale roll / daze triggers from earlier
        SetIdleContact();

        // 1. Windup, facing Marsh
        facePlayer = true;
        yield return PlayRollWindup();

        // 2. Bouncing chase roll
        yield return ChaseRoll(s);

        // 3. Brake, then the windup pose again. The direction locks at the end of the pause.
        yield return BrakeAndPause(s);

        // 4. Final roll: a straight line until he hits something
        yield return FinalRoll(s);

        // 5. Impact, fall back, daze, get up
        yield return ImpactAndDaze(s);

        facePlayer = true;
    }

    // Plays RollWindup and waits for the OnRollWindupEnd animation event. The animation then
    // holds its last frame until StartRoll is sent.
    private IEnumerator PlayRollWindup()
    {
        rollWindupEnded = false;
        Trigger(TrigRollWindup);
        AudioManager.Instance?.PlaySFX(windupClip, windupVolume);

        float t = 0f;
        while (!rollWindupEnded && t < windupEventTimeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (!rollWindupEnded)
            Debug.LogWarning($"[{name}] OnRollWindupEnd never fired. Add the animation event to the last frame of puffs-roll-windup.", this);
    }

    private IEnumerator ChaseRoll(ChefPhaseSettings s)
    {
        facePlayer = false;
        SetRollContact();

        Vector2 targetDir = DirToPlayer();
        rollMover.StartRolling(targetDir, rollSpeedStart);
        Trigger(TrigStartRoll);
        StartRollLoopSound();

        float elapsed = 0f;
        float refreshTimer = 0f;
        float stuckTimer = 0f;
        steerLockUntil = 0f;

        while (elapsed < s.rollChaseDuration)
        {
            elapsed += Time.deltaTime;
            refreshTimer -= Time.deltaTime;

            rollMover.Speed = Mathf.Lerp(rollSpeedStart, s.rollSpeedMax, Mathf.Clamp01(elapsed / rollRampDuration));

            if (rollLoopSource != null)
                rollLoopSource.pitch = Mathf.Lerp(0.9f, 1.3f, Mathf.Clamp01(rollMover.Speed / Mathf.Max(0.01f, s.rollSpeedMax)));

            // Steer toward Marsh with some inaccuracy and a limited turn rate. Locked briefly after a bounce.
            if (Time.time >= steerLockUntil)
            {
                if (refreshTimer <= 0f)
                {
                    float error = Random.Range(rollInaccuracyMin, rollInaccuracyMax) * (Random.value < 0.5f ? -1f : 1f);
                    targetDir = Rotate(DirToPlayer(), error);
                    refreshTimer = rollDirectionRefresh;
                }

                Vector2 current = rollMover.Direction;
                float angle = Vector2.SignedAngle(current, targetDir);
                float maxStep = rollTurnRate * Time.deltaTime;
                rollMover.SetDirection(Rotate(current, Mathf.Clamp(angle, -maxStep, maxStep)));
            }

            // Pinned in a corner or against the counter: head back toward the arena
            if (rollMover.ActualSpeed < rollMover.Speed * 0.25f) stuckTimer += Time.deltaTime;
            else stuckTimer = 0f;

            if (stuckTimer > 0.4f)
            {
                Vector2 escape = arenaPoint != null
                    ? ((Vector2)arenaPoint.position - (Vector2)transform.position).normalized
                    : Random.insideUnitCircle.normalized;

                rollMover.SetDirection(escape);
                steerLockUntil = Time.time + 0.5f;
                stuckTimer = 0f;
            }

            yield return null;
        }
    }

    private IEnumerator BrakeAndPause(ChefPhaseSettings s)
    {
        // Slow down while still in the roll animation
        float startSpeed = rollMover.Speed;
        float t = 0f;
        while (t < rollBrakeTime)
        {
            t += Time.deltaTime;
            rollMover.Speed = Mathf.Lerp(startSpeed, 0f, Mathf.Clamp01(t / rollBrakeTime));
            yield return null;
        }

        StopRollLoopSound();
        rollMover.StopRolling();
        SetIdleContact();
        facePlayer = true;

        // Untouchable while he charges the final roll (the deflect sound tells the player)
        health.SetInvulnerable(BossHealth.ReasonCharging, true);

        // The windup pose again, held for the pause while he vibrates. Marsh gets a moment to
        // reposition before the direction locks.
        yield return PlayRollWindup();
        yield return VibrateVisuals(s.finalRollPause, chargeShakeStart, chargeShakeEnd, chargeShakeInterval);

        lockedRollDirection = DirToPlayer();
    }

    private IEnumerator FinalRoll(ChefPhaseSettings s)
    {
        health.SetInvulnerable(BossHealth.ReasonCharging, false);
        facePlayer = false;
        SetRollContact();
        finalRollHit = false;
        finalRollActive = true;

        float startSpeed = s.finalRollSpeed * 0.6f;
        rollMover.StartRolling(lockedRollDirection, startSpeed, stopOnBump: true);
        Trigger(TrigStartRoll);
        StartRollLoopSound();
        if (rollLoopSource != null) rollLoopSource.pitch = 1.35f;

        float t = 0f;
        while (!finalRollHit && t < finalRollTimeout)
        {
            t += Time.deltaTime;
            rollMover.Speed = Mathf.Lerp(startSpeed, s.finalRollSpeed, Mathf.Clamp01(t / finalRollRamp));
            yield return null;
        }

        finalRollActive = false;

        if (!finalRollHit)
            Log("  Final roll hit nothing before the timeout, dazing anyway.");
    }

    private IEnumerator ImpactAndDaze(ChefPhaseSettings s)
    {
        StopRollLoopSound();
        DisableContact();   // he's helpless now, so it's safe to walk up to him

        Vector2 away = finalRollHit ? lastBumpNormal : -rollMover.Direction;

        AudioManager.Instance?.PlaySFX(impactClip, impactVolume);
        ScreenEffects.Instance?.ShakeScreen(impactShake);
        VFXManager.Instance?.SpawnDodgeDust(transform.position);

        Trigger(TrigFallIntoDaze);
        yield return rollMover.Slide(away * impactFallbackSpeed, impactFallbackTime);

        // The rest of the daze: the dazed animation loops until GetUp
        yield return new WaitForSeconds(Mathf.Max(0f, s.dazeDuration - impactFallbackTime));

        Trigger(TrigGetUp);
        AudioManager.Instance?.PlaySFX(getUpClip, getUpVolume);
        yield return WaitForReturnToIdle(6f);
    }

    // ==================== ROLL HELPERS ====================
    private Vector2 DirToPlayer()
    {
        if (player == null) return Vector2.down;

        Vector2 d = (Vector2)player.position - (Vector2)transform.position;
        return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.down;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void StartRollLoopSound()
    {
        AudioManager.Instance?.PlayLoopingSFX(ref rollLoopSource, rollLoopClip, rollLoopVolume);
    }

    private void StopRollLoopSound()
    {
        AudioManager.Instance?.StopLoopingSFX(ref rollLoopSource);
    }

    private void SetIdleContact()
    {
        if (contact != null) contact.SetContact(idleContactDamage, idleContactKnockback);
    }

    private void SetRollContact()
    {
        if (contact != null) contact.SetContact(rollContactDamage, rollContactKnockback);
    }

    private void DisableContact()
    {
        if (contact != null) contact.SetContact(0f, 0f);
    }

    // ==================== CROISSANT CANNON ====================
    // Vanish -> reappear on a different spot with the cannon in hand -> windup (squish + sound)
    // -> a stream of shots aimed at Marsh -> repeat per volley -> put the cannon away.
    private IEnumerator CroissantAttack()
    {
        if (shooter == null || croissantCannon == null)
        {
            Debug.LogWarning($"[{name}] Croissant needs an EnemyShooter on the boss root and a Croissant Cannon WeaponData assigned. Skipping.", this);
            yield return new WaitForSeconds(stubAttackDuration);
            yield break;
        }

        ChefPhaseSettings s = CurrentSettings;

        // Equip while hidden, so the cannon only appears together with him
        shooter.EquipWeapon(croissantCannon, isPickup: true, playSound: false);
        shooter.HideWeapon(true);
        ApplyCroissantSettings(s);

        // 1. Vanish and reappear somewhere else, cannon in hand
        yield return RepositionWithCannon();

        // 2. Windup + stream, once per volley
        int volleys = Mathf.Max(1, s.croissantVolleys);
        for (int v = 0; v < volleys; v++)
        {
            yield return CroissantWindup(s);
            yield return CroissantStream(s);

            if (v < volleys - 1)
                yield return HoldAim(VolleyPause(s), s);
        }

        // 3. Put the cannon away
        shooter.SquishEffect();
        yield return new WaitForSeconds(0.15f);
        shooter.HideWeapon(true);
        ClearCroissantSettings();
    }

    private void ApplyCroissantSettings(ChefPhaseSettings s)
    {
        shooter.SetBulletSpeedMultiplier(s.croissantBulletSpeedMultiplier);

        if (s.croissantBulletsPerShot > 0)
            shooter.SetBulletOverrides(s.croissantBulletsPerShot, s.croissantSpread);
        else
            shooter.ClearBulletOverrides();
    }

    private void ClearCroissantSettings()
    {
        shooter.SetBulletSpeedMultiplier(1f);
        shooter.ClearBulletOverrides();
    }

    // Spawn protection while he vanishes and reappears, so he can't be hit mid-teleport
    private IEnumerator RepositionWithCannon()
    {
        health.SetInvulnerable(BossHealth.ReasonHidden, true);
        DisableContact();
        facePlayer = true;

        yield return WaitUntilIdle();
        Trigger(TrigDespawn);
        yield return WaitForStateFinished();

        TeleportTo(PickShootSpot());

        // Point the cannon at Marsh before it appears so it doesn't swing round
        currentAim = AimTarget();
        if (weaponAimer != null) weaponAimer.SetAimDirection(currentAim);

        Trigger(TrigSpawn);
        yield return new WaitForSeconds(croissantWeaponShowDelay);
        shooter.HideWeapon(false);
        yield return WaitForReturnToIdle();

        SetIdleContact();
        health.SetInvulnerable(BossHealth.ReasonHidden, false);
    }

    // Picks a random spot that isn't the one used last time, isn't where he's standing,
    // and (when possible) isn't right next to Marsh.
    private Transform PickShootSpot()
    {
        if (shootSpots == null || shootSpots.Length == 0)
        {
            Log("  (no shoot spots assigned, reappearing in place)");
            return null;
        }

        if (shootSpots.Length < 2)
            Debug.LogWarning($"[{name}] Only {shootSpots.Length} shoot spot assigned. He needs at least 2 to never repeat.", this);

        bool canAvoidRepeat = shootSpots.Length > 1;
        List<int> allowed = new List<int>();

        for (int i = 0; i < shootSpots.Length; i++)
        {
            if (shootSpots[i] == null) continue;
            if (canAvoidRepeat && i == lastShootSpot) continue;
            if (canAvoidRepeat && Vector2.Distance(shootSpots[i].position, transform.position) < shootSpotMinMoveDistance) continue;
            allowed.Add(i);
        }

        // The distance rule ruled everything out: any spot except the last one will do
        if (allowed.Count == 0)
        {
            for (int i = 0; i < shootSpots.Length; i++)
            {
                if (shootSpots[i] == null) continue;
                if (canAvoidRepeat && i == lastShootSpot) continue;
                allowed.Add(i);
            }

            if (allowed.Count == 0) return null;
        }

        List<int> preferred = allowed.FindAll(i =>
            player == null || Vector2.Distance(shootSpots[i].position, player.position) >= shootSpotMinPlayerDistance);

        List<int> pool = preferred.Count > 0 ? preferred : allowed;
        lastShootSpot = pool[Random.Range(0, pool.Count)];
        return shootSpots[lastShootSpot];
    }

    // Same windup as the button mushrooms: a sound, and the weapon squishing a couple of times,
    // while the cannon keeps pointing at Marsh.
    private IEnumerator CroissantWindup(ChefPhaseSettings s)
    {
        float duration = Mathf.Max(0.05f, s.croissantWindup);
        AudioManager.Instance?.PlaySFXWithPitch(croissantWindupClip, croissantWindupVolume, 0.1f);

        int pulses = Mathf.Max(1, croissantWindupPulses);
        float pulseInterval = duration / pulses;
        float nextPulse = 0f;
        int pulsesDone = 0;

        float t = 0f;
        while (t < duration)
        {
            TrackAim(s.croissantAimTurnRate);

            if (pulsesDone < pulses && t >= nextPulse)
            {
                shooter.SquishEffect();
                pulsesDone++;
                nextPulse += pulseInterval;
            }

            t += Time.deltaTime;
            yield return null;
        }
    }

    // The stream: shots fired one after another while the aim keeps following Marsh
    private IEnumerator CroissantStream(ChefPhaseSettings s)
    {
        int shots = s.croissantShots > 0 ? s.croissantShots : Mathf.Max(1, croissantCannon.burstCount);
        float interval = s.croissantShotInterval > 0f ? s.croissantShotInterval : croissantCannon.burstInterval;

        int fired = 0;
        float nextShot = Time.time;

        while (fired < shots)
        {
            TrackAim(s.croissantAimTurnRate);

            if (Time.time >= nextShot)
            {
                shooter.Shoot();
                fired++;
                nextShot = Time.time + interval;
            }

            yield return null;
        }
    }

    private float VolleyPause(ChefPhaseSettings s)
    {
        return s.croissantVolleyPause > 0f ? s.croissantVolleyPause : croissantCannon.fireRate;
    }

    private IEnumerator HoldAim(float duration, ChefPhaseSettings s)
    {
        float t = 0f;
        while (t < duration)
        {
            TrackAim(s.croissantAimTurnRate);
            t += Time.deltaTime;
            yield return null;
        }
    }

    // Direction from the cannon to Marsh
    private Vector2 AimTarget()
    {
        if (player == null) return Vector2.right;

        Vector2 origin = weaponAimer != null ? (Vector2)weaponAimer.transform.position : (Vector2)transform.position;
        Vector2 d = (Vector2)player.position - origin;
        return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.right;
    }

    // turnRate 0 = point straight at Marsh every frame (bullets fire along this direction instantly).
    // Above 0 the aim can only turn that fast, so the stream sweeps and lags behind a moving Marsh.
    private void TrackAim(float turnRate)
    {
        if (weaponAimer == null) return;

        Vector2 target = AimTarget();

        if (turnRate <= 0f || currentAim.sqrMagnitude < 0.001f)
        {
            currentAim = target;
        }
        else
        {
            float angle = Vector2.SignedAngle(currentAim, target);
            float step = turnRate * Time.deltaTime;
            currentAim = Rotate(currentAim, Mathf.Clamp(angle, -step, step));
        }

        weaponAimer.SetAimDirection(currentAim);
    }

    // ==================== STUB ATTACKS ====================

    private IEnumerator KnivesAttack()
    {
        Log("  [stub] Knife pattern (step 5)");
        yield return new WaitForSeconds(stubAttackDuration);
    }

    // ==================== COUNTER PHASE ====================
    // Vanish -> reappear behind the counter -> charge + summon -> hold -> vanish -> return.
    // Uses the real Despawn / Spawn / SummonCharge / Summon animations; the summon itself is a stub.
    private IEnumerator CounterPhase()
    {
        currentActionName = "Counter phase";
        Log("Counter phase: start.");

        health.SetFlinchEnabled(false);
        health.SetInvulnerable(BossHealth.ReasonHidden, true);
        DisableContact();
        facePlayer = true;

        // Vanish
        yield return WaitUntilIdle();
        Trigger(TrigDespawn);
        yield return WaitForStateFinished();

        // Reappear behind the counter
        TeleportTo(counterPoint);
        Trigger(TrigSpawn);
        yield return WaitForReturnToIdle();

        // Charge up, then summon on the animation event
        Trigger(TrigSummonCharge);
        yield return new WaitForSeconds(CurrentSettings.summonChargeTime);

        summonActionFired = false;
        Trigger(TrigSummon);
        yield return WaitForSummonAction();
        Log("  [stub] Would summon minions + spikes here (steps 6 and 7)");
        yield return WaitForReturnToIdle();

        // Placeholder for "wait while the player deals with the wave"
        yield return new WaitForSeconds(CurrentSettings.counterHoldTime);

        // Come back out
        yield return WaitUntilIdle();
        Trigger(TrigDespawn);
        yield return WaitForStateFinished();

        TeleportTo(arenaPoint);
        Trigger(TrigSpawn);
        yield return WaitForReturnToIdle();

        // If the phase transition is next, hand straight over from Hidden to Transition with no gap
        if (transitionQueued)
            health.SetInvulnerable(BossHealth.ReasonTransition, true);

        health.SetInvulnerable(BossHealth.ReasonHidden, false);
        Log("Counter phase: end.");
    }

    private IEnumerator WaitForSummonAction()
    {
        float t = 0f;
        while (!summonActionFired && t < summonEventTimeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (!summonActionFired)
            Debug.LogWarning($"[{name}] OnSummonAction never fired. Add the animation event to puffs-summon and make sure BossAnimationRelay is on the Animator's object.", this);
    }

    private void TeleportTo(Transform point)
    {
        if (point == null)
        {
            Log("  (no teleport point assigned, staying put)");
            return;
        }

        transform.position = point.position;
        if (body != null) body.position = point.position;
    }

    // ==================== DEBUG ====================
    protected override string DebugSummary()
    {
        return $"Phase {phase} | Now: {currentActionName} | Next: {nextAttack} | " +
               $"Attacks since counter: {attacksSinceCounter}/{attacksBeforeCounter}" +
               (transitionQueued ? " | TRANSITION QUEUED" : "");
    }
}