using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        // Ensure menu is visible at start
        if (menuPanel != null)
            menuPanel.SetActive(true);

        // Pause game while menu is open
        Time.timeScale = 0f;
    }

    private void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitGame);
    }

    // -- START GAME --
    private void OnStartGame()
    {
        Debug.Log("Starting game...");

        // Resume time
        Time.timeScale = 1f;

        // Close menu
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // Disable this script so ESC doesn't open main menu again
        gameObject.GetComponent<MenuManager>().enabled = false;
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