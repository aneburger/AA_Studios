// Sequences every beat of the tutorial in order.

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TopDown.Movement;

public class TutorialDirector : MonoBehaviour
{
    // ── Dialogue Sequences ───────────────────────────────────────────
    [Header("Dialogue")]
    [SerializeField] private DialogueSequence openingDialogue;
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

    // ── Scene Objects ────────────────────────────────────────────────
    [Header("Scene Objects")]
    //[SerializeField] private GameObject wasdHint;               // WASD sprite on the floor
    [SerializeField] private GameObject plungerPickup;          // the plunger interactable
    [SerializeField] private GameObject toilet;                 // toilet enemy/dummy object
    //[SerializeField] private GameObject door;                   // door enemies burst through
    [SerializeField] private GameObject mrBlobs;                // snail object
    //[SerializeField] private GameObject evilChef;              // evil chef prefab/object

    // ── Prompts ──────────────────────────────────────────────────────
    [Header("Prompts")]
    [SerializeField] private GameObject shootPrompt;            // "left click to shoot" UI
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
    [SerializeField] private AudioClip crashBangClip;
    [SerializeField] private AudioClip toiletFlushClip;
    [SerializeField] private AudioClip fightMusicClip;

    // -- References --
    private Transform playerTransform;
    private PlayerMover playerMover;
    private PlayerShooter playerShooter;
    private PlayerInput playerInput;

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

    private void Awake()
    {
        Instance = this;
    }

    // ── START ────────────────────────────────────────────────────────
    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerMover = player.GetComponent<PlayerMover>();
            playerShooter = player.GetComponent<PlayerShooter>();
            playerInput = player.GetComponent<PlayerInput>();
        }
        
        // Hide everything that shouldn't be visible yet
        //shootPrompt?.SetActive(false);
        //rightClickPrompt?.SetActive(false);
        minigunPickup?.SetActive(false);
        //evilChef?.SetActive(false);

        // Disable shooting until taught
        if (playerShooter != null) playerShooter.enabled = false;

        // Hide waves
        SetWaveActive(wave1Enemies, false);
        SetWaveActive(wave2Enemies, false);
        SetWaveActive(wave3Enemies, false);

        StartCoroutine(RunTutorial());
    }

    // -- MASTER SEQUENCE --
    private IEnumerator RunTutorial()
    {  
        yield return null;

        // PART 1: Intro dialogue

        // No moving at start
        if (playerInput != null) playerInput.enabled = false;
        if (playerMover != null) playerMover.enabled = false;
        if (playerShooter != null) playerShooter.enabled = false;

        playerMover?.SetSleeping(true);

        // Fade in from black
        ScreenEffects.Instance?.FadeFromBlack(1.7f);
        yield return new WaitForSeconds(1f);

        // Fade in house music
        AudioManager.Instance?.FadeInMusic(houseMusicClip, 1f);
        yield return new WaitForSeconds(2.5f);

        // Wake marsh up
        playerMover?.SetSleeping(false);
        yield return new WaitForSeconds(0.5f);

        // Opening dialogue
        yield return StartCoroutine(PlayDialogue(openingDialogue));

        // Now let player move
        if (playerMover != null) playerMover.enabled = true;
        if (playerInput != null) playerInput.enabled = true;


        // PART 2: WASD hint
        
    /*

        // ── BEAT 2: WASD hint ────────────────────────────────────────
        wasdHint?.SetActive(true);
        yield return StartCoroutine(WaitForPlayerToMove());
        wasdHint?.SetActive(false);

        // ── BEAT 3: Wait for plunger pickup ──────────────────────────
        yield return StartCoroutine(WaitUntil(() => plungerPickedUp));

        // ── BEAT 4: Player interacts with toilet (fails) ──────────────
        yield return StartCoroutine(WaitUntil(() => toiletInteracted));
        yield return StartCoroutine(PlayDialogue(toiletFailDialogue));

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

    // ================================================================
    //  HELPERS
    // ================================================================

    // Plays dialogue and waits for it to finish
    private IEnumerator PlayDialogue(DialogueSequence sequence)
    {
        if (sequence == null) yield break;
        bool done = false;
        DialogueManager.Instance.StartDialogue(sequence, () => done = true);
        yield return new WaitUntil(() => done);
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

    public void OnPlungerPickedUp()    => plungerPickedUp = true;
    public void OnToiletInteracted()   => toiletInteracted = true;
    public void OnToiletDead()         => toiletDead = true;
    public void OnWave1Clear()         => wave1Clear = true;
    public void OnWave2Clear()         => wave2Clear = true;
    public void OnWave3Clear()         => wave3Clear = true;
    public void OnMinigunPickedUp()    => minigunPickedUp = true;
    public void OnSporeDropped()       => sporeDropped = true;
    public void OnMutateActivated()    => mutateActivated = true;
}
