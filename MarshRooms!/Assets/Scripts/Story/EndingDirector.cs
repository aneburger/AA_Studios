using System.Collections;
using UnityEngine;
using TopDown.Movement;
using Unity.Cinemachine;
using UnityEngine.Serialization;

public class EndingDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossRoomController room;

    [Header("Marsh Walk To Cage")]
    [SerializeField] private Transform marshCageSpot;
    [SerializeField] private Vector2 faceDirectionAtCage = Vector2.up;
    [Range(0.1f, 1f)] [SerializeField] private float walkSpeedScale = 0.5f;
    [SerializeField] private float walkTimeout = 6f;
    [SerializeField] private float arriveDistance = 0.1f;

    [Header("Cage (sprite swap)")]
    [SerializeField] private SpriteRenderer cageRenderer;
    [SerializeField] private Sprite cageOpenSprite;
    [SerializeField] private string cageOpenSortingLayer = "Background";
    [SerializeField] private AudioClip cageClickClip;
    [Range(0f, 1f)] [SerializeField] private float cageClickVolume = 1f;
    [SerializeField] private float delayAfterClick = 0.4f;
    [SerializeField] private float cageOpenDuration = 0.6f;

    [Header("Mr. Blobs")]
    [SerializeField] private Transform blobs;
    [SerializeField] private GameObject blobsShadow;
    [SerializeField] private Transform blobsFreeSpot;
    [SerializeField] private int hopCount = 3;
    [SerializeField] private float hopDuration = 0.9f;
    [SerializeField] private float hopHeight = 0.25f;
    [SerializeField] private AudioClip hopClip;
    [Range(0f, 1f)] [SerializeField] private float hopVolume = 0.8f;
    [Tooltip("Calls MrBlobsAnimator.FlipFacing() once he lands (only if he lands facing the wrong way)")]
    [SerializeField] private MrBlobsAnimator blobsAnimator;
    [SerializeField] private bool flipBlobsOnLand = false;

    [Header("Reunion")]
    [SerializeField] private Vector2 faceDirectionAtBlobs = Vector2.right;
    [SerializeField] private float pauseBeforeReunionDialogue = 0.5f;
    [SerializeField] private DialogueSequence reunionDialogue;
    [SerializeField] private float pauseAfterReunionDialogue = 0.6f;

    [Header("Crowd (C + D)")]
    [SerializeField] private CrowdChatter chatter;
    [SerializeField] private CrowdMember[] surroundCrowd;
    [Header("Chef Puffs (only appears if he was NOT killed)")]
    [Tooltip("Normal crowd member, inactive in the scene. Joins the final audience if the Chef was spared.")]
    [SerializeField] private CrowdMember puffsAudience;

    [Header("Crowd Spawn")]
    [Tooltip("Marsh turns this way as the crowd starts spawning.")]
    [SerializeField] private Vector2 faceDirectionOnCrowd = Vector2.down;
    [SerializeField] private float crowdStaggerMin = 0.1f;
    [SerializeField] private float crowdStaggerMax = 0.3f;
    [SerializeField] private float pauseAfterCrowdSpawn = 0.8f;
    [Tooltip("Part 1: Marsh's 'What! Not you guys again.' / 'Stay back!'")]
    [SerializeField] private DialogueSequence surroundDialogue;
    [Tooltip("The final crowd member (the 'Red One' speaker). Inactive in the scene, spawns after the first dialogue.")]
    [SerializeField] private CrowdMember speakerMember;
    [SerializeField] private float pauseBeforeSpeakerSpawn = 0.6f;
    [SerializeField] private float pauseAfterSpeakerSpawn = 0.6f;
    [Tooltip("Part 2: everything from 'Look, everyone! It's the Red One!' to 'ALL HAIL PRESIDENT BLOBS!'")]
    [SerializeField] private DialogueSequence redOneDialogue;
    [SerializeField] private string[] hailCheerLines = { "ALL HAIL BLOBS!", "Yayyy!", "Woo!", "All hail Mr. Blobs!" };
    [Tooltip("How long the crowd cheers on a visible screen before the fade starts.")]
    [SerializeField] private float cheerBeforeFade = 4f;
    [SerializeField] private float fadeToBlackDuration = 1.2f;
    [SerializeField] private float blackHoldTime = 1f;

    [Header("Time Skip (E)")]
    [Tooltip("Toggled off while the screen is black.")]
    [SerializeField] private GameObject[] oldDecor;
    [Tooltip("Toggled on while the screen is black.")]
    [SerializeField] private GameObject[] newDecor;
    [SerializeField] private Transform blobsThroneSpot;
    [Tooltip("Animator on Mr. Blobs. Auto-found on the Blobs object if empty.")]
    [SerializeField] private Animator blobsCrownAnimator;
    [SerializeField] private string crownTrigger = "Crown";
    [SerializeField] private Transform marshThroneSpot;
    [SerializeField] private Vector2 faceDirectionAtThrone = Vector2.down;
    [Tooltip("Hand-placed, inactive in the scene. Separate from the surround crowd.")]
    [SerializeField] private CrowdMember[] audienceCrowd;
    [Tooltip("Optional. Audience members turn to face this (e.g. the throne). Empty = they face up.")]
    [SerializeField] private Transform audienceFaceTarget;
    [Tooltip("Time spent on black after everything is set up, before the fade back in.")]
    [SerializeField] private float pauseWhileBlack = 0.8f;
    [SerializeField] private float fadeInDuration = 1.2f;
    [SerializeField] private float pauseAfterFadeIn = 0.8f;

    [Header("Epilogue (F)")]
    [Tooltip("Marsh: 'Everyone quiet! Your new president has something to say.'")]
    [SerializeField] private DialogueSequence epilogueIntroDialogue;
    [Header("Epilogue Demands (Blobs speaks, a pause, then Marsh translates)")]
    [SerializeField] private DialogueSequence demand1BlobsDialogue;
    [FormerlySerializedAs("demand1Dialogue")]
    [SerializeField] private DialogueSequence demand1MarshDialogue;
    [SerializeField] private DialogueSequence demand2BlobsDialogue;
    [FormerlySerializedAs("demand2Dialogue")]
    [SerializeField] private DialogueSequence demand2MarshDialogue;
    [SerializeField] private DialogueSequence demand3BlobsDialogue;
    [FormerlySerializedAs("demand3Dialogue")]
    [SerializeField] private DialogueSequence demand3MarshDialogue;
    [Tooltip("The little beat between Blobs's demand and Marsh translating it.")]
    [SerializeField] private float pauseBetweenBlobsAndMarsh = 0.4f;
    [SerializeField] private string[] demand1CheerLines = { "YAY! LETTUCE!", "Yay lettuce!", "WOO BLOBS RULES", "Lettuce!" };
    [SerializeField] private string[] demand2CheerLines = { "Yay!", "Woo!", "Long live Blobs!" };
    [SerializeField] private string[] demand3CheerLines = { "Boo Escargot!", "Yuck Escargot!", "BANNED!", "Down with Escargot!" };
    [Header("Epilogue Cheer Intensity")]
    [Tooltip("Demands 1 and 2: below 1 is calmer, 1 = the CrowdChatter values.")]
    [SerializeField] private float demandCheerIntensity = 0.5f;
    [SerializeField] private float demandCheerDuration = 2.5f;
    [Tooltip("Demand 3 (the finale): above 1 is wilder than normal.")]
    [SerializeField] private float finalCheerIntensity = 1.5f;
    [Tooltip("How long the finale cheers on a visible screen before the fade starts.")]
    [SerializeField] private float finalCheerBeforeFade = 4f;
    [SerializeField] private float endFadeDuration = 1.5f;
    [SerializeField] private float endBlackHoldTime = 1f;

    [Header("Camera (time skip)")]
    [Tooltip("Leave empty: the CinemachineCamera is found automatically at the time skip.")]
    [SerializeField] private CinemachineCamera followCamera;
    [Tooltip("Empty at the middle of the room. The camera cuts to it while the screen is black.")]
    [SerializeField] private Transform finalCameraTarget;

    private PlayerMover mover;
    private PlayerAimer aimer;
    private PlayerShooter shooter;
    private Transform player;

    // Hook this to PortobelloOutro.onOutroFinished
    public void Begin()
    {
        StartCoroutine(RunEnding());
    }

    private IEnumerator RunEnding()
    {
        if (room == null || room.Player == null)
        {
            Debug.LogWarning("EndingDirector: no BossRoomController / player.", this);
            yield break;
        }

        player = room.Player.transform;
        mover = room.Mover;
        aimer = room.Player.GetComponent<PlayerAimer>();
        shooter = room.Player.GetComponent<PlayerShooter>();

        // Player is already input-locked by PortobelloOutro (LockPlayerExternally).
        if (shooter != null) shooter.HideWeapon(true);
        if (aimer != null) aimer.enabled = false;

        yield return new WaitForSeconds(0.5f);

        // -- B: Reunion --
        yield return WalkPlayerTo(marshCageSpot, faceDirectionAtCage);
        yield return new WaitForSeconds(0.3f);

        yield return OpenCage();
        yield return BlobsHopOut();

        FaceMarsh(faceDirectionAtBlobs);
        yield return new WaitForSeconds(pauseBeforeReunionDialogue);

        yield return PlayDialogue(reunionDialogue);
        yield return new WaitForSeconds(pauseAfterReunionDialogue);

        // -- C: crowd surrounds Marsh and Blobs --
        FaceMarsh(faceDirectionOnCrowd);
        yield return SpawnCrowd(surroundCrowd, null);
        yield return new WaitForSeconds(pauseAfterCrowdSpawn);

        yield return PlayDialogue(surroundDialogue);

        // The speaker spawns in after Marsh's reaction, then the dialogue continues
        yield return new WaitForSeconds(pauseBeforeSpeakerSpawn);
        if (speakerMember != null)
            yield return SpawnCrowd(new[] { speakerMember }, null);
        yield return new WaitForSeconds(pauseAfterSpeakerSpawn);

        yield return PlayDialogue(redOneDialogue);

        // -- D: crowd cheers with speech bubbles while the screen fades out --
        // The cheering runs in waves until the fade has finished
        if (chatter != null) chatter.CheerFor(cheerBeforeFade + fadeToBlackDuration, hailCheerLines);
        yield return new WaitForSeconds(cheerBeforeFade);

        ScreenEffects.Instance?.FadeToBlack(fadeToBlackDuration);
        yield return new WaitForSeconds(fadeToBlackDuration + blackHoldTime);

        // -- E: time skip (screen is black) --
        yield return TimeSkip();

        ScreenEffects.Instance?.FadeFromBlack(fadeInDuration);
        yield return new WaitForSeconds(fadeInDuration + pauseAfterFadeIn);

        // -- F: epilogue --
        yield return PlayDialogue(epilogueIntroDialogue);

        yield return DemandBeat(demand1BlobsDialogue, demand1MarshDialogue, demand1CheerLines, demandCheerIntensity, demandCheerDuration);
        yield return DemandBeat(demand2BlobsDialogue, demand2MarshDialogue, demand2CheerLines, demandCheerIntensity, demandCheerDuration);

        // Demand 3 is the finale: wilder cheering, and the fade starts while they cheer
        if (demand3BlobsDialogue != null || demand3MarshDialogue != null)
        {
            yield return PlayDemandDialogue(demand3BlobsDialogue, demand3MarshDialogue);

            if (chatter != null)
                chatter.CheerFor(finalCheerBeforeFade + endFadeDuration, demand3CheerLines, finalCheerIntensity);
            yield return new WaitForSeconds(finalCheerBeforeFade);
        }

        ScreenEffects.Instance?.FadeToBlack(endFadeDuration);
        yield return new WaitForSeconds(endFadeDuration + endBlackHoldTime);

        // -- G: music and credits go here next --
    }

    // One demand: dialogue, then a short calm cheer. An empty dialogue slot skips the whole beat.
    private IEnumerator DemandBeat(DialogueSequence blobsDialogue, DialogueSequence marshDialogue, string[] cheerLines, float intensity, float cheerDuration)
    {
        if (blobsDialogue == null && marshDialogue == null) yield break;

        yield return PlayDemandDialogue(blobsDialogue, marshDialogue);

        if (chatter != null) chatter.CheerFor(cheerDuration, cheerLines, intensity);
        yield return new WaitForSeconds(cheerDuration);
    }

    // Blobs's demand, a small pause, then Marsh's translation. Either slot can be empty.
    private IEnumerator PlayDemandDialogue(DialogueSequence blobsDialogue, DialogueSequence marshDialogue)
    {
        if (blobsDialogue != null)
        {
            yield return PlayDialogue(blobsDialogue);
            if (marshDialogue != null)
                yield return new WaitForSeconds(pauseBetweenBlobsAndMarsh);
        }

        if (marshDialogue != null)
            yield return PlayDialogue(marshDialogue);
    }

    // -- TIME SKIP --
    private IEnumerator TimeSkip()
    {
        // Old crowd leaves
        if (surroundCrowd != null)
            foreach (CrowdMember m in surroundCrowd)
                if (m != null) m.gameObject.SetActive(false);
        if (speakerMember != null) speakerMember.gameObject.SetActive(false);

        // Decor swap, each object toggled individually
        SetAll(oldDecor, false);
        SetAll(newDecor, true);

        // Blobs on the throne, crowned
        if (blobs != null && blobsThroneSpot != null)
        {
            Rigidbody2D rb = blobs.GetComponent<Rigidbody2D>();
            SetBlobsPosition(rb, blobsThroneSpot.position);
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        Animator crownAnim = blobsCrownAnimator != null
            ? blobsCrownAnimator
            : (blobs != null ? blobs.GetComponentInChildren<Animator>() : null);
        if (crownAnim != null && HasTrigger(crownAnim, crownTrigger))
            crownAnim.SetTrigger(crownTrigger);
        else
            Debug.LogWarning("EndingDirector: no Blobs Animator with a '" + crownTrigger + "' trigger found.", this);

        // Marsh next to the throne
        if (marshThroneSpot != null)
            TeleportMarsh(marshThroneSpot.position, faceDirectionAtThrone);

        // Camera: stop following Marsh, cut straight to the middle of the room (no pan)
        if (followCamera == null) followCamera = FindFirstObjectByType<CinemachineCamera>();
        if (followCamera != null && finalCameraTarget != null)
        {
            followCamera.Target.TrackingTarget = finalCameraTarget;
            followCamera.PreviousStateIsValid = false;
        }

        // Audience is simply switched on (no spawn animation, no spawn sound, all at once)
        if (chatter != null) chatter.SetMembers(null);
        yield return SpawnCrowd(audienceCrowd, audienceFaceTarget, playSpawnSound: false, instant: true, animate: false);
        if (ChefWasSpared && puffsAudience != null)
            yield return SpawnCrowd(new[] { puffsAudience }, audienceFaceTarget, playSpawnSound: false, instant: true, animate: false);

        yield return new WaitForSeconds(pauseWhileBlack);
    }

    // The Chef counts as spared unless the player finished him off (run flag set by ChefDownedHits)
    private static bool ChefWasSpared => !StoryFlags.HasRun("killed_chef");

    private static void SetAll(GameObject[] objects, bool active)
    {
        if (objects == null) return;
        foreach (GameObject go in objects)
            if (go != null) go.SetActive(active);
    }

    private void TeleportMarsh(Vector2 pos, Vector2 faceDir)
    {
        if (player == null) return;

        player.position = pos;
        Rigidbody2D rb2d = player.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.position = pos;
            rb2d.linearVelocity = Vector2.zero;
        }

        if (mover != null) mover.SetMoveInput(Vector2.zero);
        FaceMarsh(faceDir);
    }

    private static bool HasTrigger(Animator animator, string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName)) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
                return true;
        return false;
    }

    // -- CROWD --
    private IEnumerator SpawnCrowd(CrowdMember[] crowd, Transform faceCenter, bool playSpawnSound = true, bool instant = false, bool animate = true)
    {
        if (crowd == null) yield break;

        foreach (CrowdMember member in crowd)
        {
            if (member == null) continue;

            // Set facing first (stored while inactive), then Spawn applies it
            if (faceCenter != null) member.FaceToward(faceCenter.position);
            member.Spawn(animate: animate, playSound: playSpawnSound);
            if (chatter != null) chatter.AddMember(member);

            if (!instant)
                yield return new WaitForSeconds(Random.Range(crowdStaggerMin, crowdStaggerMax));
        }
    }

    // -- WALK --
    private IEnumerator WalkPlayerTo(Transform target, Vector2 faceOnArrive)
    {
        if (player == null || mover == null || target == null) yield break;

        float elapsed = 0f;
        while (elapsed < walkTimeout &&
               Vector2.Distance(player.position, target.position) > arriveDistance)
        {
            Vector2 dir = ((Vector2)target.position - (Vector2)player.position).normalized;

            mover.SetMoveInput(dir * walkSpeedScale);
            mover.SetFacingOverride(dir);
            aimer?.SetAimOverride(dir);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mover.SetMoveInput(Vector2.zero);
        FaceMarsh(faceOnArrive);
        mover.ForceIdleAnimation();
    }

    private void FaceMarsh(Vector2 dir)
    {
        if (mover == null) return;
        mover.SetFacingOverride(dir);
        mover.FaceDirection(dir);
        aimer?.SetAimOverride(dir);
        mover.ForceIdleAnimation();
    }

    // -- CAGE --
    private IEnumerator OpenCage()
    {
        if (cageClickClip != null)
            AudioManager.Instance?.PlaySFX(cageClickClip, cageClickVolume);

        yield return new WaitForSeconds(delayAfterClick);

        if (cageRenderer != null)
        {
            if (cageOpenSprite != null)
                cageRenderer.sprite = cageOpenSprite;
            else
                Debug.LogWarning("EndingDirector: no cage open sprite assigned.", this);

            // Cage drops behind everything so Blobs can walk out in front of it
            if (!string.IsNullOrEmpty(cageOpenSortingLayer))
                cageRenderer.sortingLayerName = cageOpenSortingLayer;
        }
        else
        {
            Debug.LogWarning("EndingDirector: cageRenderer not assigned.", this);
        }

        yield return new WaitForSeconds(cageOpenDuration);
    }

    // -- BLOBS --
    private IEnumerator BlobsHopOut()
    {
        if (blobs == null || blobsFreeSpot == null) yield break;

        Rigidbody2D rb = blobs.GetComponent<Rigidbody2D>();
        Collider2D col = blobs.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (blobsShadow != null) blobsShadow.SetActive(true);

        Vector2 start = blobs.position;
        Vector2 end = blobsFreeSpot.position;
        int hops = Mathf.Max(1, hopCount);
        float perHop = hopDuration / hops;

        for (int i = 0; i < hops; i++)
        {
            if (hopClip != null)
                AudioManager.Instance?.PlaySFX(hopClip, hopVolume);

            float t = 0f;
            while (t < perHop)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / perHop);
                float overall = (i + p) / hops;

                Vector2 pos = Vector2.Lerp(start, end, overall);
                pos.y += Mathf.Sin(p * Mathf.PI) * hopHeight;

                SetBlobsPosition(rb, pos);
                yield return null;
            }
        }

        SetBlobsPosition(rb, end);
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (col != null) col.enabled = true;

        if (flipBlobsOnLand && blobsAnimator != null)
            blobsAnimator.FlipFacing();

        yield return new WaitForSeconds(0.3f);
    }

    private void SetBlobsPosition(Rigidbody2D rb, Vector2 pos)
    {
        blobs.position = pos;
        if (rb != null) rb.position = pos;
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
}