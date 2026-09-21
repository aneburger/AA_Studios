using System.Collections;
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
}

public class ChefPuffsBoss : BossBrain
{
    // Animator trigger names
    private const string TrigDespawn = "Despawn";
    private const string TrigSpawn = "Spawn";
    private const string TrigSummonCharge = "SummonCharge";
    private const string TrigSummon = "Summon";

    [Header("Director")]
    [SerializeField] private int attacksBeforeCounterMin = 2;
    [SerializeField] private int attacksBeforeCounterMax = 3;

    [Header("Phase Settings")]
    [SerializeField] private ChefPhaseSettings phase1 = new ChefPhaseSettings();
    [SerializeField] private ChefPhaseSettings phase2 = new ChefPhaseSettings
    {
        downtimeMin = 0.8f,
        downtimeMax = 1.5f,
        animSpeed = 1.25f,
        summonChargeTime = 0.8f
    };

    [Header("Counter Phase")]
    [SerializeField] private Transform counterPoint;
    [SerializeField] private Transform arenaPoint;
    [SerializeField] private float summonEventTimeout = 3f;

    [Header("Step 3 Stubs")]
    [SerializeField] private float stubAttackDuration = 1.5f;

    private ChefPhaseSettings CurrentSettings => phase <= 1 ? phase1 : phase2;

    private ChefAttack? lastAttack;
    private ChefAttack nextAttack;
    private string currentActionName = "-";
    private int attacksBeforeCounter;
    private int attacksSinceCounter;
    private bool summonActionFired;
    private Rigidbody2D body;

    // -- AWAKE --
    protected override void Awake()
    {
        base.Awake();
        body = GetComponent<Rigidbody2D>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (relay != null) relay.SummonAction += HandleSummonAction;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (relay != null) relay.SummonAction -= HandleSummonAction;
    }

    private void HandleSummonAction()
    {
        summonActionFired = true;
        Log("Animation event: OnSummonAction");
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
    }

    // ==================== DIRECTOR ====================
    protected override IEnumerator FightLoop()
    {
        yield return new WaitForSeconds(openingDelay);

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

        float roll = Random.value * total;
        int fallback = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] <= 0f) continue;

            fallback = i;
            if (roll < weights[i]) return (ChefAttack)i;
            roll -= weights[i];
        }

        return (ChefAttack)fallback;
    }

    private IEnumerator Downtime()
    {
        currentActionName = "Downtime";
        health.SetFlinchEnabled(true);

        float wait = Random.Range(CurrentSettings.downtimeMin, CurrentSettings.downtimeMax);
        float t = 0f;

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

    // ==================== STUB ATTACKS ====================
    private IEnumerator RollAttack()
    {
        Log("  [stub] Roll + daze");
        yield return new WaitForSeconds(stubAttackDuration);
    }

    private IEnumerator CroissantAttack()
    {
        Log("  [stub] Croissant cannon");
        yield return new WaitForSeconds(stubAttackDuration);
    }

    private IEnumerator KnivesAttack()
    {
        Log("  [stub] Knife pattern");
        yield return new WaitForSeconds(stubAttackDuration);
    }

    // ==================== COUNTER PHASE ====================
    private IEnumerator CounterPhase()
    {
        currentActionName = "Counter phase";
        Log("Counter phase: start.");

        health.SetFlinchEnabled(false);
        health.SetInvulnerable(BossHealth.ReasonHidden, true);

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