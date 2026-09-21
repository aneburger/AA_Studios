// Reusable boss "brain". Runs the fight loop, tracks it so it can be cancelled cleanly

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TopDown.Movement;

public abstract class BossBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected BossRoomController room;
    [SerializeField] protected BossHealth health;
    [SerializeField] protected BossHealthBarUI healthBar;
    [SerializeField] protected Animator animator;

    [Header("Animator Names")]
    [SerializeField] protected string idleStateName = "IdleBlend";
    [SerializeField] protected string angryTrigger = "Angry";
    [SerializeField] protected string animSpeedParameter = "AnimSpeed";

    [Header("Fight")]
    [SerializeField] protected float openingDelay = 1f;
    [SerializeField] protected bool facePlayer = true;

    [Header("Phase Transition")]
    [SerializeField] private float transitionEffectDelay = 0.35f;
    [SerializeField] private float transitionShake = 0.9f;
    [SerializeField] private Color transitionFlashColor = Color.white;
    [Range(0f, 1f)] [SerializeField] private float transitionFlashAlpha = 0.6f;
    [SerializeField] private float transitionFlashDuration = 0.35f;
    [SerializeField] private float hitStopDuration = 0.07f;
    [SerializeField] private AudioClip transitionClip;
    [Range(0f, 1f)] [SerializeField] private float transitionVolume = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;
    [SerializeField] private bool showDebugOverlay = true;

    protected Transform player;
    protected DirectionalAnimator directionalAnimator;
    protected BossAnimationRelay relay;

    protected int phase = 1;
    protected bool fightActive;
    protected bool transitionQueued;

    private Coroutine fightRoutine;
    private bool hitStopActive;

    private readonly HashSet<string> animParameters = new HashSet<string>();
    private readonly HashSet<string> warnedParameters = new HashSet<string>();
    private readonly List<GameObject> hazards = new List<GameObject>();
    private readonly List<GameObject> minions = new List<GameObject>();

    protected virtual int LastPhase => 2;

    // What each boss provides
    protected abstract IEnumerator FightLoop();
    protected abstract void ApplyPhase(int newPhase);
    protected virtual void OnFightCancelled() { }

    // -- AWAKE --
    protected virtual void Awake()
    {
        if (health == null) health = GetComponent<BossHealth>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        directionalAnimator = GetComponentInChildren<DirectionalAnimator>();
        relay = GetComponentInChildren<BossAnimationRelay>();

        if (animator != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
                animParameters.Add(p.name);
        }
    }

    // -- ENABLE / DISABLE --
    protected virtual void OnEnable()
    {
        if (room != null) room.OnFightStarted += BeginFight;

        if (health != null)
        {
            health.OnPhaseTwoThreshold += HandleThresholdCrossed;
            health.OnDied += HandleBossDied;
        }

        PlayerHealth.OnPlayerDeath += HandlePlayerDeath;
    }

    protected virtual void OnDisable()
    {
        if (room != null) room.OnFightStarted -= BeginFight;

        if (health != null)
        {
            health.OnPhaseTwoThreshold -= HandleThresholdCrossed;
            health.OnDied -= HandleBossDied;
        }

        PlayerHealth.OnPlayerDeath -= HandlePlayerDeath;
        RestoreTimeScale();
    }

    private void OnDestroy()
    {
        RestoreTimeScale();
    }

    // -- UPDATE --
    private void Update()
    {
        if (!facePlayer || directionalAnimator == null) return;

        if (player == null)
        {
            FindPlayer();
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        if (toPlayer.sqrMagnitude > 0.01f)
            directionalAnimator.SetDirection(toPlayer);
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    // ==================== FIGHT LIFECYCLE ====================
    private void BeginFight()
    {
        if (fightActive) return;

        FindPlayer();
        fightActive = true;
        phase = 1;
        transitionQueued = false;

        ApplyPhase(phase);
        Log("Fight started.");
        fightRoutine = StartCoroutine(FightLoop());
    }

    // Stops everything the boss is doing and runs cleanup.
    protected void CancelFight()
    {
        if (fightRoutine != null)
        {
            StopCoroutine(fightRoutine);
            fightRoutine = null;
        }

        fightActive = false;
        transitionQueued = false;

        RestoreTimeScale();
        ResetAnimTriggers();
        ClearHazards();
        OnFightCancelled();

        Log("Fight cancelled.");
    }

    private void HandleThresholdCrossed()
    {
        if (!fightActive || phase >= LastPhase || transitionQueued) return;

        transitionQueued = true;
        Log("Phase threshold crossed: transition queued.");
    }

    private void HandleBossDied()
    {
        CancelFight();
        Log("Boss died. (Death sequence comes in step 8.)");
    }

    private void HandlePlayerDeath()
    {
        CancelFight();
    }

    // ==================== PHASE TRANSITION ====================
    protected IEnumerator PlayPhaseTransition()
    {
        Log("Phase transition: start.");
        health.SetInvulnerable(BossHealth.ReasonTransition, true);
        health.SetFlinchEnabled(false);

        yield return WaitUntilIdle();
        Trigger(angryTrigger);

        yield return new WaitForSeconds(transitionEffectDelay);

        ScreenEffects.Instance?.FlashColor(transitionFlashColor, transitionFlashAlpha, transitionFlashDuration);
        ScreenEffects.Instance?.ShakeScreen(transitionShake);
        AudioManager.Instance?.PlaySFX(transitionClip, transitionVolume);
        if (healthBar != null) healthBar.Punch();
        yield return HitStop(hitStopDuration);

        // Nothing carries over into the new phase
        ClearHazards();
        ClearMinions();

        yield return WaitForReturnToIdle(5f);

        phase++;
        ApplyPhase(phase);
        health.NotifyPhaseTwoStarted();
        transitionQueued = false;
        health.SetInvulnerable(BossHealth.ReasonTransition, false);

        Log($"Phase {phase} started.");
    }

    // Short real-time freeze. Guarded so it never fights the dialogue system or a pause.
    protected IEnumerator HitStop(float duration)
    {
        if (duration <= 0f || hitStopActive) yield break;
        if (Time.timeScale < 0.99f) yield break;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsRunning) yield break;

        hitStopActive = true;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        RestoreTimeScale();
    }

    private void RestoreTimeScale()
    {
        if (!hitStopActive) return;
        hitStopActive = false;

        if (DialogueManager.Instance == null || !DialogueManager.Instance.IsRunning)
            Time.timeScale = 1f;
    }

    // ==================== HAZARDS / MINIONS ====================
    public void RegisterHazard(GameObject hazard)
    {
        if (hazard != null) hazards.Add(hazard);
    }

    public void RegisterMinion(GameObject minion)
    {
        if (minion != null) minions.Add(minion);
    }

    protected void ClearHazards()
    {
        foreach (GameObject h in hazards)
            if (h != null) Destroy(h);

        hazards.Clear();
    }

    // Placeholder
    protected virtual void ClearMinions()
    {
        foreach (GameObject m in minions)
            if (m != null) Destroy(m);

        minions.Clear();
    }

    // ==================== ANIMATOR HELPERS ====================
    protected void Trigger(string triggerName)
    {
        if (animator == null) return;

        if (!animParameters.Contains(triggerName))
        {
            if (warnedParameters.Add(triggerName))
                Debug.LogWarning($"[{name}] Animator has no parameter '{triggerName}'.", this);
            return;
        }

        animator.SetTrigger(triggerName);
    }

    protected void ResetAnimTriggers()
    {
        if (animator == null) return;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger)
                animator.ResetTrigger(p.name);
        }
    }

    protected void SetAnimSpeed(float speed)
    {
        if (animator != null && animParameters.Contains(animSpeedParameter))
            animator.SetFloat(animSpeedParameter, speed);
    }

    protected bool IsIdle()
    {
        if (animator == null) return true;
        return animator.GetCurrentAnimatorStateInfo(0).IsName(idleStateName) && !animator.IsInTransition(0);
    }

    // Waits until the animator is back in the idle state
    protected IEnumerator WaitUntilIdle(float timeout = 4f)
    {
        float t = 0f;
        while (!IsIdle())
        {
            t += Time.deltaTime;
            if (t > timeout)
            {
                Debug.LogWarning($"[{name}] Timed out waiting for the '{idleStateName}' state. Is idleStateName correct?", this);
                yield break;
            }
            yield return null;
        }
    }

    protected IEnumerator WaitForReturnToIdle(float timeout = 4f)
    {
        yield return null;
        yield return WaitUntilIdle(timeout);
    }

    // After setting a trigger whose state holds its last frame
    protected IEnumerator WaitForStateFinished(float timeout = 4f)
    {
        if (animator == null) yield break;

        yield return null;

        float t = 0f;
        while (t < timeout)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (!animator.IsInTransition(0) && info.normalizedTime >= 1f) yield break;

            t += Time.deltaTime;
            yield return null;
        }
    }

    // ==================== DEBUG ====================
    protected void Log(string message)
    {
        if (debugLogging) Debug.Log($"[{name}] {message}", this);
    }

    protected virtual string DebugSummary()
    {
        return $"Phase {phase}" + (transitionQueued ? " | TRANSITION QUEUED" : "");
    }

    [ContextMenu("Debug: Queue Phase Transition")]
    private void DebugQueueTransition()
    {
        HandleThresholdCrossed();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private GUIStyle debugStyle;

    private void OnGUI()
    {
        if (!showDebugOverlay || !fightActive) return;

        if (debugStyle == null)
        {
            debugStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            debugStyle.normal.textColor = Color.yellow;
        }

        GUI.Label(new Rect(12f, 12f, 1000f, 30f), DebugSummary(), debugStyle);
    }
#endif
}