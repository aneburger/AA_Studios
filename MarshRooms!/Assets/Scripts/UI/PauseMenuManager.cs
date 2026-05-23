using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PauseMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    private bool isPaused = false;
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

        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeGame);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartGame);

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
        if (isPaused)
            OnResumeGame();
        else
            OnPauseGame();
    }

    // -- PAUSE GAME --
    private void OnPauseGame()
    {
        Debug.Log("Game paused...");

        isPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
    }

    // -- RESUME GAME --
    private void OnResumeGame()
    {
        Debug.Log("Resuming game...");

        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    // -- RESTART GAME --
    private void OnRestartGame()
    {
        Debug.Log("Restarting game...");

        // Resume time before reloading
        Time.timeScale = 1f;

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // -- QUIT GAME --
    private void OnQuitGame()
    {
        Debug.Log("Quitting to desktop...");

        // Resume time before quitting
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}