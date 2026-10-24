using UnityEngine;

public class MenuProgressVisuals : MonoBehaviour
{
    [Header("Marsh animation")]
    [SerializeField] private GameObject defaultMarsh;
    [SerializeField] private GameObject beatenMarsh;
    [SerializeField] private GameObject inProgressMarsh;

    [SerializeField] private bool inProgressOverridesBeaten = true;

    [Header("Hide with menu")]
    [SerializeField] private GameObject menuPanel;

    [Header("Testing")]
    [SerializeField] private bool forceBeatenForTesting = false;

    private GameObject current;
    private bool hiddenWithMenu;
    private float nextCheck;
    private float lastWarning = -999f;

    private void OnEnable()
    {
        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        if (menuPanel != null && !menuPanel.activeInHierarchy)
        {
            if (!hiddenWithMenu)
            {
                SetAll(null);
                hiddenWithMenu = true;
            }
            return;
        }

        if (hiddenWithMenu)
        {
            hiddenWithMenu = false;
            Refresh();
            return;
        }

        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 0.5f;

        if (Choose() != current)
        {
            Refresh();
            return;
        }

        if (current != null && !current.activeSelf)
        {
            current.SetActive(true);
        }
    }

    public void Refresh()
    {
        current = Choose();
        SetAll(current);
    }

    private GameObject Choose()
    {
        bool beaten = GameProgress.IsBeaten || forceBeatenForTesting;
        bool inProgress = LevelLoader.Instance != null && LevelLoader.Instance.HasSavedGame;
        bool wantInProgress = inProgress && (!beaten || inProgressOverridesBeaten);

        GameObject chosen = null;
        if (wantInProgress) chosen = inProgressMarsh;
        if (chosen == null && beaten) chosen = beatenMarsh;
        if (chosen == null) chosen = defaultMarsh;

        if (chosen == null) chosen = beatenMarsh;
        if (chosen == null) chosen = inProgressMarsh;

        return chosen;
    }

    private void SetAll(GameObject toShow)
    {
        SetActiveIfAssigned(defaultMarsh, defaultMarsh == toShow);
        SetActiveIfAssigned(beatenMarsh, beatenMarsh == toShow);
        SetActiveIfAssigned(inProgressMarsh, inProgressMarsh == toShow);
    }

    private static void SetActiveIfAssigned(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }
}