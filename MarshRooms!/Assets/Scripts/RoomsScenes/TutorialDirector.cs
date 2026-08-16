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
    [SerializeField] private DialogueSequence crashDialogue;
    [SerializeField] private DialogueSequence enemyBurstDialogue;
    [SerializeField] private DialogueSequence minigunDropDialogue;
    [SerializeField] private DialogueSequence sporeFullDialogue;
    [SerializeField] private DialogueSequence wave3StartDialogue;
    [SerializeField] private DialogueSequence postWave3Dialogue1;
    [SerializeField] private DialogueSequence postWave3Dialogue2;
    [SerializeField] private DialogueSequence evilChefDialogue;
    [SerializeField] private DialogueSequence marshEndDialogue;
    [SerializeField] private DialogueSequence tutorialDeathDialogue;

    [Header("Mr Blobs Dialogue")]
    [SerializeField] private DialogueSequence blobsPreToiletDialogue;
    [SerializeField] private DialogueSequence blobsToiletDialogue;
    [SerializeField] private DialogueSequence blobsPostToiletDialogue;

    [Header("Positioning")]
    [SerializeField] private Transform bedExitPosition;
    [SerializeField] private Collider2D bedCollider;
    [SerializeField] private float walkOffBedSpeed;
    [SerializeField] private Transform toiletInteractSpot;
    [SerializeField] private float walkToToiletSpeed;
    [SerializeField] private Transform blobsMoveSpot;
    [SerializeField] private Transform marshFinalSpot;
    [SerializeField] private Transform[] chefPuffsMoveSpot;

    // -- Scene Objects --
    [Header("Refereces")]
    [SerializeField] private ToiletHealth toiletHealth;
    [SerializeField] private GameObject toilet;
    [SerializeField] private Collider2D mrBlobsCollider;
    [SerializeField] private GameObject mrBlobs;
    [SerializeField] private MrBlobsAnimator mrBlobsAnimator;
    [SerializeField] private SpriteFader mrBlobsFader;
    [SerializeField] private GameObject plungerPickup;
    [SerializeField] private GameObject minigunPickup;
    [SerializeField] private GameObject[] chefPuffs;

    // -- Scene Objects --
    [Header("Interaction")]
    [SerializeField] private Interactable toiletInteractable;
    [SerializeField] private Interactable mrBlobsInteractable;

    // -- Prompts --
    [Header("Prompts")]
    [SerializeField] private GameObject wasdPrompt;
    [SerializeField] private float wasdPromptDelay = 3f;  
    [SerializeField] private SpriteFader wasdPromptFader;

    [SerializeField] private GameObject shootPrompt;
    [SerializeField] private SpriteFader shootPromptFader;

    [SerializeField] private GameObject rightClickPrompt;
    [SerializeField] private SpriteFader rightClickPromptFader;

    [SerializeField] private GameObject scrollPrompt;
    [SerializeField] private SpriteFader scrollPromptFader;

    [SerializeField] private GameObject dodgePrompt;
    [SerializeField] private SpriteFader dodgePromptFader;

    [SerializeField] private GameObject toiletArrowPrompt;
    [SerializeField] private SpriteFader toiletArrowFader;

    // -- Enemy Waves --

    [Header("Ambush")]
    [SerializeField] private GameObject[] wave1Enemies;
    [SerializeField] private Transform[] wave1MoveSpots;

    [SerializeField] private GameObject[] wave2Enemies;
    [SerializeField] private GameObject[] wave3Enemies;
    [SerializeField] private GameObject[] wave4Enemies;

    [Header("Wave Spawn Timing")]
    [SerializeField] private float spawnStaggerMin = 0.4f;
    [SerializeField] private float spawnStaggerMax = 0.9f;

    // ── Audio ────────────────────────────────────────────────────────
    [Header("Audio")]
    [SerializeField] private AudioClip houseMusicClip;
    [Range(0f, 1f)] public float houseMusicVolume;
    [SerializeField] private AudioClip toiletUnclogAttemptClip;
    [Range(0f, 1f)] public float toiletUnclogAttemptVolume;
    [SerializeField] private AudioClip triggerClickClip;
    [Range(0f, 1f)] public float triggerClickVolume;
    [SerializeField] private AudioClip crashBangClip;
    [Range(0f, 1f)] public float crashBangVolume;
    [SerializeField] private AudioClip doorPoundClip;
    [Range(0f, 1f)] public float doorPoundVolume;
    [SerializeField] private AudioClip fightMusicClip;
    [Range(0f, 1f)] public float fightMusicVolume;

    [Header("Toilet Fail SFX Timing")]
    [SerializeField] private float toiletSfxInitialPause = 0.4f;
    [SerializeField] private float toiletSfxMidPause = 0.5f;
    [SerializeField] private float toiletSfxRapidInterval = 0.15f;

    [Header("Trigger Discovery SFX Timing")]
    [SerializeField] private float triggerClickPause = 0.5f;
    [SerializeField] private float triggerClickGap = 0.4f;

    // -- Script References --
    private Transform playerTransform;
    private PlayerMover playerMover;
    private BaseMover playerBaseMover;
    private PlayerShooter playerShooter;
    private PlayerInput playerInput;
    private PlayerAimer playerAimer;
    private PlayerHealth playerHealth;
    private PlayerSporeActivator playerSporeActivator;

    // -- Internal State --
    private bool plungerPickedUp = false;
    private bool toiletInteracted = false;
    private bool toiletDead = false;
    private bool blobsPostToiletInteracted = false;
    private bool wave1Clear = false;
    private bool wave2Clear = false;
    private bool wave3Clear = false;
    private bool wave4Clear = false;
    private bool minigunPickedUp = false;
    private bool weaponScrolled = false;
    private bool playerDodged = false;
    private bool sporeCollectedThisWave = false;
    private bool mutateActivated = false;
    private bool shootingEnabled = false;

    private Vector2 lastAmbushDeathPosition;
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
            playerAimer = player.GetComponent<PlayerAimer>();
            playerBaseMover = player.GetComponent<BaseMover>();
            playerHealth = player.GetComponent<PlayerHealth>();
            playerSporeActivator = player.GetComponent<PlayerSporeActivator>();

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
            playerAimer = player.GetComponent<PlayerAimer>();
            playerBaseMover = player.GetComponent<BaseMover>();
            playerHealth = player.GetComponent<PlayerHealth>();
            playerSporeActivator = player.GetComponent<PlayerSporeActivator>();
        }
        
        shootPrompt?.SetActive(false);
        wasdPrompt?.SetActive(false);
        scrollPrompt?.SetActive(false);
        dodgePrompt?.SetActive(false);
        rightClickPrompt?.SetActive(false);
        toiletArrowPrompt?.SetActive(false);
        minigunPickup?.SetActive(false);

        // Hide waves
        SetWaveActive(wave1Enemies, false);
        SetWaveActive(wave2Enemies, false);
        SetWaveActive(wave3Enemies, false);
        SetWaveActive(wave4Enemies, false);

        // hide HUD until fight begins
        HUDManager.Instance?.SetHUDVisible(false);

        // Fade out menu music
        AudioManager.Instance.FadeMusicVolume(0f, 1.5f);
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
        yield return new WaitForSeconds(2f);
        ScreenEffects.Instance?.FadeFromBlack(3.5f);
        yield return new WaitForSeconds(2f);

        // Fade in house music
        AudioManager.Instance?.FadeInMusic(houseMusicClip, 2.5f, houseMusicVolume);
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
        playerBaseMover?.SetSpeed(playerBaseMover.OriginalSpeed * 0.8f);
        playerBaseMover?.DirectionalAnimator?.SetAnimationSpeed(0.8f);
        playerSporeActivator?.SetCanActivate(false);
        playerShooter.SetCanShoot(false);
        playerMover.canDodge = false;

        // ==== PART 2: WASD hint ====
        Coroutine hintRoutine = StartCoroutine(WasdHintRoutine());

        // Wait for player to actually move
        yield return StartCoroutine(WaitForPlayerToMove());

        // Stop hint routine first
        StopCoroutine(hintRoutine);

        // Only fade out if it was actually visible
        if (wasdPromptFader != null && wasdPromptFader.gameObject.activeSelf)
        {
            wasdPromptFader.FadeOut();
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
        yield return StartCoroutine(WalkToPoint(toiletInteractSpot));
        playerAimer?.SetAimOverride(Vector2.up);

        yield return StartCoroutine(ToiletFailSfxSequence());
        yield return StartCoroutine(PlayDialogue(toiletFailDialogue));

        // Discover trigger
        yield return StartCoroutine(TriggerClickSfxSequence());
        if (toiletInteractable != null) toiletInteractable.enabled = false;

        // ==== PART 5: Teach shooting ====
        yield return StartCoroutine(PlayDialogue(toiletShootDialogue));
        playerAimer?.ClearAimOverride();
        yield return new WaitForSeconds(0.4f);

        shootPrompt?.SetActive(true);
        toiletArrowPrompt?.SetActive(true);

        shootPromptFader?.FadeIn();
        toiletArrowFader?.FadeIn();

        playerShooter?.SetCanShoot(true);

        // Wait until player lands a shot on the toilet or time out
        yield return StartCoroutine(WaitForConditionOrTimeout(() => shootingEnabled, 6f));

        //if (shootingEnabled)
        //{
        //    toiletArrowFader?.FadeOut();
        //    toiletArrowPrompt?.SetActive(false);
        //}

        shootPromptFader?.FadeOut();
        yield return new WaitForSeconds(0.3f);
        shootPrompt?.SetActive(false);


        // ==== PART 6: Wait for toilet to die ====
        yield return StartCoroutine(WaitUntil(() => toiletDead));
        yield return StartCoroutine(PlayDialogue(toiletWinDialogue));


        // ==== PART 7: Mr Blobs Moves + Bonding dialogue with Blobs ====
        CurrentStage = TutorialStage.PostToilet;
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(MoveMrBlobsToSpot(blobsMoveSpot, 5f));
        yield return StartCoroutine(WaitUntil(() => blobsPostToiletInteracted));
        // Lock player and no talking to Mr Blobs
        mrBlobsInteractable?.SetInteractionLocked(true);
        if (mrBlobsInteractable != null) mrBlobsInteractable.enabled = false;

        // ==== PART 8: Crash — enemies burst in ====
        LockPlayer();
        LockPlayerKeepInput();
        playerAimer?.SetAimOverride(Vector2.down);
        yield return new WaitForSeconds(0.2f);

        // First door bang
        AudioManager.Instance?.PlaySFX(doorPoundClip, doorPoundVolume);
        for(int i = 0; i < 5; i++)
        {
            ScreenEffects.Instance?.ShakeScreen(0.1f);
            yield return new WaitForSeconds(0.25f);
        }
        AudioManager.Instance?.FadeOutMusic(1f);

        mrBlobsAnimator.Hide();
        yield return new WaitForSeconds(0.3f);
        
        // What the shroom?
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(PlayDialogue(crashDialogue));
        yield return new WaitForSeconds(1f);

        // Second bang - fade to black
        AudioManager.Instance?.PlaySFX(crashBangClip, crashBangVolume);
        ScreenEffects.Instance?.ShakeScreen(0.7f);
        ScreenEffects.Instance?.FadeToBlack(0.35f);
        yield return new WaitForSeconds(1f);

        // ==== PART 9: Wave 1 ====
        SetWaveActive(wave1Enemies, true);
        TrackAmbushDeaths(wave1Enemies);
        SubscribeWaveClear(() => wave1Clear = true);

        ScreenEffects.Instance?.FadeFromBlack(0.5f);
        AudioManager.Instance?.FadeInMusic(fightMusicClip, 2f, fightMusicVolume);
        
        // Enemies walk in
        yield return StartCoroutine(MoveEnemiesToSpots(wave1Enemies, wave1MoveSpots));

        // Enemy Dialogue
        yield return StartCoroutine(PlayDialogue(enemyBurstDialogue));

        // Make HUD visible here
        HUDManager.Instance?.SetHUDVisible(true);
        playerHealth.UpdateHUD();

        // Restore full player speed/animation/dodge now that real combat begins
        playerBaseMover?.SetSpeed(playerBaseMover.OriginalSpeed);
        playerBaseMover?.DirectionalAnimator?.SetAnimationSpeed(1f);
        if (playerMover != null) playerMover.canDodge = true;
        UnlockPlayer();
        UnlockPlayerFull();
        playerShooter?.SetCanShoot(true);
        
        // Show dodge hint
        StartCoroutine(DodgeHintRoutine());

        // Hand control to their AI + start the actual fight
        foreach (var enemy in wave1Enemies)
        {
            if (enemy == null) continue;

            var ai = enemy.GetComponent<IEnemyAI>();
            if (ai != null)
            {
                ai.SkipSpawnGrace();
                ((Behaviour)ai).enabled = true;
            }
            yield return new WaitForSeconds(2.6f);
        }

        // Wait for player to defeat wave
        yield return StartCoroutine(WaitUntil(() => wave1Clear));

        // ==== PART 10: Minigun drop + learn weapon switching ====
        
        // Minigun drop (force to be at last position the enemy died)
        if (minigunPickup != null)
        {
            minigunPickup.transform.position = lastAmbushDeathPosition;
            minigunPickup.SetActive(true);
        }

        // Wait for player to pick up the weapon and then fire dialogue
        yield return StartCoroutine(WaitUntil(() => minigunPickedUp));
        yield return StartCoroutine(PlayDialogue(minigunDropDialogue));

        // Show scroll hint, wait for weapon switch
        scrollPrompt?.SetActive(true);
        scrollPromptFader?.FadeIn();

        // Wait for player to scroll
        yield return StartCoroutine(WaitForConditionOrTimeout(() => weaponScrolled, 6f));

        scrollPromptFader?.FadeOut();
        yield return new WaitForSeconds(0.3f);
        scrollPrompt?.SetActive(false);

        yield return new WaitForSeconds(1f);

        // ==== PART 11: Wave 2 + spore teaching ====

        yield return StartCoroutine(SetWaveActiveStaggered(wave2Enemies, true));
        SubscribeWaveClear(() => wave2Clear = true);

        // Wait for wave 2 to clear
        yield return StartCoroutine(WaitUntil(() => wave2Clear));

        sporeCollectedThisWave = SporeManager.Instance != null && SporeManager.Instance.IsFull;
        SporeManager.Instance.OnSporeCountChanged += HandleSporeCountChanged;

        // Wait for the player to pick up a spore, but don't wait forever if something goes wrong
        yield return StartCoroutine(WaitForConditionOrTimeout(() => sporeCollectedThisWave, 8f));

        SporeManager.Instance.OnSporeCountChanged -= HandleSporeCountChanged;

        // Small beat, then fill + fire the reveal line
        yield return new WaitForSeconds(0.3f);
        SporeManager.Instance?.FillToMax();
        playerAimer?.SetAimOverride(Vector2.down);
        yield return StartCoroutine(PlayDialogue(sporeFullDialogue));
        yield return new WaitForSeconds(0.8f);

        //  ==== Part 12: Wave 3 + mutate teaching ==== 
        foreach (var enemy in wave3Enemies)
            enemy?.GetComponent<EnemyController>()?.SetExternalAIControl(true);

        LockPlayerKeepInput();

        yield return StartCoroutine(SetWaveActiveStaggered(wave3Enemies, true));
        SubscribeWaveClear(() => wave3Clear = true);

        // Keep AI disabled so they just stand there, until mutation
        foreach (var enemy in wave3Enemies)
        {
            var ai = enemy?.GetComponent<IEnemyAI>();
            if (ai != null) ((Behaviour)ai).enabled = false;
        }

        yield return StartCoroutine(PlayDialogue(wave3StartDialogue));

        playerSporeActivator?.SetCanActivate(true);
        rightClickPrompt?.SetActive(true);
        rightClickPromptFader?.FadeIn();

        yield return StartCoroutine(WaitForConditionOrTimeout(() => mutateActivated, 6f));

        rightClickPromptFader?.FadeOut();
        yield return new WaitForSeconds(0.3f);
        rightClickPrompt?.SetActive(false);

        UnlockPlayerFull();

        // Mutation is live release enemies and player together
        foreach (var enemy in wave3Enemies)
        {
            if (enemy == null) continue;

            var ai = enemy.GetComponent<IEnemyAI>();
            if (ai != null)
            {
                ai.SkipSpawnGrace();
                ((Behaviour)ai).enabled = true;
            }

            yield return new WaitForSeconds(Random.Range(0.8f, 1.3f));

        }

        yield return StartCoroutine(WaitUntil(() => wave3Clear));

        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(SetWaveActiveStaggered(wave4Enemies, true));
        SubscribeWaveClear(() => wave4Clear = true);
        yield return StartCoroutine(WaitUntil(() => wave4Clear));

        // -- Part 13: Post-fight + evil chef --
        yield return new WaitForSeconds(4f);

        LockPlayer();
        LockPlayerKeepInput();
        yield return StartCoroutine(PlayDialogue(postWave3Dialogue1));

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(WalkToPoint(marshFinalSpot));
        playerAimer?.SetAimOverride(Vector2.right);
        
        mrBlobsAnimator.Unhide();
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(PlayDialogue(postWave3Dialogue2));

        yield return new WaitForSeconds(1f);
        
        // Spawn Cheff Puffs
        yield return StartCoroutine(SetWaveActiveStaggered(chefPuffs, true));
        yield return new WaitForSeconds(0.5f);

        mrBlobsAnimator?.FlipFacing();
        yield return new WaitForSeconds(0.2f);
        mrBlobsAnimator.Hide();
        yield return new WaitForSeconds(0.5f);
        
        // Steal snail
        yield return StartCoroutine(MoveEnemiesToSpots(chefPuffs, chefPuffsMoveSpot));
        mrBlobsFader?.FadeOut(0.2f);
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(PlayDialogue(evilChefDialogue));
        yield return new WaitForSeconds(0.5f);
        
        // Despawn animation, then destroy
        Animator chefAnim = chefPuffs[0]?.GetComponentInChildren<Animator>();
        if (chefAnim != null)
        {
            chefAnim.SetTrigger("Despawn");
            yield return new WaitForSeconds(1.2f);
        }

        if (chefPuffs[0] != null)
            Destroy(chefPuffs[0]);

        // -- Part 13: Final scene --
        AudioManager.Instance?.FadeOutMusic(1.5f);
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(WalkToPoint(blobsMoveSpot));
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(PlayDialogue(marshEndDialogue));
        
        yield return new WaitForSeconds(0.5f);
        ScreenEffects.Instance?.FadeToBlack(0.6f);
        yield return new WaitForSeconds(2f);
        UnlockPlayer();
        UnlockPlayerFull();
        LevelLoader.Instance.LoadLevel("Floor_01");
    }

    // ======================== HELPERS ========================

    // -- LOCK PLAYER --
    private void LockPlayer()
    {
        playerShooter.HideWeapon(true);
        playerMover?.ForceIdleAnimation();
        playerBaseMover?.StopMovement();
        if (playerInput != null) playerInput.enabled = false;
        if (playerMover != null) playerMover.enabled = false;
        if (playerAimer != null) playerAimer.enabled = false;
    }

    // -- UNLOCK PLAYER --
    private void UnlockPlayer()
    {   
        playerShooter.HideWeapon(false);
        if (playerMover != null) playerMover.enabled = true;
        if (playerInput != null) playerInput.enabled = true;
        if (playerAimer != null) playerAimer.enabled = true;
        playerAimer?.ClearAimOverride();
    }

    // -- PARTIAL LOCK --
    private void LockPlayerKeepInput()
    {   
        playerShooter.HideWeapon(true);
        playerMover?.ForceIdleAnimation();
        playerBaseMover?.StopMovement();
        playerMover?.SetCanMove(false);
        if (playerMover != null) playerMover.enabled = false;
        if (playerAimer != null) playerAimer.enabled = false;
        if (playerMover != null) playerMover.canDodge = false;
        playerShooter?.SetCanShoot(false);
    }

    // -- PARTIAL UNLOCK --
    private void UnlockPlayerFull()
    {
        playerShooter.HideWeapon(false);
        if (playerMover != null) playerMover.enabled = true;
        playerMover?.SetCanMove(true);
        if (playerAimer != null) playerAimer.enabled = true;
        if (playerMover != null) playerMover.canDodge = true;
        playerShooter?.SetCanShoot(true);
        playerAimer?.ClearAimOverride();
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
        if (bedCollider != null)
        {
            bedCollider.enabled = true;
            bedCollider.excludeLayers = 0;
        }
    }

    // -- WALK TO POINT -- 
    private IEnumerator WalkToPoint(Transform target, float timeout = 2f)
    {
        if (playerMover != null) playerMover.enabled = true;
        if (playerAimer != null) playerAimer.enabled = false;

        float elapsed = 0f;

        while (Vector2.Distance(playerTransform.position, target.position) > 0.05f && elapsed < timeout)
        {
            Vector2 dir = ((Vector2)target.position - (Vector2)playerTransform.position).normalized;
            playerBaseMover?.SetMoveInput(dir);
            playerBaseMover?.SetFacingOverride(dir);
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerBaseMover?.SetMoveInput(Vector2.zero);
        playerBaseMover?.ClearFacingOverride();
        playerTransform.position = target.position;

        playerBaseMover?.FaceDirection(Vector2.up);
        playerMover?.ForceIdleAnimation();
        if (playerAimer != null) playerAimer.enabled = true;
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
                if (hintVisible) wasdPromptFader?.FadeOut();
                yield break;
            }

            timer += Time.deltaTime;
            if (timer >= wasdPromptDelay && !hintVisible)
            {
                hintVisible = true;
                wasdPromptFader?.FadeIn();
            }

            yield return null;
        }
    }

    // -- MOVE MR BLOBS --
    private IEnumerator MoveMrBlobsToSpot(Transform target, float duration)
    {
        if (mrBlobs == null || target == null) yield break;

        mrBlobsInteractable?.SetInteractionLocked(true);
        if (mrBlobsInteractable != null) mrBlobsInteractable.enabled = false;
        if (mrBlobsCollider != null) mrBlobsCollider.enabled = false;

        Vector3 start = mrBlobs.transform.position;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            mrBlobs.transform.position = Vector3.Lerp(start, target.position, t / duration);
            yield return null;
        }
        mrBlobs.transform.position = target.position;

        mrBlobsInteractable?.SetInteractionLocked(false);
        if (mrBlobsInteractable != null) mrBlobsInteractable.enabled = true;
        if (mrBlobsCollider != null) mrBlobsCollider.enabled = true;
    }

    private IEnumerator MoveEnemiesToSpots(GameObject[] enemies, Transform[] spots)
    {
        EnemyMover[] movers = new EnemyMover[enemies.Length];

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            var ai = enemy.GetComponent<IEnemyAI>() as Behaviour;
            if (ai != null) ai.enabled = false;

            EnemyMover mover = enemy.GetComponent<EnemyMover>();
        }

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null) continue;
            movers[i] = enemies[i].GetComponent<EnemyMover>();
            movers[i].SetSpeed(movers[i].OriginalSpeed * 2f);
        }

        bool[] arrived = new bool[enemies.Length];

        while (true)
        {
            bool allArrived = true;

            for (int i = 0; i < enemies.Length; i++)
            {
                if (arrived[i] || enemies[i] == null || spots[i] == null) continue;

                Vector2 pos = enemies[i].transform.position;
                Vector2 targetPos = spots[i].position;

                if (Vector2.Distance(pos, targetPos) <= 0.1f)
                {
                    movers[i].Stop();
                    movers[i].SetSpeed(movers[i].OriginalSpeed);
                    arrived[i] = true;
                }
                else
                {
                    movers[i].Move((targetPos - pos).normalized);
                    allArrived = false;
                }
            }

            if (allArrived) break;
            yield return null;
        }
    }

    // -- TOILET FAIL SFX SEQUENCE --
    private IEnumerator ToiletFailSfxSequence()
    {
        LockPlayer();
        playerShooter.HideWeapon(false);
        toiletInteractable?.SetInteractionLocked(true);

        yield return new WaitForSeconds(toiletSfxInitialPause);

        AudioManager.Instance?.PlaySFXWithPitch(toiletUnclogAttemptClip, toiletUnclogAttemptVolume, 0.1f);
        toiletHealth?.PlayHitAnimation();
        playerShooter.SquishEffect();
        yield return new WaitForSeconds(toiletSfxMidPause);

        AudioManager.Instance?.PlaySFXWithPitch(toiletUnclogAttemptClip, toiletUnclogAttemptVolume, 0.1f);
        toiletHealth?.PlayHitAnimation();
        playerShooter.SquishEffect();
        yield return new WaitForSeconds(toiletSfxMidPause);

        for (int i = 0; i < 3; i++)
        {
            AudioManager.Instance?.PlaySFXWithPitch(toiletUnclogAttemptClip, toiletUnclogAttemptVolume, 0.1f);
            toiletHealth?.PlayHitAnimation();
            playerShooter.SquishEffect();
            yield return new WaitForSeconds(toiletSfxRapidInterval);
        }

        UnlockPlayer();
    }

    // -- TRIGGER DISCOVERY SFX SEQUENCE --
    private IEnumerator TriggerClickSfxSequence()
    {
        LockPlayer();
        playerShooter.HideWeapon(false);
        yield return new WaitForSeconds(triggerClickPause);

        AudioManager.Instance?.PlaySFXWithPitch(triggerClickClip, triggerClickVolume, 0.1f);
        yield return new WaitForSeconds(triggerClickGap);
        
        AudioManager.Instance?.PlaySFXWithPitch(triggerClickClip, triggerClickVolume, 0.1f);
        yield return new WaitForSeconds(triggerClickGap);

        UnlockPlayer();
    }

    // -- TRACK LAST AMBUSH --
    private void TrackAmbushDeaths(GameObject[] enemies)
    {
        foreach (var enemy in enemies)
        {
            EnemyHealth health = enemy?.GetComponent<EnemyHealth>();
            if (health != null)
                health.OnDied += (pos) => lastAmbushDeathPosition = pos;
        }
    }

    // -- SET WAVE ACTIVE (STAGGERED) --
    private IEnumerator SetWaveActiveStaggered(GameObject[] wave, bool spawnAnimate)
    {
        if (wave == null) yield break;

        foreach (var enemy in wave)
        {
            if (enemy == null) continue;
            if (spawnAnimate)
                enemy.GetComponent<EnemyController>()?.SetShouldSpawnAnimate();
            enemy.SetActive(true);

            float delay = Random.Range(spawnStaggerMin, spawnStaggerMax);
            yield return new WaitForSeconds(delay);
        }
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

    // -- DODGE HINT ROUTINE --
    private IEnumerator DodgeHintRoutine()
    {
        dodgePrompt?.SetActive(true);
        dodgePromptFader?.FadeIn();

        yield return StartCoroutine(WaitForConditionOrTimeout(() => playerDodged || wave1Clear, 6f));

        dodgePromptFader?.FadeOut();
        yield return new WaitForSeconds(0.3f);
        dodgePrompt?.SetActive(false);
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
    }

    // Activates or deactivates a wave of enemies
    private void SetWaveActive(GameObject[] wave, bool active, bool spawnAnimate = false)
    {
        if (wave == null) return;
        foreach (var enemy in wave)
        {
            if (enemy == null) continue;
            if (active && spawnAnimate)
                enemy.GetComponent<EnemyController>()?.SetShouldSpawnAnimate();
            enemy.SetActive(active);
        }
    }

    // -- SUBSCRIBE WAVE CLEAR --
    private void SubscribeWaveClear(System.Action onClear)
    {
        void Handler()
        {
            EnemyManager.OnAllEnemiesDead -= Handler;
            onClear();
        }
        EnemyManager.OnAllEnemiesDead += Handler;
    }

    // -- WAIT FOR CONDITION OR TIMEOUT --
    private IEnumerator WaitForConditionOrTimeout(System.Func<bool> condition, float timeout)
    {
        float elapsed = 0f;
        while (!condition() && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void HandleSporeCountChanged(int current, int max)
    {
        if (current > 0)
            sporeCollectedThisWave = true;
    }

    // ================================================================
    //  PUBLIC CALLBACKS
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

    public void HandlePlayerDeath(PlayerHealth health)
    {
        StartCoroutine(TutorialDeathRoutine(health));
    }

    private IEnumerator TutorialDeathRoutine(PlayerHealth health)
    {   
        LockPlayer();
        yield return new WaitForSeconds(1.5f);
        yield return StartCoroutine(PlayDialogue(tutorialDeathDialogue));
        yield return new WaitForSeconds(0.3f);
        health.Revive();
        yield return new WaitForSeconds(0.3f);
        UnlockPlayer();
    }

    private IEnumerator HideToiletPromptAfterFade()
    {
        yield return new WaitForSeconds(0.3f); 
        toiletArrowPrompt?.SetActive(false);
    }

    private bool toiletPromptHidden = false;
    public void OnToiletShot()
    {
        shootingEnabled = true;

        if (toiletPromptHidden)
            return;

        toiletPromptHidden = true;

        toiletArrowFader?.FadeOut();
        StartCoroutine(HideToiletPromptAfterFade());
    }
    public void OnToiletDead() => toiletDead = true;
    public void OnBlobsPostToiletInteracted() => blobsPostToiletInteracted = true;
    public void OnWeaponScrolled() => weaponScrolled = true;
    public void OnPlayerDodged() => playerDodged = true;
    public void OnMinigunPickedUp()    => minigunPickedUp = true;
    public void OnMutateActivated()    => mutateActivated = true;
}
