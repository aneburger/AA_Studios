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
    [SerializeField] private PlayerInput playerInput;

    private bool isPaused = false;
    private InputAction escapeAction;

    //// Track which menu opened the controls panel
    //private enum MenuSource { Main, Pause }
    //private MenuSource controlsMenuSource = MenuSource.Main;

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
    }

    private void OnDestroy()
    {
        // Clean up the input action
        if (escapeAction != null)
            escapeAction.Dispose();
    }

    // -- HANDLE ESCAPE PRESSED --
    private void HandleEscapePressed()
    {
        // If controls menu is open, close it first
        if (controlsMenuPanel != null && controlsMenuPanel.activeInHierarchy)
        {
            OnCloseControls();
            return;
        }

        // Otherwise toggle pause menu
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

        // Lower music volume
        AudioManager.Instance.SetMusicVolume(0.1f);

        // Disable player controls
        if (playerInput != null) playerInput.enabled = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
    }

    // -- RESUME GAME --
    private void OnResumeGame()
    {
        Debug.Log("Resuming game...");

        isPaused = false;
        Time.timeScale = 1f;

        // Volume back to normal
        AudioManager.Instance.SetMusicVolume(0.5f);

        // Enable player controls
        if (playerInput != null) playerInput.enabled = true;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
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

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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