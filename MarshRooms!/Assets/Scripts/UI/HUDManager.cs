using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("Health")]
    [SerializeField] private Transform heartContainer;
    [SerializeField] private Image heartPrefab;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite threeQuarterHeartSprite;
    [SerializeField] private Sprite halfHeartSprite;
    [SerializeField] private Sprite quarterHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;

    [Header("Heart Effects")]
    [SerializeField] private Material heartFlashMaterial;
    [SerializeField] private Color damageFlashTint = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private float damageShakeAmount = 2f;
    [SerializeField] private float damageShakeDuration = 0.25f;
    [SerializeField] private float healPopAmount = 0.35f;
    [SerializeField] private float heartbeatInterval = 0.8f;
    [SerializeField] private float heartbeatAmount = 0.15f;
    [SerializeField] private float heartFlashDuration = 0.15f;
    [SerializeField] private float newHeartSuppressTime = 0.5f;

    [Header("Spores")]
    [SerializeField] private SporeBar sporeBar;

    [Header("Ammo")]
    [SerializeField] private AmmoDisplay ammoDisplay;

    [Header("Inventory")]
    [SerializeField] private InventoryDisplay inventoryDisplay;

    [Header("Menu")]
    [SerializeField] private GameObject menuPanel;

    [Header("HUD Elements")]
    [SerializeField] private GameObject hudElements;

    private Image[] heartImages;
    private UIPunch[] heartPunches;
    private float[] heartFlashTimers;
    private int lastDisplayedHealth = -1;
    private bool lowHealthPulse = false;
    private float suppressHealEffectsUntil = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InitializeHearts(); // initialize heart display based on max health
    }

    // -- UPDATE --
    private void Update()
    {
        if (heartFlashTimers == null || heartImages == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return; // paused

        for (int i = 0; i < heartFlashTimers.Length && i < heartImages.Length; i++)
        {
            if (heartFlashTimers[i] <= 0f) continue;

            heartFlashTimers[i] -= dt;
            if (heartFlashTimers[i] <= 0f && heartImages[i] != null)
                heartImages[i].sprite = GetHeartSprite(i, lastDisplayedHealth);
        }
    }

    // -- INITIALIZE HEARTS --
    private void InitializeHearts()
    {
        if (heartContainer == null || heartPrefab == null)
        {
            return;
        }

        // clear existing hearts
        foreach (Transform child in heartContainer)
        {
            Destroy(child.gameObject);
        }

        // Get max health from player
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null)
        {
            return;
        }

        int maxHealth = playerHealth.MaxHealth;
        int maxHearts = Mathf.CeilToInt(maxHealth / 4f); // Each heart represents 4 HP

        // creates heart images dynamically here
        int previousHeartCount = heartImages != null ? heartImages.Length : 0;
        heartImages = new Image[maxHearts];
        heartPunches = new UIPunch[maxHearts];
        heartFlashTimers = new float[maxHearts];
        for (int i = 0; i < maxHearts; i++)
        {
            Image newHeart = Instantiate(heartPrefab, heartContainer);
            heartImages[i] = newHeart;

            UIPunch punch = newHeart.GetComponent<UIPunch>();
            if (punch == null) punch = newHeart.gameObject.AddComponent<UIPunch>();
            heartPunches[i] = punch;
        }

        if (previousHeartCount > 0 && maxHearts > previousHeartCount)
            suppressHealEffectsUntil = Time.unscaledTime + newHeartSuppressTime;

        lastDisplayedHealth = -1;
        ApplyLowHealthPulse();
    }

    // -- UPDATE HEALTH DISPLAY --
    public void UpdateHealthDisplay(int currentHealth, int maxHealth, bool animate = false)
    {
        if (heartImages == null || heartImages.Length == 0)
            InitializeHearts();

        int fullHearts = currentHealth / 4;
        int remainder = currentHealth % 4;

        // display hearts
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (i < fullHearts)
            {
                heartImages[i].sprite = fullHeartSprite;
                heartImages[i].enabled = true;
            }
            else if (i == fullHearts && remainder > 0)
            {
                switch (remainder)
                {
                    case 3:
                        heartImages[i].sprite = threeQuarterHeartSprite;
                        break;
                    case 2:
                        heartImages[i].sprite = halfHeartSprite;
                        break;
                    case 1:
                        heartImages[i].sprite = quarterHeartSprite;
                        break;
                }
                heartImages[i].enabled = true;
            }
            else
            {
                heartImages[i].sprite = emptyHeartSprite;
                heartImages[i].enabled = true;
            }
        }

        if (animate && lastDisplayedHealth >= 0)
            PlayHeartEffects(lastDisplayedHealth, currentHealth);

        lastDisplayedHealth = currentHealth;
    }

    // -- PLAY HEART EFFECTS --
    private void PlayHeartEffects(int previousHealth, int currentHealth)
    {
        if (heartPunches == null) return;

        for (int i = 0; i < heartPunches.Length; i++)
        {
            if (heartPunches[i] == null) continue;

            int before = Mathf.Clamp(previousHealth - i * 4, 0, 4);
            int after = Mathf.Clamp(currentHealth - i * 4, 0, 4);

            if (after < before)
            {
                heartPunches[i].Shake(damageShakeAmount, damageShakeDuration);
                FlashHeart(i, damageFlashTint);
            }
            else if (after > before && Time.unscaledTime >= suppressHealEffectsUntil)
            {
                heartPunches[i].Punch(healPopAmount, 0.25f);
            }
        }
    }

    // -- FLASH HEART --
    private void FlashHeart(int index, Color tint)
    {
        heartPunches[index].Flash(tint, heartFlashDuration, heartFlashMaterial);

        if (heartFlashMaterial == null) return;

        heartImages[index].sprite = fullHeartSprite;
        heartFlashTimers[index] = heartFlashDuration;
    }

    // -- GET HEART SPRITE --
    private Sprite GetHeartSprite(int index, int health)
    {
        int quarters = Mathf.Clamp(health - index * 4, 0, 4);

        switch (quarters)
        {
            case 4: return fullHeartSprite;
            case 3: return threeQuarterHeartSprite;
            case 2: return halfHeartSprite;
            case 1: return quarterHeartSprite;
            default: return emptyHeartSprite;
        }
    }

    // -- SET LOW HEALTH PULSE --
    public void SetLowHealthPulse(bool active)
    {
        lowHealthPulse = active;
        ApplyLowHealthPulse();
    }

    // -- APPLY LOW HEALTH PULSE --
    private void ApplyLowHealthPulse()
    {
        if (heartPunches == null || heartPunches.Length == 0) return;

        if (lowHealthPulse)
        {
            if (heartPunches[0] != null)
                heartPunches[0].StartPulse(heartbeatInterval, heartbeatAmount);
        }
        else
        {
            foreach (UIPunch punch in heartPunches)
            {
                if (punch != null) punch.StopPulse();
            }
        }
    }

    // -- REFRESH HEARTS  -- 
    public void RefreshHearts()
    {
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null) return;

        int neededHearts = Mathf.CeilToInt(playerHealth.MaxHealth / 4f);

        if (heartImages == null || heartImages.Length != neededHearts)
            InitializeHearts();

        UpdateHealthDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    // -- SET HUD VISIBLE --
    public void SetHUDVisible(bool visible)
    {
        if (hudElements != null)
            hudElements.SetActive(visible);
    }

    // -- CLOSE MENU --
    public void CloseMenu()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);
    }
}