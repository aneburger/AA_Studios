using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NewGameConfirmManager : MonoBehaviour
{
    public enum NewGameConfirmSource
    {
        MainMenuNewGame,
        MainMenuTutorial
    }

    [Header("Panel")]
    [SerializeField] private GameObject newGameConfirmPanel;

    [Header("Buttons")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private Button easyModeButton;
    [SerializeField] private Button normalModeButton;
    [SerializeField] private Button hardModeButton;
    [SerializeField] private Button deathCapModeButton;

    [Header("Text")]
    [SerializeField] private TMP_Text yesText;
    [SerializeField] private TMP_Text noText;
    [SerializeField] private TMP_Text modeDescription;
    [SerializeField] private TMP_Text easyModeText;
    [SerializeField] private TMP_Text normalModeText;
    [SerializeField] private TMP_Text hardModeText;
    [SerializeField] private TMP_Text deathCapModeText;

    [Header("Hover Arrows")]
    [SerializeField] private GameObject yesArrow;
    [SerializeField] private GameObject noArrow;
    [SerializeField] private GameObject easyModeHover;
    [SerializeField] private GameObject normalModeHover;
    [SerializeField] private GameObject hardModeHover;
    [SerializeField] private GameObject deathCapModeHover;

    [Header("UI Audio")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [Range(0f, 1f)][SerializeField] private float hoverVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float clickVolume = 1f;

    [Header("Text Colors")]
    [SerializeField] private Color selectedColor = new Color(255f / 255f, 225f / 255f, 213f / 255f, 255f / 255f);
    [SerializeField] private Color normalColor = new Color(0.78f, 0.78f, 0.78f, 1f);

    [Header("Start Floor")]
    [SerializeField] private string startFloor = "Floor_01";

    private Button[] buttons;
    private TMP_Text[] texts;
    private GameObject[] arrows;
    private Button[] difficultyButtons;
    private TMP_Text[] difficultyTexts;
    private GameObject[] difficultyHovers;

    private NewGameConfirmSource currentSource;
    private int selectedDifficultyIndex = -1;
    private int hoveredDifficultyIndex = -1;
    private int selectedConfirmationIndex = -1;
    private int hoveredConfirmationIndex = -1;
    private Difficulty chosenDifficulty = Difficulty.Normal;

    public bool NewGameConfirmIsOpen => newGameConfirmPanel != null && newGameConfirmPanel.activeSelf;

    private void Awake()
    {
        ResolveAssetReferences();

        buttons = new[] { yesButton, noButton };
        texts = new[] { yesText, noText };
        arrows = new[] { yesArrow, noArrow };
        difficultyButtons = new[] { easyModeButton, normalModeButton, hardModeButton, deathCapModeButton };
        difficultyTexts = new[] { easyModeText, normalModeText, hardModeText, deathCapModeText };
        difficultyHovers = new[] { easyModeHover, normalModeHover, hardModeHover, deathCapModeHover };

        ConfigureNavigation();
        ConfigurePointerEvents();

        if (newGameConfirmPanel != null)
            newGameConfirmPanel.SetActive(false);
    }

    private void Start()
    {
        if (yesButton != null)
            yesButton.onClick.AddListener(OnYesPressed);

        if (noButton != null)
            noButton.onClick.AddListener(OnNoPressed);

        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            int index = i;
            if (difficultyButtons[i] != null)
                difficultyButtons[i].onClick.AddListener(() => SelectDifficulty(index));
        }
    }

    private void ResolveAssetReferences()
    {
        easyModeButton = ResolveButton(easyModeButton, "easy-mode-button");
        normalModeButton = ResolveButton(normalModeButton, "normal-mode-button");
        hardModeButton = ResolveButton(hardModeButton, "hard-mode-button");
        deathCapModeButton = ResolveButton(deathCapModeButton, "death-cap-mode-button");

        easyModeHover = ResolveChild(easyModeHover, "easy-hover");
        normalModeHover = ResolveChild(normalModeHover, "normal-hover");
        hardModeHover = ResolveChild(hardModeHover, "hard-hover");
        deathCapModeHover = ResolveChild(deathCapModeHover, "death-cap-hover");

        modeDescription = ResolveText(modeDescription, "mode-description");
        easyModeText = ResolveText(easyModeText, easyModeButton);
        normalModeText = ResolveText(normalModeText, normalModeButton);
        hardModeText = ResolveText(hardModeText, hardModeButton);
        deathCapModeText = ResolveText(deathCapModeText, deathCapModeButton);
    }

    private Button ResolveButton(Button current, string objectName)
    {
        if (current != null)
            return current;

        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button.name == objectName)
                return button;
        }

        return null;
    }

    private GameObject ResolveChild(GameObject current, string objectName)
    {
        if (current != null)
            return current;

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child.gameObject;
        }

        return null;
    }

    private TMP_Text ResolveText(TMP_Text current, Button button)
    {
        if (current != null || button == null)
            return current;

        return button.GetComponentInChildren<TMP_Text>(true);
    }

    private TMP_Text ResolveText(TMP_Text current, string objectName)
    {
        if (current != null)
            return current;

        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == objectName)
                return text;
        }

        return null;
    }

    private void ConfigureNavigation()
    {
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            if (difficultyButtons[i] == null)
                continue;

            Navigation nav = difficultyButtons[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnLeft = difficultyButtons[(i + difficultyButtons.Length - 1) % difficultyButtons.Length];
            nav.selectOnRight = difficultyButtons[(i + 1) % difficultyButtons.Length];
            nav.selectOnDown = yesButton;
            difficultyButtons[i].navigation = nav;
        }

        ConfigureButtonNavigation(yesButton, noButton);
        ConfigureButtonNavigation(noButton, yesButton);
    }

    private void ConfigureButtonNavigation(Button button, Button otherButton)
    {
        if (button == null)
            return;

        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.Explicit;
        nav.selectOnUp = normalModeButton;
        nav.selectOnLeft = otherButton;
        nav.selectOnRight = otherButton;
        button.navigation = nav;
    }

    private void ConfigurePointerEvents()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            int index = i;
            AddPointerTrigger(buttons[i], EventTriggerType.PointerEnter, _ => SetConfirmationHover(index, true));
            AddPointerTrigger(buttons[i], EventTriggerType.Select, _ => SetConfirmationHover(index, true));
            AddPointerTrigger(buttons[i], EventTriggerType.PointerExit, _ => ClearConfirmationHover(index));
        }

        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            if (difficultyButtons[i] == null)
                continue;

            int index = i;
            AddPointerTrigger(difficultyButtons[i], EventTriggerType.PointerEnter, _ => SetDifficultyHover(index, true));
            AddPointerTrigger(difficultyButtons[i], EventTriggerType.Select, _ => SetDifficultyHover(index, true));
            AddPointerTrigger(difficultyButtons[i], EventTriggerType.PointerExit, _ => ClearDifficultyHover(index));
        }
    }

    private void AddPointerTrigger(Button button, EventTriggerType type, UnityAction<BaseEventData> callback)
    {
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    public void Open(NewGameConfirmSource source)
    {
        currentSource = source;
        chosenDifficulty = Difficulty.Normal;

        if (newGameConfirmPanel != null)
            newGameConfirmPanel.SetActive(true);

        SetDifficultySelection((int)Difficulty.Normal, false);
        SetConfirmationSelection(0, false);
    }

    private void Close()
    {
        if (newGameConfirmPanel != null)
            newGameConfirmPanel.SetActive(false);

        selectedDifficultyIndex = -1;
        hoveredDifficultyIndex = -1;
        selectedConfirmationIndex = -1;
        hoveredConfirmationIndex = -1;
    }

    private void SelectDifficulty(int index)
    {
        if (index < 0 || index >= difficultyButtons.Length || difficultyButtons[index] == null)
            return;

        PlayUiSound(clickClip, clickVolume);
        chosenDifficulty = (Difficulty)index;
        SetDifficultySelection(index, false);
    }

    private void SetDifficultySelection(int index, bool playHoverSound)
    {
        if (index < 0 || index >= difficultyButtons.Length || difficultyButtons[index] == null)
            return;

        bool changed = selectedDifficultyIndex != index;
        selectedDifficultyIndex = index;

        for (int i = 0; i < difficultyTexts.Length; i++)
        {
            if (difficultyTexts[i] != null)
                difficultyTexts[i].color = i == index ? selectedColor : normalColor;
        }

        UpdateDifficultyHovers();

        if (modeDescription != null)
            modeDescription.text = GetDifficultyDescription((Difficulty)index);

        SelectButton(difficultyButtons[index]);

        if (playHoverSound && changed)
            PlayUiSound(hoverClip, hoverVolume);
    }

    private string GetDifficultyDescription(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                return "Fewer, weaker, slower enemies";
            case Difficulty.Hard:
                return "More, tougher, faster enemies";
            case Difficulty.DeathCap:
                return "RESTART FROM FLOOR 1 WHEN YOU DIE.";
            default:
                return "The intended experience";
        }
    }

    private void SetDifficultyHover(int index, bool playHoverSound)
    {
        if (index < 0 || index >= difficultyButtons.Length || difficultyButtons[index] == null)
            return;

        bool changed = hoveredDifficultyIndex != index;
        hoveredDifficultyIndex = index;
        UpdateDifficultyHovers();

        if (playHoverSound && changed)
            PlayUiSound(hoverClip, hoverVolume);
    }

    private void ClearDifficultyHover(int index)
    {
        if (hoveredDifficultyIndex != index)
            return;

        hoveredDifficultyIndex = -1;
        UpdateDifficultyHovers();
    }

    private void SetConfirmationSelection(int index, bool playHoverSound)
    {
        if (index < 0 || index >= buttons.Length || buttons[index] == null)
            return;

        bool changed = selectedConfirmationIndex != index;
        selectedConfirmationIndex = index;

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
                texts[i].color = i == index ? selectedColor : normalColor;
        }

        UpdateConfirmationHovers();

        SelectButton(buttons[index]);

        if (playHoverSound && changed)
            PlayUiSound(hoverClip, hoverVolume);
    }

    private void SetConfirmationHover(int index, bool playHoverSound)
    {
        if (index < 0 || index >= buttons.Length || buttons[index] == null)
            return;

        hoveredConfirmationIndex = index;
        SetConfirmationSelection(index, playHoverSound);
    }

    private void ClearConfirmationHover(int index)
    {
        if (hoveredConfirmationIndex != index)
            return;

        hoveredConfirmationIndex = -1;
        UpdateConfirmationHovers();
    }

    private void UpdateDifficultyHovers()
    {
        for (int i = 0; i < difficultyHovers.Length; i++)
        {
            if (difficultyHovers[i] != null)
                difficultyHovers[i].SetActive(i == selectedDifficultyIndex || i == hoveredDifficultyIndex);
        }
    }

    private void UpdateConfirmationHovers()
    {
        for (int i = 0; i < arrows.Length; i++)
        {
            if (arrows[i] != null)
                arrows[i].SetActive(i == selectedConfirmationIndex || i == hoveredConfirmationIndex);
        }
    }

    private void SelectButton(Button button)
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != button.gameObject)
            EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void OnYesPressed()
    {
        PlayUiSound(clickClip, clickVolume);
        Close();

        Time.timeScale = 1f;
        DifficultyManager.Instance.StartNewRun(chosenDifficulty);
        LevelLoader.Instance?.ClearSavedGame();

        MenuManager menuManager = FindFirstObjectByType<MenuManager>();
        if (menuManager != null)
        {
            string scene = currentSource == NewGameConfirmSource.MainMenuTutorial ? "Tutorial" : startFloor;
            menuManager.OnBeginGame(scene);
        }
    }

    public void HandleEscapePressed()
    {
        OnNoPressed();
    }

    private void OnNoPressed()
    {
        PlayUiSound(clickClip, clickVolume);
        Close();
    }

    private void PlayUiSound(AudioClip clip, float volume)
    {
        if (clip != null)
            AudioManager.Instance?.PlaySFXWithPitch(clip, volume);
    }
}
