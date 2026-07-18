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

    // -- INITIALIZE HEARTS --
    private void InitializeHearts()
    {
        if (heartContainer == null || heartPrefab == null)
        {
            Debug.LogError("HUDManager: heartContainer or heartPrefab is not assigned");
            return;
        }

        // clear existing hearts
        foreach (Transform child in heartContainer)
        {
            Destroy(child.gameObject);
        }

        // Get max health from player
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("HUDManager: PlayerHealth not found in scene");
            return;
        }

        int maxHealth = playerHealth.MaxHealth;
        int maxHearts = Mathf.CeilToInt(maxHealth / 4f); // Each heart represents 4 HP

        // creates heart images dynamically here
        heartImages = new Image[maxHearts];
        for (int i = 0; i < maxHearts; i++)
        {
            Image newHeart = Instantiate(heartPrefab, heartContainer);
            heartImages[i] = newHeart;
        }
    }

    // -- UPDATE HEALTH DISPLAY --
    public void UpdateHealthDisplay(int currentHealth, int maxHealth)
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
    }

    // -- REFRESH HEARTS  -- when max health changes
    public void RefreshHearts()
    {
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            InitializeHearts();
            UpdateHealthDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
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