using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [SerializeField] private Image[] heartImages;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite threeQuarterHeartSprite;
    [SerializeField] private Sprite halfHeartSprite;
    [SerializeField] private Sprite quarterHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // -- UPDATE HEALTH DISPLAY --
    public void UpdateHealthDisplay(int currentHealth, int maxHealth)
    {
        int fullHearts = currentHealth / 4;
        int remainder = currentHealth % 4;

        // Display full hearts
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (i < fullHearts)
            {
                heartImages[i].sprite = fullHeartSprite;
                heartImages[i].enabled = true;
            }
            else if (i == fullHearts && remainder > 0)
            {
                // Display partial heart
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
                // Empty heart or hidden
                heartImages[i].enabled = false;
            }
        }
    }
}