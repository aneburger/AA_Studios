using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System.Collections;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }
 
    [Header("Scenes")]
    [SerializeField] private string persistentScene = "Persistent";
 
    [Header("Player")]
    [SerializeField] private string playerTag = "Player";
 
    [Header("Transition")]
    [SerializeField] private float fadeDuration = 0.5f;
 
    public string CurrentLevelScene { get; private set; }
    
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
    
    // -- LOAD LEVEL --
    public void LoadLevel(string sceneName)
    {
        StartCoroutine(LoadLevelRoutine(sceneName));
    }
    
    // -- RELOAD CURRENT LEVEL --
    public void ReloadCurrentLevel()
    {
        if (!string.IsNullOrEmpty(CurrentLevelScene))
            LoadLevel(CurrentLevelScene);
    }

    // -- GET CURRENT FLOOR NUMBER -- 
    public int GetCurrentFloorNumber()
    {
        if (string.IsNullOrEmpty(CurrentLevelScene)) return 1;

        string digits = System.Text.RegularExpressions.Regex.Match(CurrentLevelScene, @"\d+").Value;
        return int.TryParse(digits, out int floorNum) ? floorNum : 1;
    }

    // -- GET NEXT FLOOR SCENE NAME --
    public string GetNextFloorSceneName()
    {
        int nextFloor = GetCurrentFloorNumber() + 1;
        return $"Floor_{nextFloor:D2}";
    }
    
    private IEnumerator LoadLevelRoutine(string sceneName)
    {
        ScreenEffects.Instance?.SetFadeImage();

        if (!SceneManager.GetSceneByName(persistentScene).isLoaded)
        {
            yield return SceneManager.LoadSceneAsync(persistentScene, LoadSceneMode.Additive);
        }
 
        if (!string.IsNullOrEmpty(CurrentLevelScene))
        {
            yield return SceneManager.UnloadSceneAsync(CurrentLevelScene);
        }
 
        Scene loaded = default;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return op;
 
        CurrentLevelScene = sceneName;
        loaded = SceneManager.GetSceneByName(sceneName);
        if (loaded.IsValid())
            SceneManager.SetActiveScene(loaded);
 
        yield return null;
 
        PositionPlayerAtSpawn();

        if(sceneName == "Tutorial") yield break;
        
        yield return Fade(toBlack: false);
    }
    
    private IEnumerator Fade(bool toBlack)
    {
        if (ScreenEffects.Instance == null || fadeDuration <= 0f)
            yield break;
 
        bool done = false;
        void OnDone() => done = true;
 
        if (toBlack)
            ScreenEffects.Instance.FadeToBlack(fadeDuration, OnDone);
        else
            ScreenEffects.Instance.FadeFromBlack(fadeDuration, OnDone);
 
        yield return new WaitUntil(() => done);
    }
 
    private void PositionPlayerAtSpawn()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            Debug.LogWarning("LevelLoader: no player found to position (missing tag?).");
            return;
        }
 
        SpawnPoint target = FindFirstObjectByType<SpawnPoint>();
        if (target == null)
        {
            Debug.LogWarning($"LevelLoader: no SpawnPoint found in '{CurrentLevelScene}'.");
            return;
        }
 
        player.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
 
        var rb2d = player.GetComponent<Rigidbody2D>();
        if (rb2d != null) rb2d.linearVelocity = Vector2.zero;
    }
}