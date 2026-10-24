using System.Collections;
using UnityEngine;
using TopDown.Movement;

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

    [Header("Crowd (C + D)")]
    [SerializeField] private CrowdChatter chatter;
    [SerializeField] private CrowdMember[] surroundCrowd;
    [SerializeField] private Transform surroundCenter;
    [SerializeField] private float crowdStaggerMin = 0.1f;
    [SerializeField] private float crowdStaggerMax = 0.3f;
    [SerializeField] private float pauseAfterCrowdSpawn = 0.8f;
    [SerializeField] private DialogueSequence surroundDialogue;
    [SerializeField] private CrowdMember speakerMember;
    [SerializeField] private float pauseBeforeSpeakerSpawn = 0.6f;
    [SerializeField] private float pauseAfterSpeakerSpawn = 0.6f;
    [SerializeField] private DialogueSequence redOneDialogue;
    [SerializeField] private string[] hailCheerLines = { "ALL HAIL BLOBS!", "Yayyy!", "Woo!", "All hail Mr. Blobs!" };
    [SerializeField] private float cheerBeforeFade = 4f;
    [SerializeField] private float fadeToBlackDuration = 1.2f;
    [SerializeField] private float blackHoldTime = 1f;

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
        yield return SpawnCrowd(surroundCrowd, surroundCenter);
        mover.SetFacingOverride(Vector2.down);
        yield return new WaitForSeconds(pauseAfterCrowdSpawn);

        yield return PlayDialogue(surroundDialogue);

        // The speaker spawns in after Marsh's reaction, then the dialogue continues
        yield return new WaitForSeconds(pauseBeforeSpeakerSpawn);
        if (speakerMember != null)
            yield return SpawnCrowd(new[] { speakerMember }, surroundCenter);
        yield return new WaitForSeconds(pauseAfterSpeakerSpawn);

        yield return PlayDialogue(redOneDialogue);

        // -- D: crowd cheers with speech bubbles while the screen fades out --
        // The cheering runs in waves until the fade has finished
        if (chatter != null) chatter.CheerFor(cheerBeforeFade + fadeToBlackDuration, hailCheerLines);
        yield return new WaitForSeconds(cheerBeforeFade);

        ScreenEffects.Instance?.FadeToBlack(fadeToBlackDuration);
        yield return new WaitForSeconds(fadeToBlackDuration + blackHoldTime);

        // -- E: time skip goes here next (screen is black) --
    }

    // -- CROWD --
    private IEnumerator SpawnCrowd(CrowdMember[] crowd, Transform faceCenter, bool playSpawnSound = true)
    {
        if (crowd == null) yield break;

        foreach (CrowdMember member in crowd)
        {
            if (member == null) continue;

            // Set facing first (stored while inactive), then Spawn applies it
            if (faceCenter != null) member.FaceToward(faceCenter.position);
            member.Spawn(animate: true, playSound: playSpawnSound);
            if (chatter != null) chatter.AddMember(member);

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