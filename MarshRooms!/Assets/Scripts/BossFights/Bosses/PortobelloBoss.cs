// Portobello's brain.
// Step 1: skeleton. Step 2: Gold Bar Gun. Step 3: Coin Spray Gun.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TopDown.Movement;

public enum PortobelloAttack
{
    GoldBarGun,
    CoinGun,
    DollarBursts,
    GoldSpikes,
    Burrow
}

[System.Serializable]
public class PortobelloPhaseSettings
{
    [Header("Timing")]
    public float downtimeMin = 1.5f;
    public float downtimeMax = 2.5f;

    [Header("Attack Weights (0 = never picked this phase)")]
    public float goldBarWeight = 1f;
    public float coinGunWeight = 1f;
    public float dollarBurstsWeight = 1f;
    public float spikeWeight = 1f;
    public float burrowWeight = 1f;

    [Header("Animation")]
    public float animSpeed = 1f;

    [Header("Gold Spikes")]
    public int spikeCountMin = 4;
    public int spikeCountMax = 6;
    public float spikeWarningTime = 0.6f;
    public int spikeRoundsMin = 1;
    public int spikeRoundsMax = 1;
    public float spikeRoundInterval = 0.5f;
    [Tooltip("0 = no extra shake (Phase 1/2). Set in Phase 3 for the more impactful version.")]
    public float spikeScreenShakeDuration = 0f;
    public float spikeScreenShakeForce = 0.5f;

    [Header("Burrow")]
    public int burrowPops = 1;
    public float burrowChaseSpeed = 4f;
    public float burrowChaseDuration = 2f;
    public float burrowStopPause = 0.4f;
    public float burrowSmashRadius = 2f;
    public float burrowSmashDamage = 3f;
    public float burrowSmashKnockback = 8f;
    public float burrowSmashShake = 0.7f;
    public float burrowPopCooldown = 1.5f;

    [Header("Throne Phase")]
    public int throneEnemyCountMin = 2;
    public int throneEnemyCountMax = 3;

    [Header("Gold Bar Gun")]
    public int goldBarVolleys = 1;
    public float goldBarWindup = 0.7f;
    public int goldBarShots = 10;
    public float goldBarShotInterval = 0.18f;
    public float goldBarVolleyPause = 0.5f;
    public float goldBarBulletSpeedMultiplier = 1f;
    public float goldBarAimTurnRate = 0f; // 0 = snap to player

    [Header("Coin Spray Gun")]
    public int coinVolleys = 1;
    public float coinVolleyPause = 0.5f;
    public int coinShots = 14;
    public float coinShotInterval = 0.12f;
    public float coinBulletSpeedMultiplier = 1f;
    public float coinAimTurnRate = 0f;
    public int coinBulletsPerShot = 1;
    public float coinSpread = 8f;
    public float coinMoveSpeed = 1.5f;

    [Header("Dollar Bursts")]
    public int dollarThrows = 3;
    public float dollarSummonPause = 0.5f;
    public float dollarSpeedMultiplier = 1f;
    public float dollarCountMultiplier = 1f;
    public float dollarHangMultiplier = 1f;
}

public class PortobelloBoss : BossBrain
{
    // Animator trigger names
    private const string TrigDespawn = "Despawn";
    private const string TrigSpawn = "Spawn";
    private const string TrigBillSummon = "bill-summon";
    private const string TrigSpikeWindup = "spike-windup";
    private const string TrigBurrowIn = "port-jump-in";
    private const string TrigBurrowOut = "port-jump-out";
    private const string TrigThroneSummon = "throne-summon";
    private const string TrigThroneJumpUp = "throne-jump-up";
    private const string TrigThroneLand = "throne-land";

    [Header("Phase Settings")]
    [SerializeField] private PortobelloPhaseSettings phase1 = new PortobelloPhaseSettings
    {
        coinGunWeight = 0f
    };
    [SerializeField] private PortobelloPhaseSettings phase2 = new PortobelloPhaseSettings
    {
        downtimeMin = 1.0f,
        downtimeMax = 1.8f,
        animSpeed = 1.15f,
        goldBarVolleys = 1,
        goldBarWindup = 0.55f,
        goldBarShots = 12,
        goldBarShotInterval = 0.15f
    };
    [SerializeField] private PortobelloPhaseSettings phase3 = new PortobelloPhaseSettings
    {
        downtimeMin = 0.7f,
        downtimeMax = 1.3f,
        animSpeed = 1.3f,
        goldBarVolleys = 2,
        goldBarWindup = 0.45f,
        goldBarShots = 14,
        goldBarShotInterval = 0.13f,
        goldBarVolleyPause = 0.4f,
        spikeScreenShakeDuration = 0.8f,
        spikeScreenShakeForce = 0.6f
    };

    protected override int LastPhase => 3;

    private PortobelloPhaseSettings CurrentSettings =>
        phase == 1 ? phase1 : phase == 2 ? phase2 : phase3;

    [Header("Contact Damage")]
    [SerializeField] private float idleContactDamage = 2f;
    [SerializeField] private float idleContactKnockback = 5f;

    [Header("Spawn / Despawn Audio")]
    [SerializeField] private AudioClip despawnClip;
    [Range(0f, 1f)] [SerializeField] private float despawnVolume = 1f;
    [SerializeField] private AudioClip spawnClip;
    [Range(0f, 1f)] [SerializeField] private float spawnVolume = 1f;

    [Header("Gold Bar Gun")]
    [SerializeField] private WeaponData goldBarGun;
    [SerializeField] private Transform[] shootSpots;
    [SerializeField] private float shootSpotMinPlayerDistance = 2.5f;
    [SerializeField] private float shootSpotMinMoveDistance = 1.5f;
    [SerializeField] private float weaponShowDelay = 0.2f;
    [SerializeField] private AudioClip gunWindupClip;
    [Range(0f, 1f)] [SerializeField] private float gunWindupVolume = 1f;
    [SerializeField] private int gunWindupPulses = 2;
    [SerializeField] private float goldBarShotShakeForce = 0.1f;

    [Header("Coin Spray Gun")]
    [SerializeField] private WeaponData coinSprayGun;
    [SerializeField] private float coinMinDistanceToPlayer = 1.5f;

    [Header("Dollar Bursts")]
    [SerializeField] private WeaponData dollarBillWeapon;
    [SerializeField] private KnifePatternData[] dollarPatterns;
    [SerializeField] private float dollarSummonEventTimeout = 3f;
    [SerializeField] private AudioClip billSummonClip;
    [Range(0f, 1f)] [SerializeField] private float billSummonVolume = 1f;
    [SerializeField] private AudioClip billLaunchClip;
    [Range(0f, 1f)] [SerializeField] private float billLaunchVolume = 1f;
    [Tooltip("Fires the launch sound this many seconds early, to compensate for audio latency.")]
    [SerializeField] private float billLaunchSoundLead = 0.05f;
    [SerializeField] private float dollarBurstShakeForce = 0.4f;

    [Header("Gold Spikes")]
    [SerializeField] private BossSpikeSpawner spikeSpawner;
    [SerializeField] private float spikeSummonEventTimeout = 3f;
    [SerializeField] private AudioClip spikeSummonClip;
    [Range(0f, 1f)] [SerializeField] private float spikeSummonVolume = 1f;

    [Header("Burrow")]
    [SerializeField] private float burrowImpactEventTimeout = 3f;
    [SerializeField] private AudioClip burrowDiveClip;
    [Range(0f, 1f)] [SerializeField] private float burrowDiveVolume = 1f;
    [SerializeField] private AudioClip burrowEmergeClip;
    [Range(0f, 1f)] [SerializeField] private float burrowEmergeVolume = 1f;
    [SerializeField] private AudioClip burrowCrashClip;
    [Range(0f, 1f)] [SerializeField] private float burrowCrashVolume = 1f;
    [SerializeField] private AudioClip burrowLoopClip;
    [Range(0f, 1f)] [SerializeField] private float burrowLoopVolume = 0.7f;

    [Header("Fallback")]
    [SerializeField] private float stubAttackDuration = 1.5f;

    [Header("Throne Phase Cadence")]
    [SerializeField] private int attacksBeforeThroneMin = 2;
    [SerializeField] private int attacksBeforeThroneMax = 3;
    [SerializeField] private Transform thronePoint;
    [SerializeField] private float throneSummonEventTimeout = 3f;
    [SerializeField] private AudioClip throneSummonClip;
    [Range(0f, 1f)] [SerializeField] private float throneSummonVolume = 1f;
    [SerializeField] private GameObject throneShadow;
    [SerializeField] private float throneJumpUpHeight = 15f;
    [SerializeField] private float throneJumpUpDuration = 0.5f;
    [SerializeField] private float throneShadowLeadTime = 0.6f;
    [SerializeField] private AudioClip throneJumpClip;
    [Range(0f, 1f)] [SerializeField] private float throneJumpVolume = 1f;
    [SerializeField] private AudioClip throneLandClip;
    [Range(0f, 1f)] [SerializeField] private float throneLandVolume = 1f;
    [SerializeField] private float throneLandShake = 0.6f;

    [Header("Throne Phase: Enemies")]
    [SerializeField] private BossMinionSpawner minionSpawner;
    [SerializeField] private GameObject[] minionPrefabs;
    [SerializeField] private Transform[] enemySpawnPoints;
    [Range(0f, 1f)] [SerializeField] private float weaponGuaranteeChance = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float healthGuaranteeChance = 0.4f;
    [SerializeField] private float enemySpawnIntervalMin = 0.15f;
    [SerializeField] private float enemySpawnIntervalMax = 0.4f;

    private BossContactDamage contact;
    private Rigidbody2D body;
    private EnemyShooter shooter;
    private WeaponAimer weaponAimer;
    private BossPatternShooter patternShooter;
    private SpriteRenderer[] spriteRenderers;

    private Vector2 currentAim = Vector2.right;
    private int lastShootSpot = -1;
    private int lastDollarPatternIndex = -1;
    private bool summonReadyFired;
    private bool burrowImpactFired;
    private PortobelloAttack? lastAttack;
    private AudioSource burrowLoopSource;
    private bool burrowLoopPausedByUs;

    private void Update()
    {
        if (burrowLoopSource == null) return;

        bool shouldPause = Time.timeScale <= 0f;

        if (shouldPause && !burrowLoopPausedByUs)
        {
            burrowLoopSource.Pause();
            burrowLoopPausedByUs = true;
        }
        else if (!shouldPause && burrowLoopPausedByUs)
        {
            burrowLoopSource.UnPause();
            burrowLoopPausedByUs = false;
        }
    }

    // -- AWAKE --
    protected override void Awake()
    {
        base.Awake();
        contact = GetComponent<BossContactDamage>();
        body = GetComponent<Rigidbody2D>();
        shooter = GetComponent<EnemyShooter>();
        weaponAimer = GetComponentInChildren<WeaponAimer>();
        patternShooter = GetComponent<BossPatternShooter>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    // -- ENABLE --
    protected override void OnEnable()
    {
        base.OnEnable();
        if (relay != null) relay.SummonReady += HandleSummonReady;
        if (relay != null) relay.BurrowImpact += HandleBurrowImpact;
        if (spikeSpawner != null) spikeSpawner.SpikeSpawned += RegisterHazard;
        if (minionSpawner != null) minionSpawner.MinionSpawned += RegisterMinion;
    }

    // -- DISABLE --
    protected override void OnDisable()
    {
        base.OnDisable();
        if (relay != null) relay.SummonReady -= HandleSummonReady;
        if (relay != null) relay.BurrowImpact -= HandleBurrowImpact;
        if (spikeSpawner != null) spikeSpawner.SpikeSpawned -= RegisterHazard;
        if (minionSpawner != null) minionSpawner.MinionSpawned -= RegisterMinion;
    }

    // Minions cleared by a phase transition
    protected override void OnMinionCleared(GameObject minion)
    {
        EnemyManager.Instance?.UnregisterEnemy(minion);
    }

    private void HandleSummonReady()
    {
        summonReadyFired = true;
    }

    private void HandleBurrowImpact()
    {
        burrowImpactFired = true;
        DoBurrowSmash(CurrentSettings);
    }

    protected override void OnAnimTrigger(string triggerName)
    {
        if (triggerName == TrigDespawn)
            AudioManager.Instance?.PlaySFXWithPitch(despawnClip, despawnVolume, 0.1f);
        else if (triggerName == TrigSpawn)
            AudioManager.Instance?.PlaySFXWithPitch(spawnClip, spawnVolume, 0.1f);
        else if (triggerName == TrigBillSummon)
            AudioManager.Instance?.PlaySFXWithPitch(billSummonClip, billSummonVolume, 0.1f);
        else if (triggerName == TrigSpikeWindup)
            AudioManager.Instance?.PlaySFXWithPitch(spikeSummonClip, spikeSummonVolume, 0.1f);
        else if (triggerName == TrigBurrowIn)
            AudioManager.Instance?.PlaySFXWithPitch(burrowDiveClip, burrowDiveVolume, 0.1f);
        else if (triggerName == TrigBurrowOut)
            AudioManager.Instance?.PlaySFXWithPitch(burrowEmergeClip, burrowEmergeVolume, 0.1f);
        else if (triggerName == TrigThroneJumpUp)
            AudioManager.Instance?.PlaySFXWithPitch(throneJumpClip, throneJumpVolume, 0.1f);
        else if (triggerName == TrigThroneLand)
            AudioManager.Instance?.PlaySFXWithPitch(throneLandClip, throneLandVolume, 0.1f);
        else if (triggerName == TrigThroneSummon)
            AudioManager.Instance?.PlaySFXWithPitch(throneSummonClip, throneSummonVolume, 0.1f);
    }

    // -- PHASE --
    protected override void ApplyPhase(int newPhase)
    {
        SetAnimSpeed(CurrentSettings.animSpeed);
        // Statue spawning on phase 2/3 entry hooks in here later.
    }

    protected override void OnFightCancelled()
    {
        if (shooter != null)
        {
            shooter.HideWeapon(true);
            ClearGunSettings();
        }
        health.SetInvulnerable(BossHealth.ReasonHidden, false);
        SetIdleContact();
        StopBurrowLoopSound();
    }

    // ==================== DIRECTOR ====================
    protected override IEnumerator FightLoop()
    {
        yield return new WaitForSeconds(openingDelay);
        SetIdleContact();

        minionSpawner?.ResetGuarantees();
        lastAttack = null;
        int attacksBeforeThrone = RollAttacksBeforeThrone();
        int attacksSinceThrone = 0;

        while (true)
        {
            if (attacksSinceThrone >= attacksBeforeThrone)
            {
                yield return ThroneAttack();

                attacksSinceThrone = 0;
                attacksBeforeThrone = RollAttacksBeforeThrone();
                yield return Downtime();
                continue;
            }

            PortobelloAttack attack = PickNextAttack();
            yield return RunAttack(attack);
            lastAttack = attack;
            attacksSinceThrone++;

            yield return Downtime();
        }
    }

    private int RollAttacksBeforeThrone()
    {
        int min = Mathf.Max(1, attacksBeforeThroneMin);
        int max = Mathf.Max(min, attacksBeforeThroneMax);
        return Random.Range(min, max + 1);
    }

    // Weighted pick that never repeats the previous attack.
    // Implemented so far: Gold Bar Gun, Coin Gun, Dollar Bursts, Gold Spikes.
    // Burrow joins this list once it's built.
    private PortobelloAttack PickNextAttack()
    {
        PortobelloPhaseSettings s = CurrentSettings;
        PortobelloAttack[] options = { PortobelloAttack.GoldBarGun, PortobelloAttack.CoinGun, PortobelloAttack.DollarBursts, PortobelloAttack.GoldSpikes, PortobelloAttack.Burrow };
        float[] weights = { s.goldBarWeight, s.coinGunWeight, s.dollarBurstsWeight, s.spikeWeight, s.burrowWeight };

        if (lastAttack.HasValue)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] == lastAttack.Value) weights[i] = 0f;
            }
        }

        float total = SumWeights(weights);
        if (total <= 0f)
        {
            // Only the previous attack has any weight this phase - allow it rather than stalling
            weights = new[] { s.goldBarWeight, s.coinGunWeight, s.dollarBurstsWeight, s.spikeWeight, s.burrowWeight };
            total = SumWeights(weights);
            if (total <= 0f) return PortobelloAttack.GoldBarGun;
        }

        float pick = Random.value * total;
        int fallback = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] <= 0f) continue;

            fallback = i;
            if (pick < weights[i]) return options[i];
            pick -= weights[i];
        }

        return options[fallback];
    }

    private static float SumWeights(float[] weights)
    {
        float total = 0f;
        foreach (float w in weights) total += w;
        return total;
    }

    private IEnumerator RunAttack(PortobelloAttack attack)
    {
        health.SetFlinchEnabled(false);

        switch (attack)
        {
            case PortobelloAttack.GoldBarGun: yield return GoldBarGunAttack(); break;
            case PortobelloAttack.CoinGun: yield return CoinSprayGunAttack(); break;
            case PortobelloAttack.DollarBursts: yield return DollarBurstsAttack(); break;
            case PortobelloAttack.GoldSpikes: yield return GoldSpikesAttack(); break;
            case PortobelloAttack.Burrow: yield return BurrowAttack(); break;
        }
    }

    private IEnumerator Downtime()
    {
        facePlayer = true;
        health.SetFlinchEnabled(true);
        SetIdleContact();

        float wait = Random.Range(CurrentSettings.downtimeMin, CurrentSettings.downtimeMax);
        yield return new WaitForSeconds(wait);
    }

    // ==================== GOLD BAR GUN ====================
    private IEnumerator GoldBarGunAttack()
    {
        if (shooter == null || goldBarGun == null || shootSpots == null || shootSpots.Length == 0)
        {
            yield return new WaitForSeconds(stubAttackDuration);
            yield break;
        }

        PortobelloPhaseSettings s = CurrentSettings;

        shooter.EquipWeapon(goldBarGun, isPickup: true, playSound: false);
        shooter.HideWeapon(true);
        ApplyGunSettings(s);

        yield return Reposition();

        int volleys = Mathf.Max(1, s.goldBarVolleys);
        for (int v = 0; v < volleys; v++)
        {
            yield return GunWindup(s.goldBarWindup);
            yield return GunStream(s);

            if (v < volleys - 1)
                yield return HoldAim(s.goldBarVolleyPause, s.goldBarAimTurnRate);
        }

        shooter.SquishEffect();
        yield return new WaitForSeconds(0.15f);
        shooter.HideWeapon(true);
        ClearGunSettings();
    }

    private void ApplyGunSettings(PortobelloPhaseSettings s)
    {
        shooter.SetBulletSpeedMultiplier(s.goldBarBulletSpeedMultiplier);
        shooter.ClearBulletOverrides();
    }

    private void ClearGunSettings()
    {
        shooter.SetBulletSpeedMultiplier(1f);
        shooter.ClearBulletOverrides();
    }

    // ==================== COIN SPRAY GUN ====================
    private IEnumerator CoinSprayGunAttack()
    {
        if (shooter == null || coinSprayGun == null)
        {
            yield return new WaitForSeconds(stubAttackDuration);
            yield break;
        }

        PortobelloPhaseSettings s = CurrentSettings;

        shooter.EquipWeapon(coinSprayGun, isPickup: true, playSound: false);
        shooter.HideWeapon(true);
        ApplyCoinSettings(s);

        yield return Reposition();

        int volleys = Mathf.Max(1, s.coinVolleys);
        for (int v = 0; v < volleys; v++)
        {
            yield return GunWindup(s.goldBarWindup);
            yield return CoinStream(s);

            if (v < volleys - 1)
                yield return HoldAim(s.coinVolleyPause, s.coinAimTurnRate);
        }

        shooter.SquishEffect();
        yield return new WaitForSeconds(0.15f);
        shooter.HideWeapon(true);
        ClearCoinSettings();
    }

    private void ApplyCoinSettings(PortobelloPhaseSettings s)
    {
        shooter.SetBulletSpeedMultiplier(s.coinBulletSpeedMultiplier);

        if (s.coinBulletsPerShot > 0)
            shooter.SetBulletOverrides(s.coinBulletsPerShot, s.coinSpread);
        else
            shooter.ClearBulletOverrides();
    }

    private void ClearCoinSettings()
    {
        shooter.SetBulletSpeedMultiplier(1f);
        shooter.ClearBulletOverrides();
    }

    private IEnumerator CoinStream(PortobelloPhaseSettings s)
    {
        int shots = Mathf.Max(1, s.coinShots);
        float interval = s.coinShotInterval > 0f ? s.coinShotInterval : coinSprayGun.burstInterval;

        int fired = 0;
        float nextShot = Time.time;
        bool wasMoving = false;

        while (fired < shots)
        {
            TrackAim(s.coinAimTurnRate);

            bool isMoving = false;
            if (player != null)
            {
                float dist = Vector2.Distance(transform.position, player.position);
                if (dist > coinMinDistanceToPlayer)
                {
                    isMoving = true;
                    Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                    transform.position += (Vector3)(dir * s.coinMoveSpeed * Time.deltaTime);
                }
            }

            if (isMoving != wasMoving)
            {
                directionalAnimator?.SetWalking(isMoving);
                wasMoving = isMoving;
            }

            if (Time.time >= nextShot)
            {
                shooter.Shoot();
                fired++;
                nextShot = Time.time + interval;
            }

            yield return null;
        }

        if (wasMoving) directionalAnimator?.SetWalking(false);
    }

    // ==================== DOLLAR BURSTS ====================
    private IEnumerator DollarBurstsAttack()
    {
        if (patternShooter == null || dollarBillWeapon == null || dollarPatterns == null || dollarPatterns.Length == 0)
        {
            yield return new WaitForSeconds(stubAttackDuration);
            yield break;
        }

        PortobelloPhaseSettings s = CurrentSettings;
        int throws = Mathf.Max(1, s.dollarThrows);

        facePlayer = true;
        SetIdleContact();
        shooter?.HideWeapon(true);

        // Vanish and reappear at a spot - he never holds a weapon for this one
        yield return RepositionNoWeapon();

        for (int i = 0; i < throws; i++)
        {
            summonReadyFired = false;
            Trigger(TrigBillSummon);
            yield return WaitForSummonReady(dollarSummonEventTimeout);

            KnifePatternData pattern = dollarPatterns[PickDifferentIndex(dollarPatterns.Length, ref lastDollarPatternIndex)];
            if (pattern == null) continue;

            patternShooter.Throw(dollarBillWeapon, pattern, AimTarget,
                s.dollarSpeedMultiplier, s.dollarCountMultiplier, s.dollarHangMultiplier, null, PlayBillLaunchSound, billLaunchSoundLead);

            if (i < throws - 1)
                yield return new WaitForSeconds(s.dollarSummonPause);
        }
    }

    private void PlayBillLaunchSound()
    {
        AudioManager.Instance?.PlaySFXWithPitch(billLaunchClip, billLaunchVolume, 0.1f);
        ScreenEffects.Instance?.ShakeScreen(dollarBurstShakeForce);
    }

    // Vanish, reappear at a shoot point, but never show a weapon - the bills spawn from thin air
    private IEnumerator RepositionNoWeapon()
    {
        health.SetInvulnerable(BossHealth.ReasonHidden, true);
        DisableContact();
        facePlayer = true;

        yield return WaitUntilIdle();
        Trigger(TrigDespawn);
        yield return WaitForStateFinished();

        TeleportTo(PickShootSpot());
        Trigger(TrigSpawn);
        yield return WaitForReturnToIdle();

        SetIdleContact();
        health.SetInvulnerable(BossHealth.ReasonHidden, false);
    }

    private IEnumerator WaitForSummonReady(float timeout)
    {
        float t = 0f;
        while (!summonReadyFired && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    // Random index that differs from the last
    private static int PickDifferentIndex(int count, ref int last)
    {
        int index = Random.Range(0, count);

        if (count > 1 && index == last)
            index = (index + Random.Range(1, count)) % count;

        last = index;
        return index;
    }

    // ==================== GOLD SPIKES ====================
    private IEnumerator GoldSpikesAttack()
    {
        if (spikeSpawner == null || player == null)
        {
            yield return new WaitForSeconds(stubAttackDuration);
            yield break;
        }

        PortobelloPhaseSettings s = CurrentSettings;

        facePlayer = true;
        SetIdleContact();

        summonReadyFired = false;
        Trigger(TrigSpikeWindup);
        yield return WaitForSummonReady(spikeSummonEventTimeout);

        if (s.spikeScreenShakeDuration > 0f)
            yield return SustainedScreenShake(s.spikeScreenShakeDuration, s.spikeScreenShakeForce);

        int count = Random.Range(Mathf.Max(1, s.spikeCountMin), Mathf.Max(1, s.spikeCountMax) + 1);
        int rounds = Random.Range(Mathf.Max(1, s.spikeRoundsMin), Mathf.Max(1, s.spikeRoundsMax) + 1);

        yield return spikeSpawner.SpawnRounds(count, rounds, s.spikeWarningTime, s.spikeRoundInterval, player);
        yield return spikeSpawner.WaitForAllSpikesFinished();

        facePlayer = true;
    }

    // A few repeated shake pulses across the duration, rather than one instant shake
    private IEnumerator SustainedScreenShake(float duration, float force, float interval = 0.12f)
    {
        float t = 0f;
        while (t < duration)
        {
            ScreenEffects.Instance?.ShakeScreen(force);
            yield return new WaitForSeconds(interval);
            t += interval;
        }
    }

    // ==================== THRONE PHASE ====================
    private IEnumerator ThroneAttack()
    {
        PortobelloPhaseSettings s = CurrentSettings;

        facePlayer = true;
        shooter?.HideWeapon(true);
        DisableContact();

        yield return EnterThrone();

        summonReadyFired = false;
        Trigger(TrigThroneSummon);
        yield return WaitForSummonReady(throneSummonEventTimeout);

        yield return FireActiveStatues();
        yield return SpawnThroneEnemyWave(s);

        yield return ExitThrone();
        SetIdleContact();
    }

    private IEnumerator SpawnThroneEnemyWave(PortobelloPhaseSettings s)
    {
        if (minionSpawner == null || minionPrefabs == null || minionPrefabs.Length == 0
            || enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            yield break;

        GameObject prefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
        int count = Random.Range(Mathf.Max(1, s.throneEnemyCountMin), Mathf.Max(1, s.throneEnemyCountMax) + 1);

        yield return minionSpawner.SpawnWave(prefab, count, enemySpawnPoints,
            weaponGuaranteeChance, healthGuaranteeChance, enemySpawnIntervalMin, enemySpawnIntervalMax);
    }

    // Placeholder until the statue system is built - fires any statues currently active
    private IEnumerator FireActiveStatues()
    {
        yield break;
    }

    // Shared jump cinematic: lift pose, rise off-screen (hidden), a shadow tells the landing
    // spot, then he visibly falls back into frame and lands. Used both entering and leaving the throne.
    private IEnumerator JumpTo(Transform destination)
    {
        facePlayer = true;

        yield return WaitUntilIdle();
        Trigger(TrigThroneJumpUp);
        yield return WaitForStateFinished();

        // Rise up from wherever he currently is
        Vector3 start = transform.position;
        Vector3 aboveStart = start + Vector3.up * throneJumpUpHeight;
        yield return MoveOverTime(start, aboveStart, throneJumpUpDuration);

        // Fully hidden now - reposition high above the destination while nobody can see him move
        SetVisible(false);

        Vector3 destPos = destination != null ? destination.position : transform.position;
        Vector3 aboveDest = destPos + Vector3.up * throneJumpUpHeight;
        transform.position = aboveDest;
        if (body != null) body.position = aboveDest;

        if (throneShadow != null)
        {
            throneShadow.transform.position = destPos;
            throneShadow.SetActive(true);
        }

        yield return new WaitForSeconds(throneShadowLeadTime);

        if (throneShadow != null) throneShadow.SetActive(false);

        // Fall back into view from above the landing spot
        SetVisible(true);
        shooter?.HideWeapon(true);
        yield return MoveOverTime(aboveDest, destPos, throneJumpUpDuration);

        Trigger(TrigThroneLand);
        ScreenEffects.Instance?.ShakeScreen(throneLandShake);

        yield return WaitForReturnToIdle();
    }

    private IEnumerator MoveOverTime(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }

        transform.position = to;
        if (body != null) body.position = to;
    }

    private void SetVisible(bool visible)
    {
        if (spriteRenderers == null) return;

        foreach (SpriteRenderer sr in spriteRenderers)
            if (sr != null) sr.enabled = visible;
    }

    private IEnumerator EnterThrone()
    {
        health.SetInvulnerable(BossHealth.ReasonHidden, true);
        DisableContact();

        yield return JumpTo(thronePoint);
    }

    private IEnumerator ExitThrone()
    {
        yield return JumpTo(PickShootSpot());

        health.SetInvulnerable(BossHealth.ReasonHidden, false);
    }

    // ==================== BURROW ====================
    private IEnumerator BurrowAttack()
    {
        if (player == null)
        {
            yield return new WaitForSeconds(stubAttackDuration);
            yield break;
        }

        PortobelloPhaseSettings s = CurrentSettings;
        int pops = Mathf.Max(1, s.burrowPops);

        facePlayer = true;
        shooter?.HideWeapon(true);

        for (int p = 0; p < pops; p++)
        {
            yield return BurrowDiveAndChase(s);
            yield return BurrowPopSmash(s);
            yield return new WaitForSeconds(s.burrowPopCooldown);
        }

        SetIdleContact();
        health.SetInvulnerable(BossHealth.ReasonHidden, false);
        facePlayer = true;
    }

    private IEnumerator BurrowDiveAndChase(PortobelloPhaseSettings s)
    {
        DisableContact();
        health.SetInvulnerable(BossHealth.ReasonHidden, true);

        yield return WaitUntilIdle();
        Trigger(TrigBurrowIn);
        yield return WaitForStateFinished();

        StartBurrowLoopSound();

        float elapsed = 0f;
        while (elapsed < s.burrowChaseDuration)
        {
            if (player != null)
            {
                Vector2 dir = (Vector2)player.position - (Vector2)transform.position;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    dir.Normalize();
                    transform.position += (Vector3)(dir * s.burrowChaseSpeed * Time.deltaTime);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // A brief pause underground before surfacing - the player's cue to get clear
        yield return new WaitForSeconds(s.burrowStopPause);
    }

    private IEnumerator BurrowPopSmash(PortobelloPhaseSettings s)
    {
        StopBurrowLoopSound();

        burrowImpactFired = false;
        Trigger(TrigBurrowOut);
        health.SetInvulnerable(BossHealth.ReasonHidden, false);

        float t = 0f;
        while (!burrowImpactFired && t < burrowImpactEventTimeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        yield return WaitForReturnToIdle();
    }

    // Circular AoE damage, applied once at the exact crash frame via the animation event
    private void DoBurrowSmash(PortobelloPhaseSettings s)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, s.burrowSmashRadius);

        foreach (Collider2D col in hits)
        {
            PlayerHealth ph = col.GetComponentInParent<PlayerHealth>();
            if (ph == null || ph.IsOnCooldown()) continue;

            ph.TakeDamage(s.burrowSmashDamage);

            BaseMover mover = col.GetComponentInParent<BaseMover>();
            if (mover != null)
            {
                Vector2 dir = ((Vector2)ph.transform.position - (Vector2)transform.position).normalized;
                mover.ApplyKnockback(dir * s.burrowSmashKnockback);
            }
        }

        ScreenEffects.Instance?.ShakeScreen(s.burrowSmashShake);
        AudioManager.Instance?.PlaySFXWithPitch(burrowCrashClip, burrowCrashVolume, 0.1f);
    }

    private void StartBurrowLoopSound()
    {
        AudioManager.Instance?.PlayLoopingSFX(ref burrowLoopSource, burrowLoopClip, burrowLoopVolume);
    }

    private void StopBurrowLoopSound()
    {
        AudioManager.Instance?.StopLoopingSFX(ref burrowLoopSource);
        burrowLoopPausedByUs = false;
    }

    // ==================== SHARED GUN HELPERS ====================

    // Vanish, reappear at a shoot point (stationary — no movement toward player), gun in hand
    private IEnumerator Reposition()
    {
        health.SetInvulnerable(BossHealth.ReasonHidden, true);
        DisableContact();
        facePlayer = true;

        yield return WaitUntilIdle();
        Trigger(TrigDespawn);
        yield return WaitForStateFinished();

        TeleportTo(PickShootSpot());

        currentAim = AimTarget();
        if (weaponAimer != null) weaponAimer.SetAimDirection(currentAim);

        Trigger(TrigSpawn);

        yield return new WaitForSeconds(weaponShowDelay);
        shooter.HideWeapon(false);

        yield return WaitForReturnToIdle();

        SetIdleContact();
        health.SetInvulnerable(BossHealth.ReasonHidden, false);
    }

    private Transform PickShootSpot()
    {
        if (shootSpots == null || shootSpots.Length == 0) return null;

        bool canAvoidRepeat = shootSpots.Length > 1;
        List<int> allowed = new List<int>();

        for (int i = 0; i < shootSpots.Length; i++)
        {
            if (shootSpots[i] == null) continue;
            if (canAvoidRepeat && i == lastShootSpot) continue;
            if (canAvoidRepeat && Vector2.Distance(shootSpots[i].position, transform.position) < shootSpotMinMoveDistance) continue;
            allowed.Add(i);
        }

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

    private IEnumerator GunWindup(float duration)
    {
        duration = Mathf.Max(0.05f, duration);
        AudioManager.Instance?.PlaySFXWithPitch(gunWindupClip, gunWindupVolume, 0.1f);

        int pulses = Mathf.Max(1, gunWindupPulses);
        float pulseInterval = duration / pulses;
        float nextPulse = 0f;
        int pulsesDone = 0;

        float t = 0f;
        while (t < duration)
        {
            TrackAim(0f);

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

    private IEnumerator GunStream(PortobelloPhaseSettings s)
    {
        int shots = s.goldBarShots > 0 ? s.goldBarShots : Mathf.Max(1, goldBarGun.burstCount);
        float interval = s.goldBarShotInterval > 0f ? s.goldBarShotInterval : goldBarGun.burstInterval;

        int fired = 0;
        float nextShot = Time.time;

        while (fired < shots)
        {
            TrackAim(s.goldBarAimTurnRate);

            if (Time.time >= nextShot)
            {
                shooter.Shoot();
                ScreenEffects.Instance?.ShakeScreen(goldBarShotShakeForce);
                fired++;
                nextShot = Time.time + interval;
            }

            yield return null;
        }
    }

    private IEnumerator HoldAim(float duration, float turnRate)
    {
        float t = 0f;
        while (t < duration)
        {
            TrackAim(turnRate);
            t += Time.deltaTime;
            yield return null;
        }
    }

    private Vector2 AimTarget()
    {
        if (player == null) return Vector2.right;

        Vector2 origin = weaponAimer != null ? (Vector2)weaponAimer.transform.position : (Vector2)transform.position;
        Vector2 d = (Vector2)player.position - origin;
        return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.right;
    }

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

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void SetIdleContact()
    {
        if (contact != null) contact.SetContact(idleContactDamage, idleContactKnockback);
    }

    private void DisableContact()
    {
        if (contact != null) contact.SetContact(0f, 0f);
    }

    private void TeleportTo(Transform point)
    {
        if (point == null) return;

        transform.position = point.position;
        if (body != null) body.position = point.position;
    }
}