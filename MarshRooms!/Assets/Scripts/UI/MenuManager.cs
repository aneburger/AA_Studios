using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject controlsMenuPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button closeControlsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button tutorialButton;

    private void Awake()
    {
        // Ensure menu is visible at start
        if (menuPanel != null)
            menuPanel.SetActive(true);

        if (controlsMenuPanel != null)
            controlsMenuPanel.SetActive(false);

        // Pause game while menu is open
        Time.timeScale = 0f;
    }

    private void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(() => OnBeginGame("Floor_01"));

        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(() => OnBeginGame("Tutorial"));

        if (controlsButton != null)
            controlsButton.onClick.AddListener(OnOpenControls);

        if (closeControlsButton != null)
            closeControlsButton.onClick.AddListener(OnCloseControls);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitGame);
    }

    // -- BEGIN GAME --
    private void OnBeginGame(string sceneName)
    {
        AudioManager.Instance.PlayMusic(AudioManager.Instance.musicClip);

        // Resume time
        Time.timeScale = 1f;

        if (controlsMenuPanel != null)
            controlsMenuPanel.SetActive(false);

        // Close menu
        if (menuPanel != null)
            menuPanel.SetActive(false);

        if (closeControlsButton != null)
            closeControlsButton.onClick.RemoveListener(OnCloseControls);

        // Disable this script so ESC doesn't open main menu again
        gameObject.GetComponent<MenuManager>().enabled = false;

        LevelLoader.Instance.LoadLevel(sceneName);
    }

    // -- OPEN CONTROLS --
    private void OnOpenControls()
    {
        Debug.Log("Opening controls menu...");

        if (menuPanel != null)
            menuPanel.SetActive(false);

        if (controlsMenuPanel != null)
            controlsMenuPanel.SetActive(true);
    }

    // -- CLOSE CONTROLS --
    private void OnCloseControls()
    {
        Debug.Log("Closing controls menu...");

        if (controlsMenuPanel != null)
            controlsMenuPanel.SetActive(false);

        if (menuPanel != null)
            menuPanel.SetActive(true);
    }

    // -- QUIT GAME --
    private void OnQuitGame()
    {
        Debug.Log("Quitting game...");

        // Resume time before quitting
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}