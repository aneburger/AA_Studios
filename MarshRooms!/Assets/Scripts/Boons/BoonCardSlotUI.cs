using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class BoonCardSlotUI : MonoBehaviour
{
    [Serializable]
    public struct CategoryBackground
    {
        public BoonCategory category;
        public Sprite background;
    }

    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button button;

    [Header("Category Backgrounds")]
    [SerializeField] private CategoryBackground[] categoryBackgrounds;

    private BoonCardData assignedCard;
    private Action<BoonCardData> onPicked;

    public void Setup(BoonCardData card, Action<BoonCardData> onPickedCallback)
    {
        assignedCard = card;
        onPicked = onPickedCallback;

        nameText.text = card.displayName;
        descriptionText.text = card.description;
        rarityText.text = card.rarity.ToString();

        if (backgroundImage != null)
        {
            Sprite bg = GetBackgroundForCategory(card.category);
            if (bg != null)
                backgroundImage.sprite = bg;
        }

        if (card.icon != null)
        {
            iconImage.sprite = card.icon;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.enabled = false;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onPicked?.Invoke(assignedCard));
    }

    private Sprite GetBackgroundForCategory(BoonCategory category)
    {
        foreach (var entry in categoryBackgrounds)
        {
            if (entry.category == category)
                return entry.background;
        }
        return null;
    }
}