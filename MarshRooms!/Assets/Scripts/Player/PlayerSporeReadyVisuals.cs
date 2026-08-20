// a slight green tint + a soft light pulse, until the player actually activates Mutated state.

using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class PlayerSporeReadyVisuals : MonoBehaviour
{
    [Header("Glow (same Light2D used by PlayerMutatedVisuals)")]
    [SerializeField] private Light2D mutateGlowLight;

    [Header("Tint")]
    [SerializeField] private Color readyTintColor = new Color32(0x00, 0xFF, 0xA9, 0xFF); // 00FFA9
    [SerializeField, Range(0f, 1f)] private float minTintBlend = 0.15f;
    [SerializeField, Range(0f, 1f)] private float maxTintBlend = 0.35f;

    [Header("Light Pulse")]
    [SerializeField] private float minLightIntensity = 0f;
    [SerializeField] private float maxLightIntensity = 0.6f;

    [Header("Pulse Speed")]
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Fade In")]
    [SerializeField] private float rampUpDuration = 0.6f;

    private SpriteRenderer[] playerRenderers;
    private Color[] originalColors;
    private float originalLightIntensity;

    private Coroutine pulseCoroutine;
    private bool isReady = false;

    // -- AWAKE --
    private void Awake()
    {
        playerRenderers = System.Array.FindAll(
            GetComponentsInChildren<SpriteRenderer>(),
            sr => sr.gameObject.name != "PlayerShadow"
            && sr.gameObject.name != "MutateGlow"
        );

        originalColors = new Color[playerRenderers.Length];
        for (int i = 0; i < playerRenderers.Length; i++)
            originalColors[i] = playerRenderers[i].color;

        if (mutateGlowLight != null)
            originalLightIntensity = mutateGlowLight.intensity;
    }

    // -- START --
    private void Start()
    {
        SporeManager.Instance.OnSporeCountChanged += HandleSporeCountChanged;
        SporeManager.Instance.OnMutatedActivated += HandleMutatedActivated;
    }

    // -- DISABLE --
    private void OnDisable()
    {
        if (SporeManager.Instance == null) return;
        SporeManager.Instance.OnSporeCountChanged -= HandleSporeCountChanged;
        SporeManager.Instance.OnMutatedActivated -= HandleMutatedActivated;
    }

    // -- SPORE COUNT CHANGED --
    private void HandleSporeCountChanged(int current, int max)
    {
        bool isFull = current >= max;

        if (isFull && !isReady)
            StartReadyState();
        else if (!isFull && isReady)
            ClearAllEffects();
    }

    // -- MUTATED ACTIVATED --
    private void HandleMutatedActivated()
    {
        if (!isReady) return;

        isReady = false;
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = null;

        SetTint(0f);
        if (mutateGlowLight != null)
            mutateGlowLight.intensity = originalLightIntensity;
    }

    // -- START READY STATE --
    private void StartReadyState()
    {
        isReady = true;
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    // -- CLEAR ALL EFFECTS -- 
    public void ClearAllEffects()
    {
        isReady = false;
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = null;

        SetTint(0f);
        if (mutateGlowLight != null)
        {
            mutateGlowLight.enabled = false;
            mutateGlowLight.intensity = originalLightIntensity;
        }
    }

    // -- PULSE ROUTINE --
    private IEnumerator PulseRoutine()
    {
        if (mutateGlowLight != null)
            mutateGlowLight.enabled = true;

        float startTime = Time.time;

        while (isReady)
        {
            // Ease the whole pulse amplitude in from 0 -> 1 over rampUpDuration,
            // so it fades in smoothly instead of popping straight into full pulsing.
            float rampFactor = rampUpDuration > 0f
                ? Mathf.Clamp01((Time.time - startTime) / rampUpDuration)
                : 1f;

            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0-1 pulse

            float tintBlend = Mathf.Lerp(minTintBlend, maxTintBlend, t) * rampFactor;
            SetTint(tintBlend);

            if (mutateGlowLight != null)
            {
                float lightIntensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, t) * rampFactor;
                mutateGlowLight.intensity = lightIntensity;
            }

            yield return null;
        }
    }

    // -- SET TINT --
    private void SetTint(float blend)
    {
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] == null) continue;
            playerRenderers[i].color = Color.Lerp(originalColors[i], readyTintColor, blend);
        }
    }
}