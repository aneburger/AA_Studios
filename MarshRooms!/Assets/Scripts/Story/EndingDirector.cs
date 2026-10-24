using System.Collections;
using UnityEngine;
using TopDown.Movement;
using Unity.Cinemachine;
using UnityEngine.Serialization;
using UnityEngine.Events;

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
    [SerializeField] private MrBlobsAnimator blobsAnimator;
    [SerializeField] private bool flipBlobsOnLand = false;

    [Header("Reunion")]
    [SerializeField] private Vector2 faceDirectionAtBlobs = Vector2.right;
    [SerializeField] private float pauseBeforeReunionDialogue = 0.5f;
    [SerializeField] private DialogueSequence reunionDialogue;
    [SerializeField] private float pauseAfterReunionDialogue = 0.6f;

    [Header("Crowd")]
    [SerializeField] private CrowdChatter chatter;
    [SerializeField] private CrowdMember[] surroundCrowd;
    [Header("Chef Puffs (only appears if he was NOT killed)")]
    [SerializeField] private CrowdMember puffsAudience;

    [Header("Crowd Spawn")]
    [SerializeField] private Vector2 faceDirectionOnCrowd = Vector2.down;
    [SerializeField] private float crowdStaggerMin = 0.1f;
    [SerializeField] private float crowdStaggerMax = 0.3f;
    [SerializeField] private float pauseAfterCrowdSpawn = 0.8f;
    [SerializeField] private DialogueSequence surroundDialogue;
    [SerializeField] private CrowdMember speakerMember;
    [SerializeField] private float pauseBeforeSpeakerSpawn = 0.6f;
    [SerializeField] private float pauseAfterSpeakerSpawn = 0.6f;
    [SerializeField] private DialogueSequence redOneDialogue;
    [SerializeField] private string[] hailCheerLines;
    [SerializeField] private float cheerBeforeFade = 4f;
    [SerializeField] private float fadeToBlackDuration = 1.2f;
    [SerializeField] private float blackHoldTime = 1f;

    [Header("Time Skip")]
    [SerializeField] private GameObject[] oldDecor;
    [SerializeField] private GameObject[] newDecor;
    [SerializeField] private Transform blobsThroneSpot;
    [SerializeField] private Animator blobsCrownAnimator;
    [SerializeField] private string crownTrigger = "Crown";
    [SerializeField] private Transform marshThroneSpot;
    [SerializeField] private Vector2 faceDirectionAtThrone = Vector2.down;
    [SerializeField] private CrowdMember[] audienceCrowd;
    [SerializeField] private Transform audienceFaceTarget;
    [SerializeField] private float pauseWhileBlack = 0.8f;
    [SerializeField] private float fadeInDuration = 1.2f;
    [SerializeField] private float pauseAfterFadeIn = 0.8f;

    [Header("Epilogue (F)")]
    [SerializeField] private DialogueSequence epilogueIntroDialogue;
    [SerializeField] private DialogueSequence demand1BlobsDialogue;
    [FormerlySerializedAs("demand1Dialogue")]
    [SerializeField] private DialogueSequence demand1MarshDialogue;
    [SerializeField] private DialogueSequence demand2BlobsDialogue;
    [FormerlySerializedAs("demand2Dialogue")]
    [SerializeField] private DialogueSequence demand2MarshDialogue;
    [SerializeField] private DialogueSequence demand3BlobsDialogue;
    [FormerlySerializedAs("demand3Dialogue")]
    [SerializeField] private DialogueSequence demand3MarshDialogue;
    [SerializeField] private float pauseBetweenBlobsAndMarsh = 0.4f;
    [SerializeField] private string[] demand1CheerLines;
    [SerializeField] private string[] demand2CheerLines;
    [SerializeField] private string[] demand3CheerLines;
    [Header("Epilogue Cheer Intensity")]
    [SerializeField] private float demandCheerIntensity = 0.5f;
    [SerializeField] private float demandCheerDuration = 2.5f;
    [SerializeField] private float finalCheerIntensity = 1.5f;
    [SerializeField] private float finalCheerBeforeFade = 4f;
    [SerializeField] private float endFadeDuration = 1.5f;
    [SerializeField] private float endBlackHoldTime = 1f;

    [Header("Music: Reunion")]
    [SerializeField] private AudioClip reunionMusic;
    [Range(0f, 1f)] [SerializeField] private float reunionStartVolume = 0.15f;
    [Range(0f, 1f)] [SerializeField] private float reunionFullVolume = 0.5f;
    [SerializeField] private float reunionFadeInDuration = 2f;
    [SerializeField] private float reunionRaiseDuration = 1.5f;
    [SerializeField] private float crowdMusicCutDuration = 0.4f;
    [Range(0f, 1f)] [SerializeField] private float crowdMusicTargetVolume = 0f;

    [Header("Music: Celebration")]
    [SerializeField] private AudioClip celebrationMusic;
    [Range(0f, 1f)] [SerializeField] private float celebrationVolume = 0.6f;
    [SerializeField] private float celebrationFadeInDuration = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float endMusicTargetVolume = 0f;
    [Range(0f, 1f)] [SerializeField] private float creditsVolume = 0.8f;
    [SerializeField] private float creditsFadeInDuration = 2f;
    [SerializeField] private UnityEvent onCreditsStart;

    [Header("Camera (time skip)")]
    [SerializeField] private CinemachineCamera followCamera;
    [SerializeField] private Transform finalCameraTarget;

    private PlayerMover mover;
    private PlayerAimer aimer;
    private PlayerShooter shooter;
    private Transform player;

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

        if (shooter != null) shooter.HideWeapon(true);
        if (aimer != null) aimer.enabled = false;

        PlayMusic(reunionMusic, reunionStartVolume, reunionFadeInDuration);
        yield return new WaitForSeconds(0.5f);

        // -- B: Reunion --
        yield return WalkPlayerTo(marshCageSpot, faceDirectionAtCage);
        yield return new WaitForSeconds(0.3f);

        if (reunionMusic != null)
            AudioManager.Instance?.FadeMusicVolume(reunionFullVolume, reunionRaiseDuration);

        yield return OpenCage();
        yield return BlobsHopOut();

        FaceMarsh(faceDirectionAtBlobs);
        yield return new WaitForSeconds(pauseBeforeReunionDialogue);

        yield return PlayDialogue(reunionDialogue);
        yield return new WaitForSeconds(pauseAfterReunionDialogue);

        if (reunionMusic != null)
            AudioManager.Instance?.FadeMusicVolume(crowdMusicTargetVolume, crowdMusicCutDuration);

        FaceMarsh(faceDirectionOnCrowd);
        yield return SpawnCrowd(surroundCrowd, null);
        yield return new WaitForSeconds(pauseAfterCrowdSpawn);

        yield return PlayDialogue(surroundDialogue);

        yield return new WaitForSeconds(pauseBeforeSpeakerSpawn);
        if (speakerMember != null)
            yield return SpawnCrowd(new[] { speakerMember }, null);
        yield return new WaitForSeconds(pauseAfterSpeakerSpawn);

        yield return PlayDialogue(redOneDialogue);

        PlayMusic(celebrationMusic, celebrationVolume, celebrationFadeInDuration);
        if (chatter != null) chatter.CheerFor(cheerBeforeFade + fadeToBlackDuration, hailCheerLines);
        yield return new WaitForSeconds(cheerBeforeFade);

        ScreenEffects.Instance?.FadeToBlack(fadeToBlackDuration);
        yield return new WaitForSeconds(fadeToBlackDuration + blackHoldTime);

        yield return TimeSkip();

        ScreenEffects.Instance?.FadeFromBlack(fadeInDuration);
        yield return new WaitForSeconds(fadeInDuration + pauseAfterFadeIn);

        yield return PlayDialogue(epilogueIntroDialogue);

        yield return DemandBeat(demand1BlobsDialogue, demand1MarshDialogue, demand1CheerLines, demandCheerIntensity, demandCheerDuration);
        yield return DemandBeat(demand2BlobsDialogue, demand2MarshDialogue, demand2CheerLines, demandCheerIntensity, demandCheerDuration);

        if (demand3BlobsDialogue != null || demand3MarshDialogue != null)
        {
            yield return PlayDemandDialogue(demand3BlobsDialogue, demand3MarshDialogue);

            if (chatter != null)
                chatter.CheerFor(finalCheerBeforeFade + endFadeDuration, demand3CheerLines, finalCheerIntensity);
            yield return new WaitForSeconds(finalCheerBeforeFade);
        }
        ScreenEffects.Instance?.FadeToBlack(endFadeDuration);
        if (celebrationMusic != null)
            AudioManager.Instance?.FadeMusicVolume(endMusicTargetVolume, endFadeDuration);
        yield return new WaitForSeconds(endFadeDuration + endBlackHoldTime);

        // -- Credits --
        if (celebrationMusic != null)
            AudioManager.Instance?.FadeMusicVolume(creditsVolume, creditsFadeInDuration);
        onCreditsStart?.Invoke();

        // Exit (ClearSavedGame, game-beaten flag, ReturnToMainMenu) goes here next
    }

    private IEnumerator DemandBeat(DialogueSequence blobsDialogue, DialogueSequence marshDialogue, string[] cheerLines, float intensity, float cheerDuration)
    {
        if (blobsDialogue == null && marshDialogue == null) yield break;

        yield return PlayDemandDialogue(blobsDialogue, marshDialogue);

        if (chatter != null) chatter.CheerFor(cheerDuration, cheerLines, intensity);
        yield return new WaitForSeconds(cheerDuration);
    }

    // Blobs's demand
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

        // Marsh next to the throne
        if (marshThroneSpot != null)
            TeleportMarsh(marshThroneSpot.position, faceDirectionAtThrone);

        // Camera
        if (followCamera == null) followCamera = FindFirstObjectByType<CinemachineCamera>();
        if (followCamera != null && finalCameraTarget != null)
        {
            followCamera.Target.TrackingTarget = finalCameraTarget;
            followCamera.PreviousStateIsValid = false;
        }

        if (chatter != null) chatter.SetMembers(null);
        yield return SpawnCrowd(audienceCrowd, audienceFaceTarget, playSpawnSound: false, instant: true, animate: false);
        if (ChefWasSpared && puffsAudience != null)
            yield return SpawnCrowd(new[] { puffsAudience }, audienceFaceTarget, playSpawnSound: false, instant: true, animate: false);

        yield return new WaitForSeconds(pauseWhileBlack);
    }

    private static void PlayMusic(AudioClip clip, float volume, float fadeIn)
    {
        if (clip == null) return;
        AudioManager.Instance?.FadeInMusic(clip, fadeIn, volume);
    }

    // The Chef counts as spared unless the player finished him off
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

            // Cage drops behind everything so Blobs can walk out in front of it
            if (!string.IsNullOrEmpty(cageOpenSortingLayer))
                cageRenderer.sortingLayerName = cageOpenSortingLayer;
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