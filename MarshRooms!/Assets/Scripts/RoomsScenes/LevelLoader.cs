using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }
 
    [Header("Scenes")]
    [SerializeField] private string persistentScene = "Persistent";
 
    [Header("Player")]
    [SerializeField] private string playerTag = "Player";
 
    [Header("Transition")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Floor Display Names")]
    // Shown on the death screen. Add one entry per floor (index 0 = floor 1).
    [SerializeField] private string[] floorDisplayNames;

    private const string SaveFileName = "savegame.json";
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public string CurrentLevelScene { get; private set; }
    private PlayerHealth playerHealth;

    // Set by ContinueSavedGame()
    private SaveGameData pendingRestoreData;

    public bool HasPendingRestore => pendingRestoreData != null;
    
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

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }
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

    // -- GET CURRENT FLOOR DISPLAY NAME --
    // Falls back to "Floor N" if you haven't filled in a name for that index yet.
    public string GetCurrentFloorDisplayName()
    {
        int floorNumber = GetCurrentFloorNumber();
        int index = floorNumber - 1;

        if (floorDisplayNames != null && index >= 0 && index < floorDisplayNames.Length && !string.IsNullOrEmpty(floorDisplayNames[index]))
            return floorDisplayNames[index];

        return $"Floor {floorNumber}";
    }

    // -- RETURN TO MAIN MENU --
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        if (BoonManager.Instance != null)
            Destroy(BoonManager.Instance.gameObject);

        if (SporeManager.Instance != null)
            Destroy(SporeManager.Instance.gameObject);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            Destroy(player);

        HUDManager.Instance?.SetHUDVisible(false);

        AudioManager.Instance?.StopMusic();

        CurrentLevelScene = null;

        SceneManager.LoadScene(persistentScene, LoadSceneMode.Single);
    }
    
    private IEnumerator LoadLevelRoutine(string sceneName)
    {
        ScreenEffects.Instance?.SetFadeImage();

        GameObject existingPlayer = GameObject.FindGameObjectWithTag(playerTag);
        playerHealth = existingPlayer != null ? existingPlayer.GetComponent<PlayerHealth>() : null;
        playerHealth?.UpdateHUD();

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

        ApplyPendingRestore();

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



    // ----- SAVE GAME CODE -----
    public bool HasSavedGame => File.Exists(SaveFilePath);

    public void SaveCurrentLevel()
    {
        if (string.IsNullOrEmpty(CurrentLevelScene))
            return;

        if (!CurrentLevelScene.StartsWith("Floor_"))
            return; // do not save Tutorial

        SaveGameData data = new SaveGameData
        {
            sceneName = CurrentLevelScene
        };

        if (BoonManager.Instance != null)
        {
            data.runStats = BoonManager.Instance.Stats;
            data.boonCounts = BoonManager.Instance.GetOwnedCountsSnapshot();
        }
        else
        {
            Debug.LogWarning("LevelLoader: BoonManager.Instance is null while saving, boon progress will not be saved.");
        }

        if (SporeManager.Instance != null)
        {
            data.currentSpores = SporeManager.Instance.GetCurrentSpores();
        }
        else
        {
            Debug.LogWarning("LevelLoader: SporeManager.Instance is null while saving, spore count will not be saved.");
        }

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            PlayerWeaponSlot weaponSlot = player.GetComponent<PlayerWeaponSlot>();
            if (weaponSlot != null)
            {
                data.weapons = weaponSlot.GetSaveData(out int currentSlot);
                data.currentWeaponSlot = currentSlot;
            }
        }
        else
        {
            Debug.LogWarning("LevelLoader: no player found while saving, weapons will not be saved.");
        }

        try
        {
            string json = JsonUtility.ToJson(data);
            File.WriteAllText(SaveFilePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"LevelLoader: failed to write save file - {e.Message}");
        }
    }

    public void ContinueSavedGame()
    {
        if (!HasSavedGame)
            return;

        SaveGameData loaded;
        try
        {
            string json = File.ReadAllText(SaveFilePath);
            loaded = JsonUtility.FromJson<SaveGameData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"LevelLoader: failed to read save file - {e.Message}");
            return;
        }

        if (loaded == null || string.IsNullOrEmpty(loaded.sceneName))
        {
            Debug.LogError("LevelLoader: save file was empty or corrupt.");
            return;
        }

        pendingRestoreData = loaded;
        LoadLevel(loaded.sceneName);
    }

    public void ClearSavedGame()
    {
        if (File.Exists(SaveFilePath))
            File.Delete(SaveFilePath);
    }

    // -- APPLY PENDING RESTORE --
    private void ApplyPendingRestore()
    {
        if (pendingRestoreData == null) return;

        SaveGameData data = pendingRestoreData;
        pendingRestoreData = null;

        if (BoonManager.Instance != null)
        {
            BoonManager.Instance.RestoreFromSave(data.runStats, data.boonCounts);
        }
        else
        {
            Debug.LogWarning("LevelLoader: BoonManager.Instance is null while restoring, boon progress was not restored.");
        }

        if (SporeManager.Instance != null)
        {
            SporeManager.Instance.RestoreSpores(data.currentSpores);
        }
        else
        {
            Debug.LogWarning("LevelLoader: SporeManager.Instance is null while restoring, spore count was not restored.");
        }

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            PlayerWeaponSlot weaponSlot = player.GetComponent<PlayerWeaponSlot>();
            weaponSlot?.RestoreFromSave(data.weapons, data.currentWeaponSlot);
        }
        else
        {
            Debug.LogWarning("LevelLoader: no player found while restoring, weapons were not restored.");
        }
    }
}