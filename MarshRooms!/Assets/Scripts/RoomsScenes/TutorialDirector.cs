// Sequences every beat of the tutorial in order.

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TopDown.Movement;

public class TutorialDirector : MonoBehaviour
{
    // -- Dialogue Sequences --
    [Header("Dialogue")]
    [SerializeField] private DialogueSequence openingDialogue;
    [SerializeField] private DialogueSequence plungerPickupDialogue;

    [SerializeField] private DialogueSequence toiletFailDialogue;
    [SerializeField] private DialogueSequence toiletShootDialogue;
    [SerializeField] private DialogueSequence toiletWinDialogue;
    [SerializeField] private DialogueSequence breakfastDialogue;
    [SerializeField] private DialogueSequence blobsDialogue;
    [SerializeField] private DialogueSequence enemyBurstDialogue;
    [SerializeField] private DialogueSequence minigunDropDialogue;
    [SerializeField] private DialogueSequence sporeFirstDialogue;
    [SerializeField] private DialogueSequence sporeFullDialogue;
    [SerializeField] private DialogueSequence wave3StartDialogue;
    [SerializeField] private DialogueSequence postWave3Dialogue;
    [SerializeField] private DialogueSequence evilChefDialogue;
    [SerializeField] private DialogueSequence marsEndDialogue;

    [Header("Mr Blobs Dialogue")]
    [SerializeField] private DialogueSequence blobsPreToiletDialogue;
    [SerializeField] private DialogueSequence blobsToiletDialogue;
    [SerializeField] private DialogueSequence blobsPostToiletDialogue;

    [Header("Bed Setup")]
    [SerializeField] private Transform bedExitPosition;
    [SerializeField] private Collider2D bedCollider;
    [SerializeField] private float walkOffBedSpeed;

    // -- Scene Objects --
    [Header("Scene Objects")]
    [SerializeField] private ToiletHealth toiletHealth;
    [SerializeField] private GameObject toilet;
    [SerializeField] private GameObject plungerPickup;
    //[SerializeField] private GameObject door;
    [SerializeField] private GameObject mrBlobs;
    //[SerializeField] private GameObject evilChef;

    // -- Scene Objects --
    [Header("Interaction")]
    [SerializeField] private Interactable toiletInteractable;

    // ── Prompts ──────────────────────────────────────────────────────
    [Header("Prompts")]
    [SerializeField] private GameObject wasdHint;
    [SerializeField] private float wasdHintDelay = 3f;
    [SerializeField] private SpriteFader wasdHintFader;

    [SerializeField] private GameObject shootPrompt;
    [SerializeField] private SpriteFader shootPromptFader;

    [SerializeField] private GameObject rightClickPrompt;       // "right click to mutate" UI

    // ── Enemy Waves ──────────────────────────────────────────────────
    [Header("Enemy Waves")]
    [SerializeField] private GameObject[] wave1Enemies;
    [SerializeField] private GameObject[] wave2Enemies;
    [SerializeField] private GameObject[] wave3Enemies;
    [SerializeField] private GameObject minigunPickup;          // hardcoded minigun drop position

    // ── Audio ────────────────────────────────────────────────────────
    [Header("Audio")]
    [SerializeField] private AudioClip houseMusicClip;
    [Range(0f, 1f)] public float houseMusicVolume;
    [SerializeField] private AudioClip crashBangClip;
    [Range(0f, 1f)] public float crashBangVolume;
    [SerializeField] private AudioClip toiletUnclogAttemptClip;
    [Range(0f, 1f)] public float toiletUnclogAttemptVolume;
    [SerializeField] private AudioClip triggerClickClip;
    [Range(0f, 1f)] public float triggerClickVolume;
    [SerializeField] private AudioClip toiletFlushClip;
    [Range(0f, 1f)] public float toiletFlushtVolume;
    [SerializeField] private AudioClip fightMusicClip;

    [Header("Toilet Fail SFX Timing")]
    [SerializeField] private float toiletSfxInitialPause = 0.4f;
    [SerializeField] private float toiletSfxMidPause = 0.5f;
    [SerializeField] private float toiletSfxRapidInterval = 0.15f;

    [Header("Trigger Discovery SFX Timing")]
    [SerializeField] private float triggerClickPause = 0.5f;
    [SerializeField] private float triggerClickGap = 0.4f;

    // -- References --
    private Transform playerTransform;
    private PlayerMover playerMover;
    private BaseMover playerBaseMover;
    private PlayerShooter playerShooter;
    private PlayerInput playerInput;
    private PlayerAimer playerAimer;

    // ── Internal State ───────────────────────────────────────────────
    private bool plungerPickedUp = false;
    private bool toiletInteracted = false;
    private bool toiletDead = false;
    private bool wave1Clear = false;
    private bool wave2Clear = false;
    private bool wave3Clear = false;
    private bool minigunPickedUp = false;
    private bool sporeDropped = false;
    private bool sporeBarFull = false;
    private bool mutateActivated = false;
    private bool shootingEnabled = false;

    public static TutorialDirector Instance { get; private set; }

    public enum TutorialStage
    {
        HouseExplore,
        ToiletBroken,
        PostToilet,
        Combat
    }

    public TutorialStage CurrentStage { get; private set; } = TutorialStage.HouseExplore;

    // -- AWAKE --
    private void Awake()
    {
        Instance = this;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerMover = player.GetComponent<PlayerMover>();
            playerShooter = player.GetComponent<PlayerShooter>();
            playerInput = player.GetComponent<PlayerInput>();
            playerBaseMover = player.GetComponent<BaseMover>();
            playerAimer = player.GetComponent<PlayerAimer>();

            if (playerInput != null) playerInput.enabled = false;
            if (playerMover != null) playerMover.enabled = false;
            if (playerShooter != null) playerShooter.enabled = false;
        }
    }

    // -- START --
    private void Start()
    {   
        toiletInteractable?.SetInteractionLocked(true);
        if (toiletInteractable != null) toiletInteractable.enabled = false;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerMover = player.GetComponent<PlayerMover>();
            playerShooter = player.GetComponent<PlayerShooter>();
            playerInput = player.GetComponent<PlayerInput>();
            playerBaseMover = player.GetComponent<BaseMover>();
        }
        
        // Hide everything that shouldn't be visible yet
        shootPrompt?.SetActive(false);
        //rightClickPrompt?.SetActive(false);
        //minigunPickup?.SetActive(false);
        //evilChef?.SetActive(false);

        // Hide waves
        SetWaveActive(wave1Enemies, false);
        SetWaveActive(wave2Enemies, false);
        SetWaveActive(wave3Enemies, false);

        StartCoroutine(RunTutorial());
    }

    // ======================== MASTER SEQUENCE ========================

    // -- RUN TUTORIAL --
    private IEnumerator RunTutorial()
    {  
        yield return null;

        // ==== PART 1: Intro dialogue ====
        
        playerMover?.SetSleeping(true);

        // Fade in from black
        ScreenEffects.Instance?.FadeFromBlack(1.5f);
        yield return new WaitForSeconds(1f);

        // Fade in house music
        AudioManager.Instance?.FadeInMusic(houseMusicClip, 0.5f);
        yield return new WaitForSeconds(3f);

        // Wake up
        playerMover?.SetSleeping(false);
        yield return new WaitForSeconds(1f);

        // Walk off bed
        yield return StartCoroutine(WalkOffBed());
        yield return new WaitForSeconds(1f);

        // Opening dialogue
        yield return StartCoroutine(PlayDialogue(openingDialogue));

        // Now let player move 
        if (playerMover != null) playerMover.enabled = true;
        if (playerInput != null) playerInput.enabled = true;

        // Slow movement for chill vibes, no dodging yet
        playerBaseMover?.SetSpeed(playerBaseMover.OriginalSpeed * 0.7f);
        playerBaseMover?.DirectionalAnimator?.SetAnimationSpeed(0.7f);
        playerMover.canDodge = false;

        // ==== PART 2: WASD hint ====
        Coroutine hintRoutine = StartCoroutine(WasdHintRoutine());

        // Wait for player to actually move
        yield return StartCoroutine(WaitForPlayerToMove());

        // Stop hint routine first
        StopCoroutine(hintRoutine);

        // Only fade out if it was actually visible
        if (wasdHintFader != null && wasdHintFader.gameObject.activeSelf)
        {
            wasdHintFader.FadeOut();
            yield return new WaitForSeconds(0.3f);
        }

        // ==== PART 3: House exploration ====

        // Next Dialogue stage
        CurrentStage = TutorialStage.HouseExplore;

        // Wait untl plunger is picked up
        yield return StartCoroutine(WaitUntil(() => plungerPickedUp));

        // Dialogue after plunger picked up
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(PlayDialogue(plungerPickupDialogue));
        
        // Player can't shoot yet
        playerShooter?.SetCanShoot(false);

        // Toilet is now interactable
        toiletInteractable?.SetInteractionLocked(false);
        CurrentStage = TutorialStage.ToiletBroken;

        // ==== PART 4: Interact with toilet - fail ====
        yield return StartCoroutine(WaitUntil(() => toiletInteracted));
        yield return StartCoroutine(ToiletFailSfxSequence());
        yield return StartCoroutine(PlayDialogue(toiletFailDialogue));

        // Discover trigger
        yield return StartCoroutine(TriggerClickSfxSequence());
        if (toiletInteractable != null) toiletInteractable.enabled = false;

        // ==== PART 5: Teach shooting ====
        yield return StartCoroutine(PlayDialogue(toiletShootDialogue));
        yield return new WaitForSeconds(0.4f);

        shootPrompt?.SetActive(true);
        shootPromptFader?.FadeIn();
        playerShooter?.SetCanShoot(true);

        // Wait until player lands a shot on the toilet
        yield return StartCoroutine(WaitUntil(() => shootingEnabled));

        shootPromptFader?.FadeOut();
        yield return new WaitForSeconds(0.3f);
        shootPrompt?.SetActive(false);

        // ==== PART 6: Wait for toilet to die ====
        yield return StartCoroutine(WaitUntil(() => toiletDead));
        yield return StartCoroutine(PlayDialogue(toiletWinDialogue));




        
    /*

    
        // ── BEAT 5: Teach shooting ────────────────────────────────────
        yield return StartCoroutine(PlayDialogue(toiletShootDialogue));
        shootPrompt?.SetActive(true);
        if (playerShooter != null) playerShooter.enabled = true;
        shootingEnabled = true;
        yield return new WaitForSeconds(1f);
        shootPrompt?.SetActive(false);

        // ── BEAT 6: Wait for toilet to die ────────────────────────────
        yield return StartCoroutine(WaitUntil(() => toiletDead));
        yield return StartCoroutine(PlayDialogue(toiletWinDialogue));

        // ── BEAT 7: Breakfast / Mr Blobs bonding ─────────────────────
        yield return StartCoroutine(PlayDialogue(breakfastDialogue));
        yield return StartCoroutine(PlayDialogue(blobsDialogue));

        // ── BEAT 8: CRASH - enemies burst in ─────────────────────────
        AudioManager.Instance?.PlaySFX(crashBangClip);
        // TODO: play door break animation / destroy door object
        door?.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(PlayDialogue(enemyBurstDialogue));

        // Switch to fight music
        AudioManager.Instance?.FadeOutMusic(1f);
        yield return new WaitForSeconds(0.5f);
        AudioManager.Instance?.FadeInMusic(fightMusicClip, 1f);

        // ── BEAT 9: Wave 1 ────────────────────────────────────────────
        SetWaveActive(wave1Enemies, true);
        yield return StartCoroutine(WaitUntil(() => wave1Clear));

        // ── BEAT 10: Minigun drop + weapon switching ──────────────────
        minigunPickup?.SetActive(true);
        yield return StartCoroutine(PlayDialogue(minigunDropDialogue));
        yield return StartCoroutine(WaitUntil(() => minigunPickedUp));

        // ── BEAT 11: Wave 2 + spore teaching ─────────────────────────
        SetWaveActive(wave2Enemies, true);

        // Wait for first enemy kill to drop spores and trigger dialogue
        yield return StartCoroutine(WaitUntil(() => sporeDropped));
        yield return StartCoroutine(PlayDialogue(sporeFirstDialogue));

        // Wait for wave 2 to clear and spore bar to be full
        yield return StartCoroutine(WaitUntil(() => wave2Clear));
        yield return StartCoroutine(EnsureSporeBarFull());
        yield return StartCoroutine(PlayDialogue(sporeFullDialogue));

        // ── BEAT 12: Wave 3 + mutate teaching ────────────────────────
        SetWaveActive(wave3Enemies, true);
        yield return StartCoroutine(PlayDialogue(wave3StartDialogue));

        // Show right click prompt, wait for mutate activation
        rightClickPrompt?.SetActive(true);
        yield return StartCoroutine(WaitUntil(() => mutateActivated));
        rightClickPrompt?.SetActive(false);

        yield return StartCoroutine(WaitUntil(() => wave3Clear));

        // ── BEAT 13: Post-fight + evil chef ──────────────────────────
        yield return StartCoroutine(PlayDialogue(postWave3Dialogue));

        evilChef?.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(PlayDialogue(evilChefDialogue));

        // Evil chef takes Mr Blobs
        mrBlobs?.SetActive(false);
        evilChef?.SetActive(false);

        yield return StartCoroutine(PlayDialogue(marsEndDialogue));

        // ── BEAT 14: Fade to level 1 ──────────────────────────────────
        AudioManager.Instance?.FadeOutMusic(1.5f);
        yield return new WaitForSeconds(0.5f);
        ScreenEffects.Instance?.FadeToBlack(1.5f, () =>
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Level1");
        });*/
    }

    // ======================== HELPERS ========================

    // -- LOCK PLAYER --
    private void LockPlayer()
    {
        playerBaseMover?.StopMovement();
        if (playerInput != null) playerInput.enabled = false;
        if (playerMover != null) playerMover.enabled = false;
        if (playerAimer != null) playerAimer.enabled = false;
    }

    // -- UNLOCK PLAYER --
    private void UnlockPlayer()
    {
        if (playerMover != null) playerMover.enabled = true;
        if (playerInput != null) playerInput.enabled = true;
        if (playerAimer != null) playerAimer.enabled = true;
    }

    // -- WALK OFF BED --
    private IEnumerator WalkOffBed()
    {
        if (playerInput != null) playerInput.enabled = false;
        if (playerMover != null) playerMover.enabled = true;

        Vector2 direction = ((Vector2)bedExitPosition.position - 
                            (Vector2)playerTransform.position).normalized;
        
        playerBaseMover?.SetMoveInput(direction);
        playerBaseMover?.SetFacingOverride(direction);

        while (Vector2.Distance(playerTransform.position, bedExitPosition.position) > 0.05f)
        {
            Vector2 dir = ((Vector2)bedExitPosition.position - (Vector2)playerTransform.position).normalized;
            playerBaseMover?.SetMoveInput(dir);
            playerBaseMover?.SetFacingOverride(dir);
            yield return null;
        }

        // Stop moving
        playerBaseMover?.SetMoveInput(Vector2.zero);
        playerBaseMover?.ClearFacingOverride();
        playerTransform.position = bedExitPosition.position;

        // Lock bed
        if (bedCollider != null) bedCollider.enabled = true;
    }

    // -- WASD HINT --
    private IEnumerator WasdHintRoutine()
    {
        float timer = 0f;
        bool hintVisible = false;

        while (true)
        {
            bool isMoving = Keyboard.current.wKey.isPressed ||
                            Keyboard.current.aKey.isPressed ||
                            Keyboard.current.sKey.isPressed ||
                            Keyboard.current.dKey.isPressed;

            if (isMoving)
            {
                if (hintVisible) wasdHintFader?.FadeOut();
                yield break;
            }

            timer += Time.deltaTime;
            if (timer >= wasdHintDelay && !hintVisible)
            {
                hintVisible = true;
                wasdHintFader?.FadeIn();
            }

            yield return null;
        }
    }

    // -- TOILET FAIL SFX SEQUENCE --
    private IEnumerator ToiletFailSfxSequence()
    {
        LockPlayer();
        toiletInteractable?.SetInteractionLocked(true);

        yield return new WaitForSeconds(toiletSfxInitialPause);

        AudioManager.Instance?.PlaySFXWithPitch(toiletUnclogAttemptClip, toiletUnclogAttemptVolume, 0.1f);
        toiletHealth?.PlayHitAnimation();
        yield return new WaitForSeconds(toiletSfxMidPause);

        AudioManager.Instance?.PlaySFXWithPitch(toiletUnclogAttemptClip, toiletUnclogAttemptVolume, 0.1f);
        toiletHealth?.PlayHitAnimation();
        yield return new WaitForSeconds(toiletSfxMidPause);

        for (int i = 0; i < 3; i++)
        {
            AudioManager.Instance?.PlaySFXWithPitch(toiletUnclogAttemptClip, toiletUnclogAttemptVolume, 0.1f);
            toiletHealth?.PlayHitAnimation();
            yield return new WaitForSeconds(toiletSfxRapidInterval);
        }

        UnlockPlayer();
    }

    // -- TRIGGER DISCOVERY SFX SEQUENCE --
    private IEnumerator TriggerClickSfxSequence()
    {
        LockPlayer();

        yield return new WaitForSeconds(triggerClickPause);

        AudioManager.Instance?.PlaySFXWithPitch(triggerClickClip, triggerClickVolume, 0.1f);
        yield return new WaitForSeconds(triggerClickGap);

        AudioManager.Instance?.PlaySFXWithPitch(triggerClickClip, triggerClickVolume, 0.1f);
        yield return new WaitForSeconds(triggerClickGap);

        UnlockPlayer();
    }

    // -- PLAY DIALOGUE --
    private IEnumerator PlayDialogue(DialogueSequence sequence)
    {
        if (sequence == null) yield break;
        bool done = false;
        DialogueManager.Instance.StartDialogue(sequence, () => done = true);
        yield return new WaitUntil(() => done);
    }

    // -- GET BLOBS DIALOGUE --
    public DialogueSequence GetBlobsDialogue()
    {
        return CurrentStage switch
        {
            TutorialStage.HouseExplore => blobsPreToiletDialogue,
            TutorialStage.ToiletBroken => blobsToiletDialogue,
            TutorialStage.PostToilet   => blobsPostToiletDialogue,
            _                          => null
        };
    }







    // Waits until a condition is true, checking every frame
    private IEnumerator WaitUntil(System.Func<bool> condition)
    {
        yield return new UnityEngine.WaitUntil(condition);
    }

    // Waits for the player to press any movement key
    private IEnumerator WaitForPlayerToMove()
    {
        yield return new WaitUntil(() =>
            Keyboard.current.wKey.isPressed ||
            Keyboard.current.aKey.isPressed ||
            Keyboard.current.sKey.isPressed ||
            Keyboard.current.dKey.isPressed
        );
    }

    // If spore bar isn't full after wave 2, spawn bonus spore pickups
    private IEnumerator EnsureSporeBarFull()
    {
        // Give SporeManager a moment to update
        yield return new WaitForSeconds(0.2f);

        if (SporeManager.Instance != null && !SporeManager.Instance.IsFull)
        {
            // Just fill it directly — tutorial should never leave player stuck
            SporeManager.Instance.FillToMax();
        }

        sporeBarFull = true;
    }

    // Activates or deactivates a wave of enemies
    private void SetWaveActive(GameObject[] wave, bool active)
    {
        if (wave == null) return;
        foreach (var enemy in wave)
            if (enemy != null) enemy.SetActive(active);
    }

    // ================================================================
    //  PUBLIC CALLBACKS
    //  Call these from other scripts when events happen
    // ================================================================

    public void OnPlungerPickedUp()
    {   
        if (plungerPickedUp) return;
        plungerPickedUp = true;
        CurrentStage = TutorialStage.ToiletBroken;              
        if (toiletInteractable != null) toiletInteractable.enabled = true;
    }

    public void OnToiletInteracted()
    {
        toiletInteracted = true;
        toiletInteractable?.SetInteractionLocked(true);
    }

    public void OnToiletShot() => shootingEnabled = true;

    public void OnToiletDead()         => toiletDead = true;
    public void OnWave1Clear()         => wave1Clear = true;
    public void OnWave2Clear()         => wave2Clear = true;
    public void OnWave3Clear()         => wave3Clear = true;
    public void OnMinigunPickedUp()    => minigunPickedUp = true;
    public void OnSporeDropped()       => sporeDropped = true;
    public void OnMutateActivated()    => mutateActivated = true;
}
