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

    [Header("Cage")]
    [SerializeField] private SpriteRenderer cageRenderer;
    [SerializeField] private Sprite cageOpenSprite;
    [SerializeField] private string cageOpenSortingLayer = "World";
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
            yield break;
        }

        player = room.Player.transform;
        mover = room.Mover;
        aimer = room.Player.GetComponent<PlayerAimer>();
        shooter = room.Player.GetComponent<PlayerShooter>();

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