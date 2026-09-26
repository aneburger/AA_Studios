// Portobello's brain.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("Animation")]
    public float animSpeed = 1f;

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
        goldBarVolleyPause = 0.4f
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

    [Header("Coin Spray Gun")]
    [SerializeField] private WeaponData coinSprayGun;
    [SerializeField] private float coinMinDistanceToPlayer = 1.5f;

    [Header("Dollar Bursts")]
    [SerializeField] private WeaponData dollarBillWeapon;
    [SerializeField] private KnifePatternData[] dollarPatterns;
    [SerializeField] private float dollarSummonEventTimeout = 3f;
    [SerializeField] private AudioClip billSummonClip;
    [Range(0f, 1f)] [SerializeField] private float billSummonVolume = 1f;

    [Header("Fallback")]
    [SerializeField] private float stubAttackDuration = 1.5f;

    private BossContactDamage contact;
    private Rigidbody2D body;
    private EnemyShooter shooter;
    private WeaponAimer weaponAimer;
    private BossPatternShooter patternShooter;

    private Vector2 currentAim = Vector2.right;
    private int lastShootSpot = -1;
    private int lastDollarPatternIndex = -1;
    private bool summonReadyFired;
    private PortobelloAttack? lastAttack;

    // -- AWAKE --
    protected override void Awake()
    {
        base.Awake();
        contact = GetComponent<BossContactDamage>();
        body = GetComponent<Rigidbody2D>();
        shooter = GetComponent<EnemyShooter>();
        weaponAimer = GetComponentInChildren<WeaponAimer>();
        patternShooter = GetComponent<BossPatternShooter>();
    }

    // -- ENABLE --
    protected override void OnEnable()
    {
        base.OnEnable();
        if (relay != null) relay.SummonReady += HandleSummonReady;
    }

    // -- DISABLE --
    protected override void OnDisable()
    {
        base.OnDisable();
        if (relay != null) relay.SummonReady -= HandleSummonReady;
    }

    private void HandleSummonReady()
    {
        summonReadyFired = true;
    }

    protected override void OnAnimTrigger(string triggerName)
    {
        if (triggerName == TrigDespawn)
            AudioManager.Instance?.PlaySFXWithPitch(despawnClip, despawnVolume, 0.1f);
        else if (triggerName == TrigSpawn)
            AudioManager.Instance?.PlaySFXWithPitch(spawnClip, spawnVolume, 0.1f);
        else if (triggerName == TrigBillSummon)
            AudioManager.Instance?.PlaySFXWithPitch(billSummonClip, billSummonVolume, 0.1f);
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
        SetIdleContact();
    }

    // ==================== DIRECTOR ====================
    protected override IEnumerator FightLoop()
    {
        yield return new WaitForSeconds(openingDelay);
        SetIdleContact();

        lastAttack = null;

        while (true)
        {
            PortobelloAttack attack = PickNextAttack();
            yield return RunAttack(attack);
            lastAttack = attack;
            yield return Downtime();
        }
    }

    // Weighted pick that never repeats the previous attack.
    // Only the attacks built so far (Gold Bar Gun, Coin Gun, Dollar Bursts) are included -
    // Gold Spikes and Burrow join this list once they're built.
    private PortobelloAttack PickNextAttack()
    {
        PortobelloPhaseSettings s = CurrentSettings;
        PortobelloAttack[] options = { PortobelloAttack.GoldBarGun, PortobelloAttack.CoinGun, PortobelloAttack.DollarBursts };
        float[] weights = { s.goldBarWeight, s.coinGunWeight, s.dollarBurstsWeight };

        if (lastAttack.HasValue)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] == lastAttack.Value) weights[i] = 0f;
            }
        }

        float total = weights[0] + weights[1] + weights[2];
        if (total <= 0f)
        {
            // Only the previous attack has any weight this phase - allow it rather than stalling
            weights = new[] { s.goldBarWeight, s.coinGunWeight, s.dollarBurstsWeight };
            total = weights[0] + weights[1] + weights[2];
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

    private IEnumerator RunAttack(PortobelloAttack attack)
    {
        switch (attack)
        {
            case PortobelloAttack.GoldBarGun: yield return GoldBarGunAttack(); break;
            case PortobelloAttack.CoinGun: yield return CoinSprayGunAttack(); break;
            case PortobelloAttack.DollarBursts: yield return DollarBurstsAttack(); break;
        }
    }

    private IEnumerator Downtime()
    {
        facePlayer = true;
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
            yield return WaitForSummonReady();

            KnifePatternData pattern = dollarPatterns[PickDifferentIndex(dollarPatterns.Length, ref lastDollarPatternIndex)];
            if (pattern == null) continue;

            patternShooter.Throw(dollarBillWeapon, pattern, AimTarget,
                s.dollarSpeedMultiplier, s.dollarCountMultiplier, s.dollarHangMultiplier, null);

            if (i < throws - 1)
                yield return new WaitForSeconds(s.dollarSummonPause);
        }
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

    private IEnumerator WaitForSummonReady()
    {
        float t = 0f;
        while (!summonReadyFired && t < dollarSummonEventTimeout)
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