using System.Collections;
using UnityEngine;
using TopDown.Movement;

public class CrowdMember : MonoBehaviour
{
    [Header("References (auto-found if empty)")]
    [SerializeField] private Animator animator;
    [SerializeField] private DirectionalAnimator directionalAnimator;
    [SerializeField] private BaseMover mover;
    [SerializeField] private Transform hopTarget;
    [SerializeField] private Transform shadow;

    [Header("Spawn")]
    [SerializeField] private string spawnTrigger = "Spawn";
    [SerializeField] private AudioClip spawnClip;
    [Range(0f, 1f)] [SerializeField] private float spawnVolume = 1f;

    [Header("Idle Variation (breaks up the sync)")]
    [SerializeField] private bool randomizeIdle = true;
    [SerializeField] private string idleStateName = "Idle";
    [Range(0f, 0.5f)] [SerializeField] private float speedVariation = 0.2f;

    [Header("Idle Glance")]
    [SerializeField] private bool glanceEnabled = true;
    [SerializeField] private float glanceIntervalMin = 4f;
    [SerializeField] private float glanceIntervalMax = 10f;
    [SerializeField] private float glanceDurationMin = 0.6f;
    [SerializeField] private float glanceDurationMax = 1.4f;

    [Header("Cheer")]
    [SerializeField] private string cheerTrigger = "";
    [SerializeField] private int cheerHops = 2;
    [SerializeField] private float hopHeight = 0.15f;
    [SerializeField] private float hopDuration = 0.18f;

    [Header("Speech Bubble")]
    [SerializeField] private Vector3 bubbleOffsetExtra = Vector3.zero;

    public Vector3 BubbleOffsetExtra => bubbleOffsetExtra;

    private Vector2 homeFacing = Vector2.up;
    private Vector3 hopBasePos;
    private Vector3 shadowBasePos;
    private Coroutine glanceRoutine;
    private Coroutine idleRoutine;
    private Coroutine cheerRoutine;

    // -- AWAKE --
    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (directionalAnimator == null) directionalAnimator = GetComponentInChildren<DirectionalAnimator>(true);
        if (mover == null) mover = GetComponent<BaseMover>();
        if (hopTarget == null) hopTarget = animator != null ? animator.transform : transform;

        if (animator != null && speedVariation > 0f)
        {
            float cap = animator.speed;
            animator.speed = Random.Range(cap * (1f - speedVariation), cap);
        }

        hopBasePos = hopTarget.localPosition;
        if (shadow != null) shadowBasePos = shadow.localPosition;
    }

    private void OnEnable()
    {
        glanceRoutine = StartCoroutine(GlanceLoop());

        if (randomizeIdle && animator != null && !string.IsNullOrEmpty(idleStateName))
            idleRoutine = StartCoroutine(RandomizeIdleOffset());
    }

    private void OnDisable()
    {
        if (glanceRoutine != null) StopCoroutine(glanceRoutine);
        if (idleRoutine != null) StopCoroutine(idleRoutine);
        idleRoutine = null;
        if (cheerRoutine != null) StopCoroutine(cheerRoutine);
        glanceRoutine = null;
        cheerRoutine = null;
        if (hopTarget != null) ApplyHop(0f);
    }

    // -- SPAWN --
    public void Spawn(bool animate = true, bool playSound = true)
    {
        gameObject.SetActive(true);
        SetFacing(homeFacing);

        if (animate && animator != null && HasTrigger(animator, spawnTrigger))
            animator.SetTrigger(spawnTrigger);

        if (playSound && spawnClip != null)
            AudioManager.Instance?.PlaySFX(spawnClip, spawnVolume);
    }

    // -- FACING --
    public void SetHomeFacing(Vector2 dir)
    {
        homeFacing = Snap(dir);
        SetFacing(homeFacing);
    }

    public void FaceToward(Vector3 worldPoint)
    {
        SetHomeFacing(worldPoint - transform.position);
    }

    public void SetGlanceEnabled(bool value)
    {
        glanceEnabled = value;
    }

    private void SetFacing(Vector2 dir)
    {
        if (!isActiveAndEnabled) return;

        dir = Snap(dir);
        if (mover != null) mover.SetFacingOverride(dir);
        if (directionalAnimator != null) directionalAnimator.SetDirection(dir);
    }

    private static Vector2 Snap(Vector2 d)
    {
        if (d.sqrMagnitude < 0.0001f) return Vector2.up;
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) return d.x >= 0f ? Vector2.right : Vector2.left;
        return d.y >= 0f ? Vector2.up : Vector2.down;
    }

    // -- IDLE OFFSET --
    private IEnumerator RandomizeIdleOffset()
    {
        int idleHash = Animator.StringToHash(idleStateName);

        yield return null;
        yield return null;

        int frames = 0;
        while (frames < 180)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.shortNameHash == idleHash && !animator.IsInTransition(0))
            {
                animator.Play(idleHash, 0, Random.value);
                idleRoutine = null;
                yield break;
            }

            // Don't burn the timeout while the game is paused
            if (Time.timeScale > 0f) frames++;
            yield return null;
        }

        idleRoutine = null;
    }

    // -- GLANCE  --
    private IEnumerator GlanceLoop()
    {
        // Random first wait so the whole crowd doesn't glance in sync
        yield return new WaitForSeconds(Random.Range(0f, glanceIntervalMax));

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(glanceIntervalMin, glanceIntervalMax));
            if (!glanceEnabled) continue;

            // Perpendicular to home: left/right when home is up
            Vector2 side = Random.value < 0.5f
                ? new Vector2(-homeFacing.y, homeFacing.x)
                : new Vector2(homeFacing.y, -homeFacing.x);

            SetFacing(side);
            yield return new WaitForSeconds(Random.Range(glanceDurationMin, glanceDurationMax));
            SetFacing(homeFacing);
        }
    }

    // -- CHEER --
    public void Cheer()
    {
        if (!isActiveAndEnabled) return;

        // Optional cheer animation plays together with the hop
        if (animator != null && HasTrigger(animator, cheerTrigger))
            animator.SetTrigger(cheerTrigger);

        if (cheerRoutine != null) StopCoroutine(cheerRoutine);
        cheerRoutine = StartCoroutine(HopRoutine());
    }

    private IEnumerator HopRoutine()
    {
        int hops = Mathf.Max(1, cheerHops);
        for (int i = 0; i < hops; i++)
        {
            float t = 0f;
            while (t < hopDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / Mathf.Max(0.0001f, hopDuration));
                ApplyHop(Mathf.Sin(p * Mathf.PI) * hopHeight);
                yield return null;
            }
        }

        ApplyHop(0f);
        cheerRoutine = null;
    }

    private void ApplyHop(float height)
    {
        hopTarget.localPosition = hopBasePos + Vector3.up * height;
        if (shadow != null) shadow.localPosition = shadowBasePos - Vector3.up * height;
    }

    private static bool HasTrigger(Animator a, string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName)) return false;
        foreach (AnimatorControllerParameter p in a.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
                return true;
        return false;
    }
}