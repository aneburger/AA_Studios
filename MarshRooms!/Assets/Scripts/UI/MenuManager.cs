using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject controlsMenuPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button closeControlsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private QuitConfirmManager quitConfirmManager;

    [Header("Controls Menu Buttons")]
    [SerializeField] private Button cancelControlsButton;
    [SerializeField] private Button confirmControlsButton;
    [SerializeField] private SliderSoundManager sliderSoundManager;

    [Header("Menu Text")]
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private TMP_Text startText;
    [SerializeField] private TMP_Text controlsText;
    [SerializeField] private TMP_Text quitText;

    [Header("Controls Menu Text")]
    [SerializeField] private TMP_Text cancelText;
    [SerializeField] private TMP_Text confirmText;

    [Header("Button Arrows")]
    [SerializeField] private GameObject tutorialArrow;
    [SerializeField] private GameObject startArrow;
    [SerializeField] private GameObject controlsArrow;
    [SerializeField] private GameObject quitArrow;

    [Header("UI Audio")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [Range(0f, 1f)][SerializeField] private float hoverVolume;
    [Range(0f, 1f)][SerializeField] private float clickVolume;

    [Header("Music")]
    [SerializeField] private AudioClip menuMusic;
    [Range(0f, 1f)][SerializeField] private float menuMusicVolume;

    [Header("Text Colors")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color normalColor = new Color(0.78f, 0.78f, 0.78f, 1f);

    private Button[] menuButtons;
    private TMP_Text[] menuTexts;
    private GameObject[] menuArrows;

    private Button[] controlsMenuButtons;
    private TMP_Text[] controlsMenuTexts;

    private int currentIndex = -1;
    private int currentControlsIndex = -1;

    public enum ControlsMenuSource
    {
        MainMenu,
        PauseMenu
    }

    public ControlsMenuSource controlsMenuSource = ControlsMenuSource.MainMenu;

    private void Awake()
    {
        // Ensure menu is visible at start
        if (menuPanel != null)
            menuPanel.SetActive(true);

        if (controlsMenuPanel != null)
            controlsMenuPanel.SetActive(false);

        // Pause game while menu is open
        Time.timeScale = 0f;

        menuButtons = new[] { tutorialButton, startButton, controlsButton, quitButton };
        menuTexts = new[] { tutorialText, startText, controlsText, quitText };
        menuArrows = new[] { tutorialArrow, startArrow, controlsArrow, quitArrow };

        controlsMenuButtons = new[] { cancelControlsButton, confirmControlsButton };
        controlsMenuTexts = new[] { cancelText, confirmText };

        ConfigureNavigation();
        ConfigurePointerEvents();
        ConfigureControlsNavigation();
        ConfigureControlsPointerEvents();
    }

    private void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(() => OnBeginGame("Floor_05"));

        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(() => OnBeginGame("Tutorial"));

        if (controlsButton != null)
            controlsButton.onClick.AddListener(OnOpenControls);

        if (cancelControlsButton != null)
            cancelControlsButton.onClick.AddListener(OnCancelControls);

        if (confirmControlsButton != null)
            confirmControlsButton.onClick.AddListener(OnConfirmControls);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitGame);

        // Tutorial is default highlighted option
        SetSelection(0, false);

        // Start title music
        AudioManager.Instance?.PlayMusic(menuMusic, menuMusicVolume);
    }


    // -- NAVIGATION SETUP --
    private void ConfigureNavigation()
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null)
                continue;

            Navigation nav = menuButtons[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = FindPreviousButton(i);
            nav.selectOnDown = FindNextButton(i);
            nav.selectOnLeft = null;
            nav.selectOnRight = null;
            menuButtons[i].navigation = nav;
        }
    }

    private void ConfigureControlsNavigation()
    {
        for (int i = 0; i < controlsMenuButtons.Length; i++)
        {
            if (controlsMenuButtons[i] == null)
                continue;

            Navigation nav = controlsMenuButtons[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = FindPreviousControlsButton(i);
            nav.selectOnDown = FindNextControlsButton(i);
            nav.selectOnLeft = null;
            nav.selectOnRight = null;
            controlsMenuButtons[i].navigation = nav;
        }
    }

    private Button FindPreviousButton(int index)
    {
        for (int i = index - 1; i >= 0; i--)
        {
            if (menuButtons[i] != null)
                return menuButtons[i];
        }

        for (int i = menuButtons.Length - 1; i > index; i--)
        {
            if (menuButtons[i] != null)
                return menuButtons[i];
        }

        return menuButtons[index];
    }

    private Button FindNextButton(int index)
    {
        for (int i = index + 1; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] != null)
                return menuButtons[i];
        }

        for (int i = 0; i < index; i++)
        {
            if (menuButtons[i] != null)
                return menuButtons[i];
        }

        return menuButtons[index];
    }

    private Button FindPreviousControlsButton(int index)
    {
        for (int i = index - 1; i >= 0; i--)
        {
            if (controlsMenuButtons[i] != null)
                return controlsMenuButtons[i];
        }

        for (int i = controlsMenuButtons.Length - 1; i > index; i--)
        {
            if (controlsMenuButtons[i] != null)
                return controlsMenuButtons[i];
        }

        return controlsMenuButtons[index];
    }

    private Button FindNextControlsButton(int index)
    {
        for (int i = index + 1; i < controlsMenuButtons.Length; i++)
        {
            if (controlsMenuButtons[i] != null)
                return controlsMenuButtons[i];
        }

        for (int i = 0; i < index; i++)
        {
            if (controlsMenuButtons[i] != null)
                return controlsMenuButtons[i];
        }

        return controlsMenuButtons[index];
    }


    // -- POINTER / SELECT EVENTS --
    private void ConfigurePointerEvents()
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null)
                continue;

            int index = i;

            EventTrigger trigger = menuButtons[i].GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = menuButtons[i].gameObject.AddComponent<EventTrigger>();

            if (trigger.triggers == null)
                trigger.triggers = new List<EventTrigger.Entry>();

            AddTrigger(trigger, EventTriggerType.PointerEnter, _ => SetSelection(index, true));
            AddTrigger(trigger, EventTriggerType.Select, _ => SetSelection(index, true));
            //AddTrigger(trigger, EventTriggerType.PointerExit, _ => StartCoroutine(ResetToDefaultWhenMouseLeaves()));
        }
    }

    private void ConfigureControlsPointerEvents()
    {
        for (int i = 0; i < controlsMenuButtons.Length; i++)
        {
            if (controlsMenuButtons[i] == null)
                continue;

            int index = i;

            EventTrigger trigger = controlsMenuButtons[i].GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = controlsMenuButtons[i].gameObject.AddComponent<EventTrigger>();

            if (trigger.triggers == null)
                trigger.triggers = new List<EventTrigger.Entry>();

            AddTrigger(trigger, EventTriggerType.PointerEnter, _ => SetControlsSelection(index, true));
            AddTrigger(trigger, EventTriggerType.Select, _ => SetControlsSelection(index, true));
        }
    }


    private void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityAction<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = type
        };

        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }


    // -- SELECTION VISUALS --
    private void SetSelection(int index, bool playHoverSound)
    {
        if (index < 0 || index >= menuButtons.Length)
            return;

        if (menuButtons[index] == null)
            return;

        bool changed = currentIndex != index;
        currentIndex = index;

        for (int i = 0; i < menuTexts.Length; i++)
        {
            if (menuTexts[i] != null)
                menuTexts[i].color = i == index ? selectedColor : normalColor;
        }

        for (int i = 0; i < menuArrows.Length; i++)
        {
            if (menuArrows[i] != null)
                menuArrows[i].SetActive(i == index);
        }

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != menuButtons[index].gameObject)
            EventSystem.current.SetSelectedGameObject(menuButtons[index].gameObject);

        if (playHoverSound && changed)
            PlayUiSound(hoverClip, hoverVolume);
    }

    private void SetControlsSelection(int index, bool playHoverSound)
    {
        if (index < 0 || index >= controlsMenuButtons.Length)
            return;

        if (controlsMenuButtons[index] == null)
            return;

        bool changed = currentControlsIndex != index;
        currentControlsIndex = index;

        for (int i = 0; i < controlsMenuTexts.Length; i++)
        {
            if (controlsMenuTexts[i] != null)
                controlsMenuTexts[i].color = i == index ? selectedColor : normalColor;
        }

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != controlsMenuButtons[index].gameObject)
            EventSystem.current.SetSelectedGameObject(controlsMenuButtons[index].gameObject);

        if (playHoverSound && changed)
            PlayUiSound(hoverClip, hoverVolume);
    }

    private void PlayUiSound(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        AudioManager.Instance?.PlaySFXWithPitch(clip, volume);
    }



    // -- BEGIN GAME --
    private void OnBeginGame(string sceneName)
    {
        PlayUiSound(clickClip, clickVolume);
        AudioManager.Instance?.PlayMusic(AudioManager.Instance.musicClip);

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
        PlayUiSound(clickClip, clickVolume);

        controlsMenuSource = ControlsMenuSource.MainMenu;
        

        if (menuPanel != null)
            menuPanel.SetActive(false);


        if (controlsMenuPanel != null)
            controlsMenuPanel.SetActive(true);

        sliderSoundManager?.LoadConfirmedValues();
        SetControlsSelection(0, false);
    }

    // -- CLOSE CONTROLS --
    private void OnCloseControls()
    {
        PlayUiSound(clickClip, clickVolume);

        if (controlsMenuPanel != null)
            controlsMenuPanel.SetActive(false);

        if (menuPanel != null)
            menuPanel.SetActive(true);

        SetSelection(0, false);
    }

   

    private void OnCancelControls()
    {
        PlayUiSound(clickClip, clickVolume);
        sliderSoundManager?.CancelChanges();
        ReturnToOwningMenu();
    }

    private void OnConfirmControls()
    {
        PlayUiSound(clickClip, clickVolume);
        sliderSoundManager?.ConfirmChanges();
        ReturnToOwningMenu();
    }

    private void ReturnToOwningMenu()
    {
        if (controlsMenuPanel != null) controlsMenuPanel.SetActive(false);
        if (controlsMenuSource == ControlsMenuSource.MainMenu)
        {
            if (menuPanel != null)
                menuPanel.SetActive(true);

            SetSelection(2, false); 
        }
    }


    private void ReturnToMainMenuPanel()
    {
        if (controlsMenuPanel != null)
            controlsMenuPanel.SetActive(false);

        if (menuPanel != null)
            menuPanel.SetActive(true);

        if (controlsButton != null)
        {
            int settingsIndex = GetMenuButtonIndex(controlsButton);
            if (settingsIndex >= 0)
                SetSelection(settingsIndex, false);
            else
                SetSelection(0, false);
        }
        else
        {
            SetSelection(0, false);
        }
    }

    private int GetMenuButtonIndex(Button button)
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == button)
                return i;
        }

        return -1;
    }

    // -- REOPEN MENU --
    public void ReopenMenu()
    {
        enabled = true;

        Time.timeScale = 0f;

        if (menuPanel != null)
            menuPanel.SetActive(true);

        if (controlsMenuPanel != null)
            controlsMenuPanel.SetActive(false);

        SetSelection(0, false);
    }

    public void RestoreAfterQuitConfirm()
    {
        if (menuPanel != null)
            menuPanel.SetActive(true);

        if (controlsMenuPanel != null)
            controlsMenuPanel.SetActive(false);

        int quitIndex = GetMenuButtonIndex(quitButton);
        SetSelection(quitIndex >= 0 ? quitIndex : 0, false);
    }

    // -- QUIT GAME --
    private void OnQuitGame()
    {
        Debug.Log("Quitting game...");
        PlayUiSound(clickClip, clickVolume);

        // Resume time before quitting
        //Time.timeScale = 1f;

        quitConfirmManager?.Open(QuitConfirmManager.QuitConfirmSource.MainMenuQuit);


//#if UNITY_EDITOR
//        UnityEditor.EditorApplication.isPlaying = false;
//#else
//        Application.Quit();
//#endif
    }
}