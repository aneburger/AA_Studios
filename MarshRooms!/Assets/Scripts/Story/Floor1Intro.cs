using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TopDown.Movement;

public class Floor1Intro : MonoBehaviour
{
    [Header("Story Flag")]
    [SerializeField] private string seenId = "intro_floor01";

    [Header("Timing")]
    [SerializeField] private float startDelay = 0.8f;
    [SerializeField] private float pauseAfterPlayerWalk = 0.3f;
    [SerializeField] private float shockedDuration = 1f;
    [SerializeField] private float pauseBeforeDialogue = 0.2f;

    [Header("Player Walk")]
    [SerializeField] private Transform introStartPoint;
    [SerializeField] private Transform playerStandPoint;
    [SerializeField] private Vector2 faceDirectionDuringWalk = Vector2.up;
    [SerializeField] private Vector2 faceDirectionOnArrive = Vector2.left;
    [Range(0.1f, 1f)] [SerializeField] private float walkSpeedScale = 0.5f;
    [SerializeField] private float walkTimeout = 6f;
    [SerializeField] private float arriveDistance = 0.1f;

    [Header("Bodyguard")]
    [SerializeField] private GameObject bodyguard;
    [SerializeField] private Animator bodyguardAnimator;
    [SerializeField] private Transform bodyguardTarget;
    [Range(0.1f, 1f)] [SerializeField] private float bodyguardSpeedMultiplier = 0.6f;
    [SerializeField] private float bodyguardWalkTimeout = 5f;
    [SerializeField] private float bodyguardArriveDistance = 0.15f;
    [SerializeField] private string shockedTrigger = "Shocked";
    [SerializeField] private Vector2 bodyguardWalkFacing = Vector2.down;
    [SerializeField] private Vector2 bodyguardArriveFacing = Vector2.right;
    [SerializeField] private float pauseAfterTurn = 0.25f;

    [Header("Exclamation Mark")]
    [SerializeField] private GameObject exclamationPrefab;
    [SerializeField] private Vector3 exclamationOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float exclamationPopDuration = 0.1f;
    [SerializeField] private float exclamationSettleDuration = 0.08f;
    [SerializeField] private float exclamationOvershoot = 1.25f;
    [SerializeField] private AudioClip shockedClip;
    [Range(0f, 1f)] [SerializeField] private float shockedVolume = 1f;

    [Header("Dialogue")]
    [SerializeField] private DialogueSequence introDialogue;

    private GameObject player;
    private PlayerMover mover;
    private PlayerAimer aimer;
    private PlayerShooter shooter;

    private readonly List<Behaviour> heldEnemies = new List<Behaviour>();
    private bool playerLocked;
    private Coroutine routine;
    private GameObject exclamationInstance;

    // -- ENABLE / DISABLE --
    private void OnEnable()  { PlayerHealth.OnPlayerDeath += HandlePlayerDeath; }
    private void OnDisable() { PlayerHealth.OnPlayerDeath -= HandlePlayerDeath; }

    // -- START --
    private void Start()
    {
        if (!string.IsNullOrEmpty(seenId) && StoryFlags.Has(seenId)) return;

        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        mover = player.GetComponent<PlayerMover>();
        aimer = player.GetComponent<PlayerAimer>();
        shooter = player.GetComponent<PlayerShooter>();

        // Lock immediately so nothing can happen during the fade-in
        LockPlayer();
        HoldEnemies();

        routine = StartCoroutine(Run());
    }

    // -- ON DESTROY --
    private void OnDestroy()
    {
        ReleasePlayer(restoreWeapon: false);
    }

    // -- SEQUENCE --
    private IEnumerator Run()
    {
        yield return null;
        if (shooter != null) shooter.HideWeapon(true);

        yield return MovePlayerToIntroStart();

        yield return new WaitForSeconds(startDelay);

        yield return WalkPlayerToStandPoint();
        yield return new WaitForSeconds(pauseAfterPlayerWalk);

        yield return BodyguardReaction();
        yield return new WaitForSeconds(pauseBeforeDialogue);

        yield return PlayDialogue(introDialogue);

        // Only marked as seen once it has fully played
        if (!string.IsNullOrEmpty(seenId)) StoryFlags.Set(seenId);

        ReleasePlayer(restoreWeapon: true);
        ReleaseEnemies();
        routine = null;
    }

    // -- MOVE PLAYER TO INTRO START --
    private IEnumerator MovePlayerToIntroStart()
    {
        if (player == null || introStartPoint == null) yield break;

        SpawnPoint spawn = FindFirstObjectByType<SpawnPoint>();
        if (spawn != null)
        {
            float waited = 0f;
            while (waited < 1f &&
                   Vector2.Distance(player.transform.position, spawn.transform.position) > 0.01f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        player.transform.position = introStartPoint.position;

        Rigidbody2D rb2d = player.GetComponent<Rigidbody2D>();
        if (rb2d != null) rb2d.linearVelocity = Vector2.zero;
    }

    // -- PLAYER WALK --
    private IEnumerator WalkPlayerToStandPoint()
    {
        if (player == null || mover == null || playerStandPoint == null) yield break;

        if (aimer != null) aimer.enabled = false;

        float elapsed = 0f;
        while (elapsed < walkTimeout &&
               Vector2.Distance(player.transform.position, playerStandPoint.position) > arriveDistance)
        {
            Vector2 dir = ((Vector2)playerStandPoint.position - (Vector2)player.transform.position).normalized;

            mover.SetMoveInput(dir * walkSpeedScale);
            mover.SetFacingOverride(faceDirectionDuringWalk);
            mover.FaceDirection(faceDirectionDuringWalk);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mover.SetMoveInput(Vector2.zero);
        mover.SetFacingOverride(faceDirectionOnArrive);
        mover.FaceDirection(faceDirectionOnArrive);
        mover.ForceIdleAnimation();
    }

    // -- BODYGUARD WALK + SHOCKED --
    private IEnumerator BodyguardReaction()
    {
        if (bodyguard == null) yield break;

        EnemyMover guardMover = bodyguard.GetComponent<EnemyMover>();
        EnemyPathing guardPathing = bodyguard.GetComponent<EnemyPathing>();
        Animator guardAnim = bodyguardAnimator != null ? bodyguardAnimator : bodyguard.GetComponentInChildren<Animator>();

        if (guardMover != null) guardMover.SetFacingOverride(bodyguardWalkFacing);

        // Walk to the target point
        if (guardMover != null && bodyguardTarget != null)
        {
            guardMover.SetSpeedMultiplier(bodyguardSpeedMultiplier);

            float elapsed = 0f;
            while (elapsed < bodyguardWalkTimeout &&
                   Vector2.Distance(bodyguard.transform.position, bodyguardTarget.position) > bodyguardArriveDistance)
            {
                Vector2 dir = guardPathing != null
                    ? guardPathing.GetDirectionToTarget(bodyguardTarget.position)
                    : ((Vector2)bodyguardTarget.position - (Vector2)bodyguard.transform.position).normalized;

                guardMover.Move(dir);

                elapsed += Time.deltaTime;
                yield return null;
            }

            guardMover.Stop();
        }

        // Turn to face the arrive direction, then a short beat before the reaction
        if (guardMover != null) guardMover.SetFacingOverride(bodyguardArriveFacing);
        yield return new WaitForSeconds(pauseAfterTurn);

        // Shocked + exclamation mark
        if (exclamationPrefab != null)
        {
            exclamationInstance = Instantiate(exclamationPrefab, bodyguard.transform);
            exclamationInstance.transform.localPosition = exclamationOffset;
            StartCoroutine(PopIn(exclamationInstance.transform));
        }

        if (shockedClip != null)
            AudioManager.Instance?.PlaySFX(shockedClip, shockedVolume);

        if (guardAnim != null)
        {
            if (HasTrigger(guardAnim, shockedTrigger))
                guardAnim.SetTrigger(shockedTrigger);
        }

        yield return new WaitForSeconds(shockedDuration);

        if (exclamationInstance != null) Destroy(exclamationInstance);
        exclamationInstance = null;
    }

    // -- HAS TRIGGER --
    private static bool HasTrigger(Animator animator, string triggerName)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
                return true;
        }
        return false;
    }

    // -- EXCLAMATION POP --
    private IEnumerator PopIn(Transform t)
    {
        if (t == null) yield break;
        Vector3 full = t.localScale;

        float e = 0f;
        while (e < exclamationPopDuration)
        {
            if (t == null) yield break;
            e += Time.deltaTime;
            float p = Mathf.Clamp01(e / Mathf.Max(0.0001f, exclamationPopDuration));
            t.localScale = full * Mathf.Lerp(0f, exclamationOvershoot, p);
            yield return null;
        }

        e = 0f;
        while (e < exclamationSettleDuration)
        {
            if (t == null) yield break;
            e += Time.deltaTime;
            float p = Mathf.Clamp01(e / Mathf.Max(0.0001f, exclamationSettleDuration));
            t.localScale = full * Mathf.Lerp(exclamationOvershoot, 1f, p);
            yield return null;
        }

        if (t != null) t.localScale = full;
    }

    // -- DIALOGUE --
    private IEnumerator PlayDialogue(DialogueSequence sequence)
    {
        if (sequence == null || DialogueManager.Instance == null) yield break;

        while (DialogueManager.Instance.IsRunning)
            yield return null;

        bool done = false;
        DialogueManager.Instance.StartDialogue(sequence, () => done = true);
        yield return new WaitUntil(() => done);
    }

    // -- ENEMY HOLD --
    private void HoldEnemies()
    {
        heldEnemies.Clear();
        foreach (EnemyAIBase ai in FindObjectsByType<EnemyAIBase>(FindObjectsSortMode.None))
        {
            if (ai == null || !ai.enabled) continue;
            ai.enabled = false;
            heldEnemies.Add(ai);
        }
    }

    private void ReleaseEnemies()
    {
        foreach (Behaviour ai in heldEnemies)
        {
            if (ai != null) ai.enabled = true;
        }
        heldEnemies.Clear();
    }

    // -- PLAYER LOCK --
    private void LockPlayer()
    {
        if (mover == null || playerLocked) return;

        playerLocked = true;
        mover.SetInputLocked(true);
    }

    private void ReleasePlayer(bool restoreWeapon)
    {
        if (!playerLocked) return;
        playerLocked = false;

        if (mover != null)
        {
            mover.SetMoveInput(Vector2.zero);
            mover.ClearFacingOverride();
            mover.SetInputLocked(false);
        }

        if (aimer != null) aimer.enabled = true;
        if (restoreWeapon && shooter != null) shooter.HideWeapon(false);
    }

    // -- PLAYER DEATH --
    private void HandlePlayerDeath()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (exclamationInstance != null) Destroy(exclamationInstance);

        ReleasePlayer(restoreWeapon: false);
        ReleaseEnemies();
    }
}