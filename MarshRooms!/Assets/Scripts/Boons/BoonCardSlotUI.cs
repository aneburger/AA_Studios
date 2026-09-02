using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;

public class BoonCardSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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

    [Header("Hover Effect")]
    [SerializeField] private RectTransform hoverTarget;
    [SerializeField] private float hoverLiftAmount = 12f;
    [SerializeField] private float hoverTweenDuration = 0.12f;
    [SerializeField] private Image glowImage;
    [SerializeField] private Color outlineColor = Color.white;

    [Header("Audio")]
    [SerializeField] private AudioClip hoverSfx;
    [Range(0f, 1f)] [SerializeField] private float hoverSfxVolume = 0.5f;
    [SerializeField] private AudioClip clickSfx;
    [Range(0f, 1f)] [SerializeField] private float clickSfxVolume = 0.7f;

    private BoonCardData assignedCard;
    private Action<BoonCardData> onPicked;

    private Vector2 basePosition;
    private Coroutine hoverRoutine;
    private bool basePositionCaptured = false;
    private bool isHovering = false;


    // -- AWAKE --
    private void Awake()
    {
        if (backgroundImage != null) backgroundImage.raycastTarget = false;
        if (iconImage != null) iconImage.raycastTarget = false;
        if (glowImage != null) glowImage.raycastTarget = false;
        if (nameText != null) nameText.raycastTarget = false;
        if (rarityText != null) rarityText.raycastTarget = false;
        if (descriptionText != null) descriptionText.raycastTarget = false;

        if (glowImage != null)
        {
            Color c = outlineColor;
            c.a = 0f;
            glowImage.color = c;
        }
    }

    // -- SETUP --
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

        basePosition = hoverTarget.anchoredPosition;
        basePositionCaptured = true;
        SetOutlineAlpha(0f);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleClicked);
    }

    // -- HANDLE CLICKED --
    private void HandleClicked()
    {
        if (clickSfx != null)
            AudioManager.Instance.PlaySFX(clickSfx, clickSfxVolume);

        onPicked?.Invoke(assignedCard);
    }

    // -- POINTER ENTER --
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable || isHovering) return;
        isHovering = true;

        if (hoverSfx != null)
            AudioManager.Instance.PlaySFX(hoverSfx, hoverSfxVolume);

        StartHoverTween(hovered: true);
    }

    // -- POINTER EXIT --
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovering) return;
        isHovering = false;

        StartHoverTween(hovered: false);
    }

    // -- START HOVER TWEEN --
    private void StartHoverTween(bool hovered)
    {
        if (!basePositionCaptured) return;

        if (hoverRoutine != null)
            StopCoroutine(hoverRoutine);

        hoverRoutine = StartCoroutine(HoverTweenRoutine(hovered));
    }

    // -- HOVER TWEEN ROUTINE --
    private IEnumerator HoverTweenRoutine(bool hovered)
    {
        Vector2 startPos = hoverTarget.anchoredPosition;
        Vector2 endPos = basePosition + (hovered ? Vector2.up * hoverLiftAmount : Vector2.zero);

        float startOutline = glowImage != null ? glowImage.color.a : 0f;
        float endOutline = hovered ? outlineColor.a : 0f;

        float t = 0f;
        while (t < hoverTweenDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(t / hoverTweenDuration);
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);

            hoverTarget.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, eased);
            SetOutlineAlpha(Mathf.Lerp(startOutline, endOutline, eased));

            yield return null;
        }

        hoverTarget.anchoredPosition = endPos;
        SetOutlineAlpha(endOutline);
        hoverRoutine = null;
    }

    // -- SET OUTLINE ALPHA --
    private void SetOutlineAlpha(float alpha)
    {
        if (glowImage == null) return;
        Color c = outlineColor;
        c.a = alpha;
        glowImage.color = c;
    }

    // -- GET BACKGROUND FOR CATEGORY --
    private Sprite GetBackgroundForCategory(BoonCategory category)
    {
        foreach (var entry in categoryBackgrounds)
        {
            if (entry.category == category)
                return entry.background;
        }
        return null;
    }

    // -- ON DISABLE --
    private void OnDisable()
    {
        isHovering = false;

        if (hoverRoutine != null)
        {
            StopCoroutine(hoverRoutine);
            hoverRoutine = null;
        }

        if (basePositionCaptured)
            hoverTarget.anchoredPosition = basePosition;

        SetOutlineAlpha(0f);
    }
}