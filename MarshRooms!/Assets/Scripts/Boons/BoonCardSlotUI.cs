using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class BoonCardSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button button;

    private BoonCardData assignedCard;
    private Action<BoonCardData> onPicked;

    public void Setup(BoonCardData card, Action<BoonCardData> onPickedCallback)
    {
        assignedCard = card;
        onPicked = onPickedCallback;

        nameText.text = card.displayName;
        descriptionText.text = card.description;
        rarityText.text = card.rarity.ToString();

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
}