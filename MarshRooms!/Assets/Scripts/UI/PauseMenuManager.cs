using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PauseMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject controlsMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button closeControlsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private PlayerInput playerInput;

    private bool isPaused = false;
    private float volumeBeforePause = 1f;
    private bool shootingWasEnabledBeforePause = true;
    private InputAction escapeAction;


    private void Awake()
    {
        escapeAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
        escapeAction.performed += ctx => HandleEscapePressed();
        escapeAction.Enable();
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

        if (closeControlsButton != null)
            closeControlsButton.onClick.AddListener(OnCloseControls);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenu);
    }

    private void OnDestroy()
    {
        if (escapeAction != null)
            escapeAction.Dispose();
    }

    // -- HANDLE ESCAPE PRESSED --
    private void HandleEscapePressed()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsRunning)
            return;

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

        volumeBeforePause = AudioManager.Instance.GetMusicVolume();
        AudioManager.Instance.SetMusicVolume(volumeBeforePause * 0.2f);

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

        AudioManager.Instance.SetMusicVolume(volumeBeforePause);

        if (playerInput != null) playerInput.enabled = true;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    // -- OPEN CONTROLS --
    private void OnOpenControls()
    {
        //controlsMenuSource = MenuSource.Pause;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (controlsMenuPanel != null)
            controlsMenuPanel.SetActive(true);
    }

    // -- CLOSE CONTROLS --
    private void OnCloseControls()
    {
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