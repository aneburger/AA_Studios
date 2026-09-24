using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
 
[System.Serializable]
public struct DeathScreenStats
{
    public int deathCount;
    public string killerEnemyName;
    public string floorName;
    public float timePlayedSeconds;
    public int killCount;

    public Sprite[] lostWeaponSprites;

    public Sprite[] collectedCardSprites;
}

public class DeathScreenManager : MonoBehaviour
{
    public static DeathScreenManager Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject deathScreenPanel;

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Text")]
    [SerializeField] private TMP_Text retryText;
    [SerializeField] private TMP_Text mainMenuText;

    [Header("Hover Arrows")]
    [SerializeField] private GameObject retryArrow;
    [SerializeField] private GameObject mainMenuArrow;

    [Header("Stats Display")]
    [SerializeField] private TMP_Text nrDiedText;
    [SerializeField] private TMP_Text enemyNameText;
    [SerializeField] private TMP_Text floorNameText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text killsText;

    [Header("Weapons Lost")]
    [SerializeField] private Image[] weaponLostSlots;

    [Header("Cards Collected")]
    [SerializeField] private Image[] cardCollectedSlots;

    [Header("UI Audio")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [Range(0f, 1f)][SerializeField] private float hoverVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float clickVolume = 1f;

    [Header("Text Colors")]
    [SerializeField] private Color selectedColor = new Color(255f / 255f, 225f / 255f, 213f / 255f, 255f / 255f);
    [SerializeField] private Color normalColor = new Color(0.78f, 0.78f, 0.78f, 1f);

    [Header("Menu References")]
    [SerializeField] private QuitConfirmManager quitConfirmManager;

    public bool IsOpen => deathScreenPanel != null && deathScreenPanel.activeSelf;

    private Button[] buttons;
    private TMP_Text[] texts;
    private GameObject[] arrows;
    private int currentIndex = -1;

    private void Awake()
    {
        Instance = this;

        buttons = new[] { retryButton, mainMenuButton };
        texts = new[] { retryText, mainMenuText };
        arrows = new[] { retryArrow, mainMenuArrow };

        ConfigureNavigation();
        ConfigurePointerEvents();

        if (deathScreenPanel != null)
            deathScreenPanel.SetActive(false);
    }

    private void Start()
    {
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryPressed);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuPressed);
    }

    private void ConfigureNavigation()
    {
        if (retryButton != null)
        {
            Navigation nav = retryButton.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnLeft = mainMenuButton;
            nav.selectOnRight = mainMenuButton;
            nav.selectOnUp = mainMenuButton;
            nav.selectOnDown = mainMenuButton;
            retryButton.navigation = nav;
        }

        if (mainMenuButton != null)
        {
            Navigation nav = mainMenuButton.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnLeft = retryButton;
            nav.selectOnRight = retryButton;
            nav.selectOnUp = retryButton;
            nav.selectOnDown = retryButton;
            mainMenuButton.navigation = nav;
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

    // -- SHOW --
    public void Show(DeathScreenStats stats)
    {
        PopulateStats(stats);

        if (deathScreenPanel != null)
            deathScreenPanel.SetActive(true);

        Time.timeScale = 0f;
        SetSelection(0, false);
    }

    private void PopulateStats(DeathScreenStats stats)
    {
        if (nrDiedText != null) nrDiedText.text = stats.deathCount.ToString();
        if (enemyNameText != null) enemyNameText.text = stats.killerEnemyName;
        if (floorNameText != null) floorNameText.text = stats.floorName;
        if (timeText != null) timeText.text = RunStatsTracker.FormatTime(stats.timePlayedSeconds);
        if (killsText != null) killsText.text = stats.killCount.ToString();

        PopulateWeaponsLost(stats.lostWeaponSprites);
        PopulateCardsCollected(stats.collectedCardSprites);
    }

    private void PopulateWeaponsLost(Sprite[] lostWeaponSprites)
    {
        if (weaponLostSlots == null)
            return;

        for (int i = 0; i < weaponLostSlots.Length; i++)
        {
            if (weaponLostSlots[i] == null)
                continue;

            Sprite sprite = (lostWeaponSprites != null && i < lostWeaponSprites.Length)
                ? lostWeaponSprites[i]
                : null;

            if (sprite != null)
            {
                weaponLostSlots[i].sprite = sprite;
                weaponLostSlots[i].enabled = true;
                weaponLostSlots[i].color = Color.white;
            }
            else
            {
                weaponLostSlots[i].sprite = null;
                weaponLostSlots[i].enabled = false;
            }
        }
    }

    private void PopulateCardsCollected(Sprite[] collectedCardSprites)
    {
        if (cardCollectedSlots == null)
            return;

        for (int i = 0; i < cardCollectedSlots.Length; i++)
        {
            if (cardCollectedSlots[i] == null)
                continue;

            Sprite sprite = (collectedCardSprites != null && i < collectedCardSprites.Length)
                ? collectedCardSprites[i]
                : null;

            if (sprite != null)
            {
                cardCollectedSlots[i].sprite = sprite;
                cardCollectedSlots[i].enabled = true;
                cardCollectedSlots[i].color = Color.white;
            }
            else
            {
                cardCollectedSlots[i].sprite = null;
                cardCollectedSlots[i].enabled = false;
            }
        }
    }

    private void Close()
    {
        if (deathScreenPanel != null)
            deathScreenPanel.SetActive(false);

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

    private void OnRetryPressed()
    {
        PlayUiSound(clickClip, clickVolume);
        Close();

        Time.timeScale = 1f;
        RunStatsTracker.Instance?.ResumeTimer();

        LevelLoader.Instance?.ReloadCurrentLevel();

        FindFirstObjectByType<PlayerHealth>()?.RestoreAfterRetry();
    }

    private void OnMainMenuPressed()
    {
        PlayUiSound(clickClip, clickVolume);
        quitConfirmManager?.Open(QuitConfirmManager.QuitConfirmSource.DeathScreenMainMenu);
    }

    public void RestoreAfterQuitConfirm()
    {
        if (deathScreenPanel != null)
            deathScreenPanel.SetActive(true);

        SetSelection(currentIndex < 0 ? 0 : currentIndex, false);
    }

    private void PlayUiSound(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        AudioManager.Instance?.PlaySFXWithPitch(clip, volume);
    }
}