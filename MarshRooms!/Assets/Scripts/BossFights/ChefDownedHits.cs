using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ChefDownedHits : MonoBehaviour
{
    [System.Serializable]
    public class Stage
    {
        public int hitsRequired = 5;
        public DialogueSequence dialogue;
    }

    [Header("References")]
    [SerializeField] private BossHealth boss;
    [SerializeField] private Animator bossAnimator;

    [Header("Hits")]
    [SerializeField] private Stage[] stages;
    [SerializeField] private int hitsToKill = 20;

    [Header("Hit Feedback")]
    [SerializeField] private AudioClip hitClip;
    [Range(0f, 1f)] [SerializeField] private float hitVolume = 1f;
    [SerializeField] private float hitSoundCooldown = 0.05f;

    [Header("Final Death")]
    [SerializeField] private string finalDeathTrigger = "FinalDeath";
    [SerializeField] private float disappearDelay = 1.5f;
    [SerializeField] private string disappearTrigger = "Disappear";
    [SerializeField] private bool disableCollidersOnDisappear = true;
    [SerializeField] private GameObject[] hideOnDisappear;
    [SerializeField] private AudioClip finalDeathClip;
    [Range(0f, 1f)] [SerializeField] private float finalDeathVolume = 1f;

    [Header("Poof VFX and Spores")]
    [SerializeField] private GameObject vanishVfxPrefab;
    [SerializeField] private Vector3 vanishVfxOffset = Vector3.zero;
    [SerializeField] private GameObject sporePrefab;
    [SerializeField] private int minSpores = 2;
    [SerializeField] private int maxSpores = 4;
    [SerializeField] private float sporeScatterRadius = 1f;

    [Header("Story")]
    [SerializeField] private string killedRunFlag = "killed_chef";

    public UnityEvent onKilled;

    private bool armed;
    private bool busy;
    private bool killed;
    private int hits;
    private int nextStage;
    private float nextHitSoundTime;

    // -- AWAKE --
    private void Awake()
    {
        if (boss == null) boss = GetComponent<BossHealth>();
        if (bossAnimator == null) bossAnimator = GetComponentInChildren<Animator>();
    }

    // -- ENABLE / DISABLE --
    private void OnEnable()
    {
        if (boss != null) boss.OnHitWhileDead += HandleHit;
    }

    private void OnDisable()
    {
        if (boss != null) boss.OnHitWhileDead -= HandleHit;
    }

    // -- BEGIN --
    public void Begin()
    {
        armed = true;
    }

    // -- HANDLE HIT --
    private void HandleHit()
    {
        if (!armed || killed) return;

        hits++;

        if (hitClip != null && Time.time >= nextHitSoundTime)
        {
            nextHitSoundTime = Time.time + hitSoundCooldown;
            AudioManager.Instance?.PlaySFXWithPitch(hitClip, hitVolume, 0.2f);
        }

        if (!busy) Evaluate();
    }

    // -- EVALUATE --
    private void Evaluate()
    {
        if (killed) return;

        if (stages != null && nextStage < stages.Length && hits >= stages[nextStage].hitsRequired)
        {
            Stage stage = stages[nextStage];
            nextStage++;
            StartCoroutine(PlayStage(stage));
            return;
        }

        if (hits >= hitsToKill)
            StartCoroutine(Kill());
    }

    // -- PLAY STAGE --
    private IEnumerator PlayStage(Stage stage)
    {
        busy = true;

        if (stage.dialogue != null && DialogueManager.Instance != null)
        {
            while (DialogueManager.Instance.IsRunning)
                yield return null;

            bool done = false;
            DialogueManager.Instance.StartDialogue(stage.dialogue, () => done = true);
            yield return new WaitUntil(() => done);
        }

        busy = false;
        Evaluate();
    }

    // -- KILL --
    private IEnumerator Kill()
    {
        killed = true;
        busy = true;

        if (!string.IsNullOrEmpty(killedRunFlag)) StoryFlags.SetRun(killedRunFlag);
        onKilled?.Invoke();

        TriggerAnimator(finalDeathTrigger);

        yield return new WaitForSeconds(disappearDelay);

        TriggerAnimator(disappearTrigger);

        if (hideOnDisappear != null)
        {
            foreach (GameObject go in hideOnDisappear)
            {
                if (go != null) go.SetActive(false);
            }
        }

        if (finalDeathClip != null) AudioManager.Instance?.PlaySFX(finalDeathClip, finalDeathVolume);
        SpawnVanishVfx();
        SpawnSpores();

        if (disableCollidersOnDisappear)
        {
            foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
                c.enabled = false;
        }
    }

    // -- TRIGGER ANIMATOR --
    private void TriggerAnimator(string trigger)
    {
        if (string.IsNullOrEmpty(trigger) || bossAnimator == null) return;

        foreach (AnimatorControllerParameter p in bossAnimator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == trigger)
            {
                bossAnimator.SetTrigger(trigger);
                return;
            }
        }
    }

    // -- VANISH VFX --
    private void SpawnVanishVfx()
    {
        if (vanishVfxPrefab == null) return;
        Instantiate(vanishVfxPrefab, transform.position + vanishVfxOffset, Quaternion.identity);
    }

    // -- SPORES --
    private void SpawnSpores()
    {
        if (sporePrefab == null) return;

        int amount = Random.Range(minSpores, Mathf.Max(minSpores, maxSpores) + 1);
        RoomManager room = RoomManager.Current;

        for (int i = 0; i < amount; i++)
        {
            Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * sporeScatterRadius;

            if (room != null)
                spawnPos = room.GetSafeDropPosition(spawnPos);

            Instantiate(sporePrefab, spawnPos, Quaternion.identity);
        }
    }
}