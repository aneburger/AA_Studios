using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }

    [Header("Startup")]
    [SerializeField] private string firstLevelScene = "Tutorial";

    private string currentLevelScene;

    // -- AWAKE --
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // -- START --
    private void Start()
    {
        LoadLevel(firstLevelScene);
    }

    // -- LOAD LEVEL --
    public void LoadLevel(string sceneName)
    {
        StartCoroutine(LoadLevelRoutine(sceneName));
    }

    private IEnumerator LoadLevelRoutine(string sceneName)
    {
        // Unload the previous level scene, if one is loaded
        if (!string.IsNullOrEmpty(currentLevelScene))
        {
            yield return SceneManager.UnloadSceneAsync(currentLevelScene);
        }

        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        currentLevelScene = sceneName;
    }

    // -- RELOAD CURRENT LEVEL --
    public void ReloadCurrentLevel()
    {
        if (!string.IsNullOrEmpty(currentLevelScene))
            LoadLevel(currentLevelScene);
    }
}