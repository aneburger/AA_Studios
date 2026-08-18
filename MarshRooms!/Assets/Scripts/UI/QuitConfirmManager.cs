using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

public class QuitConfirmManager : MonoBehaviour
{
    public enum QuitConfirmSource
    {
        MainMenuQuit,
        PauseMenuMainMenu,
        PauseMenuQuit
    }

    [Header("Panel")]
    [SerializeField] private GameObject quitConfirmPanel;

    [Header("Buttons")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Text")]
    [SerializeField] private TMP_Text yesText;
    [SerializeField] private TMP_Text noText;

    [Header("Hover Arrows")]
    [SerializeField] private GameObject yesArrow;
    [SerializeField] private GameObject noArrow;

    [Header("UI Audio")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [Range(0f, 1f)][SerializeField] private float hoverVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float clickVolume = 1f;

    [Header("Text Colors")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color normalColor = new Color(0.78f, 0.78f, 0.78f, 1f);

    [Header("Menu References")]
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private PauseMenuManager pauseMenuManager;

    private Button[] buttons;
    private TMP_Text[] texts;
    private GameObject[] arrows;

    private QuitConfirmSource currentSource;
    private int currentIndex = -1;

    private void Awake()
    {
        buttons = new[] { yesButton, noButton };
        texts = new[] { yesText, noText };
        arrows = new[] { yesArrow, noArrow };

        ConfigureNavigation();
        ConfigurePointerEvents();

        if (quitConfirmPanel != null)
            quitConfirmPanel.SetActive(false);
    }

    private void Start()
    {
        if (yesButton != null)
            yesButton.onClick.AddListener(OnYesPressed);

        if (noButton != null)
            noButton.onClick.AddListener(OnNoPressed);
    }

    private void ConfigureNavigation()
    {
        if (yesButton != null)
        {
            Navigation nav = yesButton.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnLeft = noButton;
            nav.selectOnRight = noButton;
            nav.selectOnUp = noButton;
            nav.selectOnDown = noButton;
            yesButton.navigation = nav;
        }

        if (noButton != null)
        {
            Navigation nav = noButton.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnLeft = yesButton;
            nav.selectOnRight = yesButton;
            nav.selectOnUp = yesButton;
            nav.selectOnDown = yesButton;
            noButton.navigation = nav;
        }
    }

    private void ConfigurePointerEvents()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            int index = i;

            EventTrigger trigger = buttons[i].GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = buttons[i].gameObject.AddComponent<EventTrigger>();

            if (trigger.triggers == null)
                trigger.triggers = new List<EventTrigger.Entry>();

            AddTrigger(trigger, EventTriggerType.PointerEnter, _ => SetSelection(index, true));
            AddTrigger(trigger, EventTriggerType.Select, _ => SetSelection(index, true));
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

    public void Open(QuitConfirmSource source)
    {
        currentSource = source;

        if (quitConfirmPanel != null)
            quitConfirmPanel.SetActive(true);

        SetSelection(0, false);
    }

    private void Close()
    {
        if (quitConfirmPanel != null)
            quitConfirmPanel.SetActive(false);

        currentIndex = -1;
    }

    private void SetSelection(int index, bool playHoverSound)
    {
        if (index < 0 || index >= buttons.Length)
            return;

        if (buttons[index] == null)
            return;

        bool changed = currentIndex != index;
        currentIndex = index;

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
                texts[i].color = i == index ? selectedColor : normalColor;
        }

        for (int i = 0; i < arrows.Length; i++)
        {
            if (arrows[i] != null)
                arrows[i].SetActive(i == index);
        }

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != buttons[index].gameObject)
            EventSystem.current.SetSelectedGameObject(buttons[index].gameObject);

        if (playHoverSound && changed)
            PlayUiSound(hoverClip, hoverVolume);
    }

    private void OnYesPressed()
    {
        PlayUiSound(clickClip, clickVolume);
        Close();

        Time.timeScale = 1f;

        switch (currentSource)
        {
            case QuitConfirmSource.MainMenuQuit:
                QuitGame();
                break;

            case QuitConfirmSource.PauseMenuMainMenu:
                LevelLoader.Instance?.ReturnToMainMenu();
                break;

            case QuitConfirmSource.PauseMenuQuit:
                QuitGame();
                break;
        }
    }

    private void OnNoPressed()
    {
        PlayUiSound(clickClip, clickVolume);
        Close();

        switch (currentSource)
        {
            case QuitConfirmSource.MainMenuQuit:
                menuManager?.RestoreAfterQuitConfirm();
                break;

            case QuitConfirmSource.PauseMenuMainMenu:
            case QuitConfirmSource.PauseMenuQuit:
                pauseMenuManager?.RestoreAfterQuitConfirm(currentSource);
                break;
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void PlayUiSound(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        AudioManager.Instance?.PlaySFXWithPitch(clip, volume);
    }
}