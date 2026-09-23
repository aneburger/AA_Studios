using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TopDown.Movement;

public class BossRoomController : MonoBehaviour
{
    [Header("Gun Room")]
    [SerializeField] private GameObject[] weaponPickupPrefabs;
    [SerializeField] private Transform[] gunSpots;

    [Header("Threshold Walk")]
    [SerializeField] private BossDoorTrigger doorTrigger;
    [SerializeField] private Transform playerStandPoint;
    [SerializeField] private Vector2 faceDirectionAfterWalk = Vector2.up;
    [Range(0.1f, 1f)] [SerializeField] private float walkSpeedScale = 0.5f;
    [SerializeField] private float walkTimeout = 4f;
    [SerializeField] private float arriveDistance = 0.1f;

    [Header("Door")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private GameObject doorBlocker;
    [SerializeField] private float musicFadeDuration = 1.5f;
    [SerializeField] private float silenceBeforeSlam = 0.5f;
    [SerializeField] private float slamSoundDelay = 0.2f;
    [SerializeField] private float postSlamBeat = 0.6f;

    [Header("Door Audio / FX")]
    [SerializeField] private AudioClip doorSlamClip;
    [Range(0f, 1f)] [SerializeField] private float doorSlamVolume = 1f;
    [SerializeField] private float slamShakeForce = 0.5f;

    [Header("Intro")]
    [SerializeField] private BossHealth boss;
    [SerializeField] private float pauseBeforeDialogue = 0.4f;
    [SerializeField] private DialogueSequence introDialogue;
    [SerializeField] private BossIntroCardUI introCard;
    [SerializeField] private float pauseBeforeCard = 0.4f;
    [SerializeField] private float pauseAfterCard = 0.2f;

    [Header("Outro")]
    [SerializeField] private BossOutroSequence outro;
    [SerializeField] private AudioClip doorOpenClip;
    [Range(0f, 1f)] [SerializeField] private float doorOpenVolume = 1f;
    [SerializeField] private Elevator elevator;

    [Header("Boss Music")]
    [SerializeField] private AudioClip bossMusic;
    [Range(0f, 1f)] [SerializeField] private float bossMusicVolume = 0.5f;
    [SerializeField] private float musicFadeInDuration = 1.5f;

    [Header("Boss Health Bar")]
    [SerializeField] private BossHealthBarUI healthBar;
    [SerializeField] private float healthBarFillDuration = 2.5f;

    public event System.Action OnFightStarted;

    private GameObject player;
    private PlayerMover mover;
    private PlayerAimer aimer;
    private PlayerShooter shooter;

    private bool sequenceStarted;
    private bool playerLocked;
    private bool hudHiddenByUs;
    private Coroutine sealRoutine;

    // Expose
    public GameObject Player => player;
    public PlayerMover Mover => mover;
    public BossHealthBarUI HealthBar => healthBar;

    // -- ENABLE / DISABLE --
    private void OnEnable()
    {
        if (doorTrigger != null) doorTrigger.OnPlayerCrossed += BeginSealSequence;
        if (boss != null) boss.OnDied += HandleBossDeath;
        PlayerHealth.OnPlayerDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        if (doorTrigger != null) doorTrigger.OnPlayerCrossed -= BeginSealSequence;
        if (boss != null) boss.OnDied -= HandleBossDeath;
        PlayerHealth.OnPlayerDeath -= HandlePlayerDeath;
    }

    // -- START --
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            mover = player.GetComponent<PlayerMover>();
            aimer = player.GetComponent<PlayerAimer>();
            shooter = player.GetComponent<PlayerShooter>();
        }
    

        if (doorBlocker != null) doorBlocker.SetActive(false);
        if (healthBar != null) healthBar.Hide();
        if (introCard != null) introCard.gameObject.SetActive(false);

        SpawnGunRoomWeapons();
    }

    // -- ON DESTROY --
    private void OnDestroy()
    {
        ReleasePlayer(restoreWeapon: false);
    }

    // ==================== GUN ROOM ====================
    private void SpawnGunRoomWeapons()
    {
        if (gunSpots == null || gunSpots.Length == 0) return;

        List<GameObject> pool = new List<GameObject>();
        if (weaponPickupPrefabs != null)
        {
            foreach (GameObject prefab in weaponPickupPrefabs)
            {
                if (prefab != null && !pool.Contains(prefab)) pool.Add(prefab);
            }
        }

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int count = Mathf.Min(pool.Count, gunSpots.Length);
        for (int i = 0; i < count; i++)
            Instantiate(pool[i], gunSpots[i].position, Quaternion.identity);
    }

    // ==================== DOOR SEQUENCE ====================
    private void BeginSealSequence()
    {
        if (sequenceStarted) return;
        sequenceStarted = true;
        sealRoutine = StartCoroutine(SealSequence());
    }

    private IEnumerator SealSequence()
    {
        if (mover != null)
            yield return new WaitUntil(() => !mover.IsDodging);

        LockPlayer();
        yield return WalkToStandPoint();

        AudioManager.Instance?.FadeOutMusic(musicFadeDuration);
        yield return new WaitForSeconds(musicFadeDuration + silenceBeforeSlam);

        doorAnimator?.SetTrigger("Close");
        yield return new WaitForSeconds(slamSoundDelay);

        AudioManager.Instance?.PlaySFX(doorSlamClip, doorSlamVolume);
        ScreenEffects.Instance?.ShakeScreen(slamShakeForce);
        if (doorBlocker != null) doorBlocker.SetActive(true);

        yield return new WaitForSeconds(postSlamBeat);

        yield return IntroSequence();
        sealRoutine = null;
    }

    private IEnumerator WalkToStandPoint()
    {
        if (player == null || mover == null || playerStandPoint == null) yield break;

        if (aimer != null) aimer.enabled = false;

        float elapsed = 0f;
        while (elapsed < walkTimeout &&
               Vector2.Distance(player.transform.position, playerStandPoint.position) > arriveDistance)
        {
            Vector2 dir = ((Vector2)playerStandPoint.position - (Vector2)player.transform.position).normalized;

            mover.SetMoveInput(dir * walkSpeedScale);

            mover.SetFacingOverride(faceDirectionAfterWalk);
            mover.FaceDirection(faceDirectionAfterWalk);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mover.SetMoveInput(Vector2.zero);
        mover.ClearFacingOverride();
        mover.FaceDirection(faceDirectionAfterWalk);
        mover.ForceIdleAnimation();
    }

    // ==================== INTRO ====================
    private IEnumerator IntroSequence()
    {
        // Dialogue between Boss and Marsh
        yield return new WaitForSeconds(pauseBeforeDialogue);
        yield return PlayDialogue(introDialogue);
        yield return new WaitForSeconds(pauseBeforeCard);

        // Boss music in
        if (bossMusic != null)
            AudioManager.Instance?.FadeInMusic(bossMusic, musicFadeInDuration, bossMusicVolume);

        // VS Overlay
        if (introCard != null)
        {
            SetHUDHidden(true);
            yield return introCard.Play();
            SetHUDHidden(false);
            yield return new WaitForSeconds(pauseAfterCard);
        }

        // Control back
        ReleasePlayer(restoreWeapon: true);

        // Health bar fills. It doubles as the countdown before the boss can act.
        if (healthBar != null && boss != null)
        {
            healthBar.Bind(boss);
            healthBar.Show();
            yield return healthBar.PlayIntroFill(healthBarFillDuration);
        }
        else
        {
            yield return new WaitForSeconds(healthBarFillDuration);
        }

        if (boss != null) boss.SetInvulnerable(BossHealth.ReasonIntro, false);

        OnFightStarted?.Invoke();
    }

    public IEnumerator PlayDialogue(DialogueSequence sequence)
    {
        if (sequence == null || DialogueManager.Instance == null) yield break;

        while (DialogueManager.Instance.IsRunning)
            yield return null;

        bool done = false;
        DialogueManager.Instance.StartDialogue(sequence, () => done = true);
        yield return new WaitUntil(() => done);
    }

    private void SetHUDHidden(bool hidden)
    {
        hudHiddenByUs = hidden;
        HUDManager.Instance?.SetHUDVisible(!hidden);
    }

    // ==================== PLAYER LOCK ====================
    private void LockPlayer()
    {
        if (mover == null || playerLocked) return;

        playerLocked = true;
        if (shooter != null) shooter.HideWeapon(true);
        mover.SetInputLocked(true);
    }

    public void LockPlayerExternally() => LockPlayer();

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
        if (sealRoutine != null)
        {
            StopCoroutine(sealRoutine);
            sealRoutine = null;
        }

        if (hudHiddenByUs) SetHUDHidden(false);

        ReleasePlayer(restoreWeapon: false);
    }

    // ==================== DEATH SEQUENCE ====================
    private void HandleBossDeath()
    {
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {

        if (outro != null)
            yield return outro.Play(this);

        ReleasePlayer(restoreWeapon: true);
        OpenDoor();
        elevator?.OpenManually();
    }

    private void OpenDoor()
    {
        if (doorBlocker != null) doorBlocker.SetActive(false);
        doorAnimator?.SetTrigger("Open");
        AudioManager.Instance?.PlaySFX(doorOpenClip, doorOpenVolume);
    }
}