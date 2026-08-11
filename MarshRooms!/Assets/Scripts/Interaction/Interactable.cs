// Reusable interactable component.

using UnityEngine;
using System;

public class Interactable : MonoBehaviour
{
    [Header("Indicator")]
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField] private float indicatorHeight = 0.5f;
    [SerializeField] private float indicatorHorizontalOffset = 0f;

    [Header("Speech Bubble")]
    [SerializeField] private GameObject speechBubblePrefab;
    [SerializeField] private float speechBubbleHeight = 0.5f;
    [SerializeField] private float speechBubbleHorizontalOffset = 0f;

    [Header("Outline")]
    [SerializeField] private SpriteRenderer targetRenderer;

    public event Action OnInteract;
    public bool PlayerInRange { get; private set; } = false;

    private InteractIndicator indicator;
    private InteractIndicator speechBubbleIndicator;
    private MaterialPropertyBlock mpb;
    private bool interactionLocked = false;
    private bool hasSomethingToSay = false;

    private static readonly int OutlineEnabledID   = Shader.PropertyToID("_OutlineEnabled");
    private static readonly int OutlineThicknessID = Shader.PropertyToID("_OutlineThickness");

    // -- AWAKE --
    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        mpb = new MaterialPropertyBlock();
        SetOutline(false);

        if (indicatorPrefab != null)
        {
            GameObject instance = Instantiate(indicatorPrefab, transform);
            Vector3 basePos = new Vector3(indicatorHorizontalOffset, indicatorHeight, 0f);
            instance.transform.localPosition = basePos;
            indicator = instance.GetComponent<InteractIndicator>();
            indicator.SetBasePosition(basePos);
            indicator.Hide();
        }

        // Speech bubble
        if (speechBubblePrefab != null)
        {
            GameObject bubbleInstance = Instantiate(speechBubblePrefab, transform);
            Vector3 bubbleBasePos = new Vector3(speechBubbleHorizontalOffset, speechBubbleHeight, 0f);
            bubbleInstance.transform.localPosition = bubbleBasePos;
            speechBubbleIndicator = bubbleInstance.GetComponent<InteractIndicator>();

            if (speechBubbleIndicator != null)
            {
                speechBubbleIndicator.SetBasePosition(bubbleBasePos);
                speechBubbleIndicator.Hide();
            }
            else
            {
                Debug.LogWarning($"Speech Bubble Prefab on '{gameObject.name}' is missing an InteractIndicator component.", this);
            }
        }
    }

    // -- PLAYER ENTERS RANGE --
    public void OnPlayerEnterRange()
    {
        PlayerInRange = true;
        if (enabled && !interactionLocked)
            SetOutline(true);

        UpdateSpeechBubble();
    }

    // -- PLAYER EXITS RANGE --
    public void OnPlayerExitRange()
    {
        PlayerInRange = false;
        SetOutline(false);

        UpdateSpeechBubble();
    }

    // -- SET INTERACTION --
    public void SetInteractionLocked(bool locked)
    {
        interactionLocked = locked;

        if (locked)
            SetOutline(false);
        else if (PlayerInRange)
            SetOutline(true);

        UpdateSpeechBubble();
    }

    // -- SET HAS SOMETHING TO SAY --
    public void SetHasSomethingToSay(bool value)
    {
        hasSomethingToSay = value;
        UpdateSpeechBubble();
    }

    // -- TRY INTERACT --
    public void TryInteract()
    {
        if (!enabled) return;
        if (!PlayerInRange || interactionLocked) return;
        OnInteract?.Invoke();
    }

    // -- SET OUTLINE --
    public void SetOutline(bool enabled)
    {
        if (targetRenderer == null) return;

        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(OutlineEnabledID,   enabled ? 1f : 0f);
        mpb.SetFloat(OutlineThicknessID, 1f);
        targetRenderer.SetPropertyBlock(mpb);

        if (enabled) indicator?.Show();
        else         indicator?.Hide();
    }

    // -- UPDATE SPEECH BUBBLE --
    private bool speechBubbleVisible = false;

    private void UpdateSpeechBubble()
    {
        bool show = hasSomethingToSay && !PlayerInRange && !interactionLocked && enabled;

        if (show == speechBubbleVisible) return;
        speechBubbleVisible = show;

        if (show) speechBubbleIndicator?.Show();
        else       speechBubbleIndicator?.Hide();
    }

    // -- DISABLE --
    public void Disable()
    {
        OnPlayerExitRange();
        gameObject.SetActive(false);
    }

    // -- ON ENABLE --
    private void OnEnable()
    {
        if (PlayerInRange && !interactionLocked)
            SetOutline(true);

        UpdateSpeechBubble();
    }
}