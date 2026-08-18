using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject controlsMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private PlayerInput playerInput;

    [SerializeField] private SliderSoundManager sliderSoundManager;

    [Header("Controls Menu Buttons")]
    [SerializeField] private Button cancelControlsButton;
    [SerializeField] private Button confirmControlsButton;

    [Header("Controls Menu Text")]
    [SerializeField] private TMP_Text cancelText;
    [SerializeField] private TMP_Text confirmText;

    [Header("Button Arrows")]
    [SerializeField] private GameObject resumeArrow;
    [SerializeField] private GameObject optionsArrow;
    [SerializeField] private GameObject mainmenuArrow;
    [SerializeField] private GameObject quitArrow;

    [Header("UI Audio")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [Range(0f, 1f)][SerializeField] private float hoverVolume;
    [Range(0f, 1f)][SerializeField] private float clickVolume;

    [SerializeField] private Color selectedColor = new Color(255f/255f, 255f/255f, 213f/255f, 255f/255f);
    [SerializeField] private Color normalColor = new Color(91f/255f, 59f/255f, 63f/255f, 255f/255f);

    private bool isPaused = false;
    //private float volumeBeforePause = 1f;
    private bool shootingWasEnabledBeforePause = true;
    private InputAction escapeAction;

    private Button[] pauseMenuButtons;
    private GameObject[] menuArrows;
    private Button[] controlsMenuButtons;
    private TMP_Text[] controlsMenuTexts;
    private int currentIndex = -1;


    private void Awake()
    {
        escapeAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
        escapeAction.performed += ctx => HandleEscapePressed();

        pauseMenuButtons = new[] { resumeButton, controlsButton, mainMenuButton, quitButton };
        menuArrows = new[] { resumeArrow, optionsArrow, mainmenuArrow, quitArrow };
        controlsMenuButtons = new[] { cancelControlsButton, confirmControlsButton };
        controlsMenuTexts = new[] { cancelText, confirmText };

        ConfigureNavigation();
        ConfigurePointerEvents();
        ConfigureControlsPointerEvents();
    }

    private void Start()
    {
        // Ensure pause menu is hidden at start
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (controlsMenuPanel != null)
            controlsMenuPanel.SetActive(false);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeGame);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartGame);

        if (controlsButton != null)
            controlsButton.onClick.AddListener(OnOpenControls);

        if (cancelControlsButton != null)
            cancelControlsButton.onClick.AddListener(OnCancelControls);

        if (confirmControlsButton != null)
            confirmControlsButton.onClick.AddListener(OnConfirmControls);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenu);

        // Resume is default highlighted option
        SetSelection(0, false);
    }

    // -- NAVIGATION SETUP --
    private void ConfigureNavigation()
    {
        for (int i = 0; i < pauseMenuButtons.Length; i++)
        {
            if (pauseMenuButtons[i] == null)
                continue;

            Navigation nav = pauseMenuButtons[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = FindPreviousButton(i);
            nav.selectOnDown = FindNextButton(i);
            nav.selectOnLeft = null;
            nav.selectOnRight = null;
            pauseMenuButtons[i].navigation = nav;
        }
    }

    private Button FindPreviousButton(int index)
    {
        for (int i = index - 1; i >= 0; i--)
        {
            if (pauseMenuButtons[i] != null)
                return pauseMenuButtons[i];
        }

        for (int i = pauseMenuButtons.Length - 1; i > index; i--)
        {
            if (pauseMenuButtons[i] != null)
                return pauseMenuButtons[i];
        }

        return pauseMenuButtons[index];
    }

    private Button FindNextButton(int index)
    {
        for (int i = index + 1; i < pauseMenuButtons.Length; i++)
        {
            if (pauseMenuButtons[i] != null)
                return pauseMenuButtons[i];
        }

        for (int i = 0; i < index; i++)
        {
            if (pauseMenuButtons[i] != null)
                return pauseMenuButtons[i];
        }

        return pauseMenuButtons[index];
    }


    // -- POINTER / SELECT EVENTS --
    private void ConfigurePointerEvents()
    {
        for (int i = 0; i < pauseMenuButtons.Length; i++)
        {
            if (pauseMenuButtons[i] == null)
                continue;

            int index = i;

            EventTrigger trigger = pauseMenuButtons[i].GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = pauseMenuButtons[i].gameObject.AddComponent<EventTrigger>();

            if (trigger.triggers == null)
                trigger.triggers = new List<EventTrigger.Entry>();

            AddTrigger(trigger, EventTriggerType.PointerEnter, _ => SetSelection(index, true));
            AddTrigger(trigger, EventTriggerType.Select, _ => SetSelection(index, true));
        }
    }

    private void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityAction<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = type
        };

        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    // -- SELECTION VISUALS --
    private void SetSelection(int index, bool playHoverSound)
    {
        if (index < 0 || index >= pauseMenuButtons.Length)
            return;

        if (pauseMenuButtons[index] == null)
            return;

        bool changed = currentIndex != index;
        currentIndex = index;

        //for (int i = 0; i < menuTexts.Length; i++)
        //{
        //    if (menuTexts[i] != null)
        //        menuTexts[i].color = i == index ? selectedColor : normalColor;
        //}

        for (int i = 0; i < menuArrows.Length; i++)
        {
            if (menuArrows[i] != null)
                menuArrows[i].SetActive(i == index);
        }

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != pauseMenuButtons[index].gameObject)
            EventSystem.current.SetSelectedGameObject(pauseMenuButtons[index].gameObject);

        if (playHoverSound && changed)
            PlayUiSound(hoverClip, hoverVolume);
    }

    private void SetControlsSelection(int index, bool playHoverSound)
    {
        if (index < 0 || index >= controlsMenuButtons.Length)
            return;

        if (controlsMenuButtons[index] == null)
            return;

        bool changed = currentIndex != index;
        currentIndex = index;

        for (int i = 0; i < controlsMenuTexts.Length; i++)
        {
            if (controlsMenuTexts[i] != null)
                controlsMenuTexts[i].color = i == index ? selectedColor : normalColor;
        }

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != controlsMenuButtons[index].gameObject)
            EventSystem.current.SetSelectedGameObject(controlsMenuButtons[index].gameObject);

        if (playHoverSound && changed)
            PlayUiSound(hoverClip, hoverVolume);
    }

    private void ConfigureControlsPointerEvents()
    {
        for (int i = 0; i < controlsMenuButtons.Length; i++)
        {
            if (controlsMenuButtons[i] == null)
                continue;

            int index = i;

            EventTrigger trigger = controlsMenuButtons[i].GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = controlsMenuButtons[i].gameObject.AddComponent<EventTrigger>();

            if (trigger.triggers == null)
                trigger.triggers = new List<EventTrigger.Entry>();

            AddTrigger(trigger, EventTriggerType.PointerEnter, _ => SetControlsSelection(index, true));
            AddTrigger(trigger, EventTriggerType.Select, _ => SetControlsSelection(index, true));
        }
    }

    private void PlayUiSound(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        AudioManager.Instance?.PlaySFXWithPitch(clip, volume);
    }

    private void OnDestroy()
    {
        if (escapeAction != null)
            escapeAction.Dispose();
    }

    // -- HANDLE ESCAPE PRESSED --
    private void HandleEscapePressed()
    {
        MenuManager menuManager = FindFirstObjectByType<MenuManager>();
        if (menuManager != null && menuManager.enabled)
            return;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsRunning)
            return;

        PlayUiSound(clickClip, clickVolume);

        if (controlsMenuPanel != null && controlsMenuPanel.activeInHierarchy)
        {
            OnCloseControls();
            return;
        }

        if (isPaused)
            OnResumeGame();
        else
            OnPauseGame();
    }

    // -- PAUSE GAME --
    private void OnPauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        PlayerShooter shooter = FindPlayerShooter();
        shootingWasEnabledBeforePause = shooter == null || shooter.CanShoot;
        shooter?.SetCanShoot(false);

        //volumeBeforePause = AudioManager.Instance.GetMusicVolume();
        //AudioManager.Instance.SetMusicVolume(volumeBeforePause * 0.2f);
        AudioManager.Instance?.SetMusicDampenMultiplier(0.2f);

        if (playerInput != null) playerInput.enabled = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    private void OnResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        // Only re-enable shooting if it was actually enabled before we
        if (shootingWasEnabledBeforePause)
            FindPlayerShooter()?.SetCanShoot(true);

        PlayUiSound(clickClip, clickVolume);
        //AudioManager.Instance?.SetMusicVolume(volumeBeforePause);
        AudioManager.Instance?.SetMusicDampenMultiplier(1f);

        if (playerInput != null) playerInput.enabled = true;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    private void OnCancelControls()
    {
        PlayUiSound(clickClip, clickVolume);
        sliderSoundManager?.CancelChanges();

        if (controlsMenuPanel != null)
            controlsMenuPanel.SetActive(false);

        if (pauseMenuPanel != null && isPaused)
            pauseMenuPanel.SetActive(true);

        SetSelection(1, false);
    }

    private void OnConfirmControls()
    {
        PlayUiSound(clickClip, clickVolume);
        sliderSoundManager?.ConfirmChanges();

        if (controlsMenuPanel != null)
            controlsMenuPanel.SetActive(false);

        if (pauseMenuPanel != null && isPaused)
            pauseMenuPanel.SetActive(true);

        SetSelection(1, false);
    }

    // -- OPEN CONTROLS --
    private void OnOpenControls()
    {
        //controlsMenuSource = MenuSource.Pause;
        PlayUiSound(clickClip, clickVolume);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (controlsMenuPanel != null)
            controlsMenuPanel.SetActive(true);

        sliderSoundManager?.LoadConfirmedValues();
        SetControlsSelection(0, false);
    }

    // -- CLOSE CONTROLS --
    private void OnCloseControls()
    {
        PlayUiSound(clickClip, clickVolume);

        if (controlsMenuPanel != null)
            controlsMenuPanel.SetActive(false);

        if (pauseMenuPanel != null && isPaused)
            pauseMenuPanel.SetActive(true);
    }

    // -- RESTART GAME --
    private void OnRestartGame()
    {

        // Resume time before reloading
        Time.timeScale = 1f;

        // Get the current active scene 
        Scene currentScene = SceneManager.GetActiveScene();

        if (currentScene.name != "Persistent")
        {
            SceneManager.UnloadSceneAsync(currentScene);
        }
    }

    // -- MAIN MENU --
    private void OnMainMenu()
    {
        LevelLoader.Instance.ReturnToMainMenu();
    }
    
    // -- FIND SHOOTER --
    private PlayerShooter FindPlayerShooter()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<PlayerShooter>() : null;
    }

    // -- ENABLE --
    private void OnEnable()
    {
        if (escapeAction != null)
            escapeAction.Enable();
    }

    // -- DISABLE --
    private void OnDisable()
    {
        if (escapeAction != null)
            escapeAction.Disable();
    }

    // -- QUIT GAME --
    private void OnQuitGame()
    {

        // Resume time before quitting
        Time.timeScale = 1f;

        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                    Application.Quit();
        #endif
    }
}