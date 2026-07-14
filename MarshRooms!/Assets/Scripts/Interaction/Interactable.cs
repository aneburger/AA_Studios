// Reusable interactable component.

using UnityEngine;
using System;

public class Interactable : MonoBehaviour
{
    [Header("Indicator")]
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField] private float indicatorHeight = 0.5f;

    [Header("Outline")]
    [SerializeField] private SpriteRenderer targetRenderer;

    public event Action OnInteract;
    public bool PlayerInRange { get; private set; } = false;

    private InteractIndicator indicator;
    private MaterialPropertyBlock mpb;
    private bool interactionLocked = false;

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
            instance.transform.localPosition = new Vector3(0f, indicatorHeight, 0f);
            indicator = instance.GetComponent<InteractIndicator>();
            indicator.SetBasePosition(new Vector3(0f, indicatorHeight, 0f));
            indicator.Hide();
        }
    }

    // -- PLAYER ENTERS RANGE --
    public void OnPlayerEnterRange()
    {
        if (!enabled) return;
        PlayerInRange = true;
        SetOutline(true);
    }

    // -- PLAYER EXITS RANGE --
    public void OnPlayerExitRange()
    {
        PlayerInRange = false;
        SetOutline(false);
    }

    // -- SET INTERACTION --
    public void SetInteractionLocked(bool locked)
    {
        interactionLocked = locked;

        if (locked)
            SetOutline(false);
        else if (PlayerInRange)
            SetOutline(true);
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

    // -- DISABLE --
    public void Disable()
    {
        OnPlayerExitRange();
        gameObject.SetActive(false);
    }
}