// Elevator ride screen shown between floors

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using TopDown.Movement;

public class ElevatorScreen : MonoBehaviour
{
    public static ElevatorScreen Instance { get; private set; }

    [Serializable]
    public class ButtonSpriteSet
    {
        public Sprite unlit;
        public Sprite[] flashFrames;
        public Sprite pop;
        public Sprite complete;
    }

    [Serializable]
    public class MarshClip
    {
        public Sprite[] frames;
        public float fps = 8f;
        public bool loop = true;
    }

    [Header("Roots")]
    [SerializeField] private GameObject screenRoot;
    [SerializeField] private RectTransform shakeRoot;

    [Header("Marsh")]
    [SerializeField] private Image marshImage;
    [SerializeField] private MarshClip marshIdle;
    [SerializeField] private MarshClip marshLookUp;
    [SerializeField] private MarshClip marshFacingAway;
    [SerializeField] private MarshClip marshLookAtGuest;

    [Header("Guest Passenger (one-off)")]
    [SerializeField] private Image guestImage;
    [SerializeField] private MarshClip guestIdle;
    [SerializeField] private MarshClip guestLookAtMarsh;
    [SerializeField] private int guestFloor = 4;
    [SerializeField] private string guestSeenId = "elevator_guest_chef";
    [SerializeField] private DialogueSequence guestStareDialogue;
    [SerializeField] private DialogueSequence guestRideDialogue;

    [Header("Guest Timing")]
    [SerializeField] private float guestStareBeat = 0.7f;
    [SerializeField] private float guestLookAwayPause = 0.6f;
    [SerializeField] private float guestRideGlance = 0.5f;
    [SerializeField] private float guestArriveDelay = 0.4f;

    [Header("Doors")]
    [SerializeField] private RectTransform leftDoor;
    [SerializeField] private RectTransform rightDoor;
    [SerializeField] private float doorOpenDistance = 40f;
    [SerializeField] private float doorOpenDuration = 0.8f;

    [Header("Backdrop")]
    [SerializeField] private Image backdropImage;
    [SerializeField] private Sprite[] floorBackdrops;

    [Header("Floor Display")]
    [SerializeField] private Image displayArrow;
    [SerializeField] private Image displayNumber;
    [SerializeField] private Sprite arrowGreen;
    [SerializeField] private Sprite arrowRed;
    [SerializeField] private Sprite[] numberSprites;
    [SerializeField] private int displayBlinkCount = 3;
    [SerializeField] private float displayBlinkInterval = 0.12f;

    [Header("Floor Buttons")]
    [SerializeField] private Image[] floorButtons;
    [SerializeField] private int[] bossFloors = { 6, 12 };
    [SerializeField] private ButtonSpriteSet normalButtons;
    [SerializeField] private ButtonSpriteSet bossButtons;
    [SerializeField] private float buttonFlashFps = 6f;
    [SerializeField] private float buttonPopDuration = 0.15f;
    [SerializeField] private float buttonPopHop = 1f;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float holdBeforeCards = 1f;
    [SerializeField] private float delayBeforePop = 0.4f;
    [SerializeField] private float delayBeforeDisplay = 0.3f;
    [SerializeField] private float rideHoldAfterDisplay = 0.5f;
    [SerializeField] private float dingToDoorDelay = 0.3f;
    [SerializeField] private float fadeStartDelay = 0.4f;
    [SerializeField] private float skipInputDelay = 0.35f;

    [Header("Rumble")]
    [SerializeField] private int rumbleAmount = 1;
    [SerializeField] private float rumbleInterval = 0.05f;
    [SerializeField, Range(0f, 1f)] private float rumbleStrength = 1f;
    [SerializeField] private bool rumbleVerticalOnly = false;

    [Header("Audio")]
    [SerializeField] private AudioClip rumbleClip;
    [Range(0f, 1f)] [SerializeField] private float rumbleVolume = 0.5f;
    [SerializeField] private AudioClip dingClip;
    [Range(0f, 1f)] [SerializeField] private float dingVolume = 0.7f;
    [SerializeField] private AudioClip doorClip;
    [Range(0f, 1f)] [SerializeField] private float doorVolume = 0.6f;
    [SerializeField] private AudioClip buttonPopClip;
    [Range(0f, 1f)] [SerializeField] private float buttonPopVolume = 0.5f;

    private bool isPlaying = false;
    private bool skipRequested = false;
    private float skipEnabledTime = float.MaxValue;

    private Vector2 leftClosedPos;
    private Vector2 rightClosedPos;
    private Vector2 shakeBasePos;
    private Color backdropFlatColor = Color.white;
    private Vector2[] buttonBasePositions;

    private bool rumbling = false;
    private float rumbleTimer = 0f;
    private AudioSource rumbleSource;

    private int flashingFloor = -1;
    private float flashTimer = 0f;

    private MarshClip currentClip;
    private float marshTimer = 0f;

    private bool guestRiding = false;
    private MarshClip currentGuestClip;
    private float guestTimer = 0f;

    // -- AWAKE --
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (leftDoor != null) leftClosedPos = leftDoor.anchoredPosition;
        if (rightDoor != null) rightClosedPos = rightDoor.anchoredPosition;
        if (shakeRoot != null) shakeBasePos = shakeRoot.anchoredPosition;
        if (backdropImage != null) backdropFlatColor = backdropImage.color;

        int count = floorButtons != null ? floorButtons.Length : 0;
        buttonBasePositions = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            if (floorButtons[i] != null)
                buttonBasePositions[i] = floorButtons[i].rectTransform.anchoredPosition;
        }

        if (screenRoot != null)
            screenRoot.SetActive(false);
    }

    // -- ON DESTROY --
    private void OnDestroy()
    {
        AudioManager.Instance?.StopLoopingSFX(ref rumbleSource);
    }

    // -- PLAY --
    public bool Play(int clearedFloor, List<BoonCardData> offers, Action<BoonCardData> onPicked, Action onFinished)
    {
        if (isPlaying) return false;
        if (floorButtons == null || clearedFloor < 1 || clearedFloor + 1 > floorButtons.Length) return false;

        StartCoroutine(PlayRoutine(clearedFloor, offers, onPicked, onFinished));
        return true;
    }

    // -- PLAY ROUTINE --
    private IEnumerator PlayRoutine(int clearedFloor, List<BoonCardData> offers, Action<BoonCardData> onPicked, Action onFinished)
    {
        isPlaying = true;
        skipRequested = false;
        skipEnabledTime = float.MaxValue;

        int destination = clearedFloor + 1;

        guestRiding = guestImage != null
            && clearedFloor == guestFloor
            && (string.IsNullOrEmpty(guestSeenId) || !StoryFlags.Has(guestSeenId));

        SetPlayerLocked(true);

        yield return FadeRoutine(true);
        Time.timeScale = 0f;

        PrepareScreen(clearedFloor);
        if (screenRoot != null) screenRoot.SetActive(true);

        yield return FadeRoutine(false);
        yield return new WaitForSecondsRealtime(holdBeforeCards);

        if (guestRiding)
        {
            SetGuestFacing(true, marshIdle);
            yield return Wait(guestStareBeat);

            yield return PlayGuestDialogue(guestStareDialogue);

            SetGuestFacing(false, marshIdle);
            yield return Wait(guestLookAwayPause);
        }

        // cards slide up, wait for a pick or a skip
        bool picked = false;
        BoonSelectionUI.Instance.Show(
            offers,
            card =>
            {
                onPicked?.Invoke(card);
                picked = true;
            },
            () => picked = true,
            manageGameState: false
        );

        yield return new WaitUntil(() => picked);
        skipEnabledTime = Time.unscaledTime + skipInputDelay;

        yield return RideRoutine(clearedFloor, destination);
        yield return ArriveRoutine();

        // screen is fully black here
        StopRumble();
        if (screenRoot != null) screenRoot.SetActive(false);

        // the guest only rides once per game
        if (guestRiding && !string.IsNullOrEmpty(guestSeenId))
            StoryFlags.Set(guestSeenId);

        guestRiding = false;
        if (guestImage != null) guestImage.gameObject.SetActive(false);

        Time.timeScale = 1f;
        SetPlayerLocked(false);
        isPlaying = false;

        onFinished?.Invoke();
    }

    // -- PREPARE SCREEN --
    private void PrepareScreen(int clearedFloor)
    {
        for (int floor = 1; floor <= floorButtons.Length; floor++)
        {
            Image button = GetButton(floor);
            if (button == null) continue;

            button.rectTransform.anchoredPosition = buttonBasePositions[floor - 1];
            ButtonSpriteSet set = GetSet(floor);

            if (floor < clearedFloor)
                button.sprite = set.complete;
            else if (floor == clearedFloor)
                button.sprite = set.flashFrames != null && set.flashFrames.Length > 0 ? set.flashFrames[0] : set.unlit;
            else
                button.sprite = set.unlit;
        }

        flashingFloor = -1;
        flashTimer = 0f;

        SetDisplayVisible(false);
        SetBackdrop(clearedFloor + 1);

        if (leftDoor != null) leftDoor.anchoredPosition = leftClosedPos;
        if (rightDoor != null) rightDoor.anchoredPosition = rightClosedPos;

        rumbling = false;
        if (shakeRoot != null) shakeRoot.anchoredPosition = shakeBasePos;

        if (guestImage != null)
        {
            guestImage.gameObject.SetActive(guestRiding);
            if (guestRiding) PlayGuest(guestIdle);
        }

        PlayMarsh(marshIdle);
    }

    // -- RIDE ROUTINE --
    private IEnumerator RideRoutine(int clearedFloor, int destination)
    {
        StartRumble();

        // they glance at each other, then quickly look away again
        if (guestRiding && !skipRequested)
        {
            SetGuestFacing(true, marshIdle);
            yield return Wait(guestRideGlance);
            SetGuestFacing(false, marshIdle);
        }

        yield return Wait(delayBeforePop);

        // cleared floor's button pops and turns green
        if (!skipRequested)
            yield return PopRoutine(clearedFloor);
        SetButtonComplete(clearedFloor);

        yield return Wait(delayBeforeDisplay);

        PlayMarsh(guestRiding ? marshIdle : marshLookUp);
        StartButtonFlash(destination);
        SetDisplay(destination);

        if (!skipRequested)
            yield return BlinkDisplayRoutine();
        SetDisplayVisible(true);

        

        yield return Wait(rideHoldAfterDisplay);
    }

    // -- ARRIVE ROUTINE --
    private IEnumerator ArriveRoutine()
    {
        StopRumble();
        AudioManager.Instance?.PlaySFX(dingClip, dingVolume);

        if (guestRiding)
        {
            yield return Wait(guestArriveDelay);

            SetGuestFacing(true, marshIdle);
            yield return Wait(guestStareBeat);

            yield return PlayGuestDialogue(guestRideDialogue);

            SetGuestFacing(false, marshIdle);
        }

        if (!skipRequested)
            yield return Wait(dingToDoorDelay);

        // turns to face the doors as they open
        PlayMarsh(marshFacingAway);
        AudioManager.Instance?.PlaySFX(doorClip, doorVolume);
        StartCoroutine(OpenDoorsRoutine());

        if (!skipRequested)
            yield return Wait(fadeStartDelay);

        yield return FadeRoutine(true);
    }

    // -- SET GUEST FACING --
    private void SetGuestFacing(bool facingEachOther, MarshClip awayClip)
    {
        bool marshCanLook = facingEachOther && marshLookAtGuest != null && marshLookAtGuest.frames != null && marshLookAtGuest.frames.Length > 0;
        bool guestCanLook = facingEachOther && guestLookAtMarsh != null && guestLookAtMarsh.frames != null && guestLookAtMarsh.frames.Length > 0;

        PlayMarsh(marshCanLook ? marshLookAtGuest : awayClip);
        PlayGuest(guestCanLook ? guestLookAtMarsh : guestIdle);
    }

    // -- PLAY GUEST DIALOGUE --
    private IEnumerator PlayGuestDialogue(DialogueSequence sequence)
    {
        if (sequence == null || DialogueManager.Instance == null) yield break;

        while (DialogueManager.Instance.IsRunning)
            yield return null;

        bool done = false;
        DialogueManager.Instance.StartDialogue(sequence, () => done = true, keepGamePaused: true);
        yield return new WaitUntil(() => done);

        // Dialogue events may have unlocked the player; keep them locked for the rest of the ride
        SetPlayerLocked(true);

        // The key that closed the dialogue must not also skip the ride
        if (skipEnabledTime != float.MaxValue)
            skipEnabledTime = Time.unscaledTime + skipInputDelay;
    }

    // -- POP ROUTINE --
    private IEnumerator PopRoutine(int floor)
    {
        Image button = GetButton(floor);
        if (button == null) yield break;

        ButtonSpriteSet set = GetSet(floor);
        Vector2 basePos = buttonBasePositions[floor - 1];

        AudioManager.Instance?.PlaySFX(buttonPopClip, buttonPopVolume);

        if (set.pop != null) button.sprite = set.pop;
        button.rectTransform.anchoredPosition = basePos + Vector2.up * buttonPopHop;

        yield return Wait(buttonPopDuration);

        button.rectTransform.anchoredPosition = basePos;
    }

    // -- BLINK DISPLAY ROUTINE --
    private IEnumerator BlinkDisplayRoutine()
    {
        for (int i = 0; i < displayBlinkCount && !skipRequested; i++)
        {
            SetDisplayVisible(true);
            yield return Wait(displayBlinkInterval);

            SetDisplayVisible(false);
            yield return Wait(displayBlinkInterval);
        }
    }

    // -- OPEN DOORS ROUTINE --
    private IEnumerator OpenDoorsRoutine()
    {
        float elapsed = 0f;

        while (elapsed < doorOpenDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / doorOpenDuration));
            float offset = Mathf.Round(doorOpenDistance * t);

            if (leftDoor != null) leftDoor.anchoredPosition = leftClosedPos + Vector2.left * offset;
            if (rightDoor != null) rightDoor.anchoredPosition = rightClosedPos + Vector2.right * offset;

            yield return null;
        }
    }

    // -- FADE ROUTINE --
    private IEnumerator FadeRoutine(bool toBlack)
    {
        if (ScreenEffects.Instance == null) yield break;

        bool done = false;

        if (toBlack)
            ScreenEffects.Instance.FadeToBlack(fadeDuration, () => done = true);
        else
            ScreenEffects.Instance.FadeFromBlack(fadeDuration, () => done = true);

        yield return new WaitUntil(() => done);
    }

    // -- WAIT --
    private IEnumerator Wait(float seconds)
    {
        float end = Time.unscaledTime + seconds;

        while (Time.unscaledTime < end && !skipRequested)
        {
            PollSkipInput();
            yield return null;
        }
    }

    // -- POLL SKIP INPUT --
    private void PollSkipInput()
    {
        if (Time.unscaledTime < skipEnabledTime) return;

        if (AnyInputPressed())
            skipRequested = true;
    }

    // -- ANY INPUT PRESSED --
    private bool AnyInputPressed()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;

        if (Mouse.current != null &&
            (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
            return true;

        if (Gamepad.current != null &&
            (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame))
            return true;

        return false;
    }

    // -- UPDATE --
    private void Update()
    {
        if (!isPlaying) return;

        float dt = Time.unscaledDeltaTime;

        UpdateMarsh(dt);
        UpdateGuest(dt);
        UpdateButtonFlash(dt);
        UpdateRumble(dt);
    }

    // -- UPDATE MARSH --
    private void UpdateMarsh(float dt)
    {
        if (currentClip == null || currentClip.frames == null || currentClip.frames.Length < 2) return;

        marshTimer += dt;
        ApplyMarshFrame();
    }

    // -- APPLY MARSH FRAME --
    private void ApplyMarshFrame()
    {
        if (marshImage == null || currentClip == null || currentClip.frames == null || currentClip.frames.Length == 0) return;

        int length = currentClip.frames.Length;
        int index = (int)(marshTimer * currentClip.fps);
        index = currentClip.loop ? index % length : Mathf.Min(index, length - 1);

        Sprite sprite = currentClip.frames[index];
        if (sprite == null || marshImage.sprite == sprite) return;

        marshImage.sprite = sprite;
        marshImage.SetNativeSize();
    }

    // -- PLAY MARSH --
    private void PlayMarsh(MarshClip clip)
    {
        currentClip = clip;
        marshTimer = 0f;
        ApplyMarshFrame();
    }

    // -- UPDATE GUEST --
    private void UpdateGuest(float dt)
    {
        if (!guestRiding || currentGuestClip == null || currentGuestClip.frames == null || currentGuestClip.frames.Length < 2) return;

        guestTimer += dt;
        ApplyGuestFrame();
    }

    // -- APPLY GUEST FRAME --
    private void ApplyGuestFrame()
    {
        if (guestImage == null || currentGuestClip == null || currentGuestClip.frames == null || currentGuestClip.frames.Length == 0) return;

        int length = currentGuestClip.frames.Length;
        int index = (int)(guestTimer * currentGuestClip.fps);
        index = currentGuestClip.loop ? index % length : Mathf.Min(index, length - 1);

        Sprite sprite = currentGuestClip.frames[index];
        if (sprite == null || guestImage.sprite == sprite) return;

        guestImage.sprite = sprite;
        guestImage.SetNativeSize();
    }

    // -- PLAY GUEST --
    private void PlayGuest(MarshClip clip)
    {
        currentGuestClip = clip;
        guestTimer = 0f;
        ApplyGuestFrame();
    }

    // -- UPDATE BUTTON FLASH --
    private void UpdateButtonFlash(float dt)
    {
        if (flashingFloor < 0) return;

        Image button = GetButton(flashingFloor);
        Sprite[] frames = GetSet(flashingFloor).flashFrames;
        if (button == null || frames == null || frames.Length == 0) return;

        flashTimer += dt;
        int index = (int)(flashTimer * buttonFlashFps) % frames.Length;
        button.sprite = frames[index];
    }

    // -- START BUTTON FLASH --
    private void StartButtonFlash(int floor)
    {
        flashingFloor = floor;
        flashTimer = 0f;
    }

    // -- UPDATE RUMBLE --
    private void UpdateRumble(float dt)
    {
        if (!rumbling || shakeRoot == null) return;

        rumbleTimer += dt;
        if (rumbleTimer < rumbleInterval) return;
        rumbleTimer = 0f;

        int x = rumbleVerticalOnly ? 0 : RollRumbleOffset();
        int y = RollRumbleOffset();
        shakeRoot.anchoredPosition = shakeBasePos + new Vector2(x, y);
    }

    // -- ROLL RUMBLE OFFSET --
    private int RollRumbleOffset()
    {
        if (UnityEngine.Random.value > rumbleStrength) return 0;

        return UnityEngine.Random.Range(-rumbleAmount, rumbleAmount + 1);
    }

    // -- START RUMBLE --
    private void StartRumble()
    {
        rumbling = true;
        rumbleTimer = 0f;
        AudioManager.Instance?.PlayLoopingSFX(ref rumbleSource, rumbleClip, rumbleVolume);
    }

    // -- STOP RUMBLE --
    private void StopRumble()
    {
        rumbling = false;
        if (shakeRoot != null) shakeRoot.anchoredPosition = shakeBasePos;
        AudioManager.Instance?.StopLoopingSFX(ref rumbleSource);
    }

    // -- SET BUTTON COMPLETE --
    private void SetButtonComplete(int floor)
    {
        Image button = GetButton(floor);
        if (button == null) return;

        if (flashingFloor == floor) flashingFloor = -1;

        button.rectTransform.anchoredPosition = buttonBasePositions[floor - 1];
        button.sprite = GetSet(floor).complete;
    }

    // -- SET DISPLAY --
    private void SetDisplay(int floor)
    {
        if (displayArrow != null)
        {
            displayArrow.sprite = IsBossFloor(floor) ? arrowRed : arrowGreen;
            displayArrow.SetNativeSize();
        }

        if (displayNumber != null && numberSprites != null && floor - 1 < numberSprites.Length)
        {
            displayNumber.sprite = numberSprites[floor - 1];
            displayNumber.SetNativeSize();
        }
    }

    // -- SET BACKDROP --
    private void SetBackdrop(int floor)
    {
        if (backdropImage == null) return;

        Sprite sprite = floorBackdrops != null && floor >= 1 && floor <= floorBackdrops.Length
            ? floorBackdrops[floor - 1]
            : null;

        backdropImage.sprite = sprite;
        backdropImage.color = sprite != null ? Color.white : backdropFlatColor;
    }

    // -- SET DISPLAY VISIBLE --
    private void SetDisplayVisible(bool visible)
    {
        if (displayArrow != null) displayArrow.enabled = visible;
        if (displayNumber != null) displayNumber.enabled = visible;
    }

    // -- GET BUTTON --
    private Image GetButton(int floor)
    {
        if (floorButtons == null || floor < 1 || floor > floorButtons.Length) return null;
        return floorButtons[floor - 1];
    }

    // -- GET SET --
    private ButtonSpriteSet GetSet(int floor)
    {
        return IsBossFloor(floor) ? bossButtons : normalButtons;
    }

    // -- IS BOSS FLOOR --
    private bool IsBossFloor(int floor)
    {
        if (bossFloors == null) return false;

        foreach (int bossFloor in bossFloors)
        {
            if (bossFloor == floor) return true;
        }

        return false;
    }

    // -- SET PLAYER LOCKED --
    private void SetPlayerLocked(bool locked)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        player?.GetComponent<PlayerMover>()?.SetInputLocked(locked);
    }
}