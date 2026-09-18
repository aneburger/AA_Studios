using UnityEngine;
using UnityEngine.UI;

public class SporeBar : MonoBehaviour
{
    [Header("Fill")]
    [SerializeField] private Image fillImage;
    [SerializeField] private SporeMeterConfig meterConfig;

    [Header("Ready Prompt")]
    [SerializeField] private GameObject readyPrompt;
    [SerializeField] private Vector2 readyPromptOffset = new Vector2(38f, 20f);

    [Header("Ready Audio")]
    [SerializeField] private AudioClip sporeReadyClip;
    [Range(0f, 1f)] [SerializeField] private float sporeReadyVolume = 0.5f;

    private bool isSporeReadyActive = false;

    [Header("Shake")]
    [SerializeField] private RectTransform shakeTarget;
    [SerializeField] private float shakeAmplitude = 1.5f;
    [SerializeField] private float shakeSpeed = 18f;

    private float targetFillAmount = 0f;
    private Vector2 originalAnchoredPosition;
    private bool hasOriginalPosition;

    private RectTransform promptRect;
    private Canvas parentCanvas;
    private RectTransform parentRect;

    // -- START --
    private void Start()
    {
        if (fillImage == null)
            fillImage = GetComponentInChildren<Image>();

        if (shakeTarget == null)
            shakeTarget = GetComponent<RectTransform>();

        if (shakeTarget != null)
        {
            originalAnchoredPosition = shakeTarget.anchoredPosition;
            hasOriginalPosition = true;
        }

        parentRect = transform.parent as RectTransform;
        parentCanvas = GetComponentInParent<Canvas>();

        if (readyPrompt != null)
        {
            promptRect = readyPrompt.GetComponent<RectTransform>();
            readyPrompt.SetActive(false);
        }

        if (SporeManager.Instance != null)
        {
            SporeManager.Instance.OnSporeCountChanged += UpdateSporeBar;
            SporeManager.Instance.OnMutatedActivated += OnMutatedActivated;
            SporeManager.Instance.OnMutatedEnded += OnMutatedEnded;
            SporeManager.Instance.OnMutatedDrainTick += UpdateDrain;

            UpdateSporeBar(SporeManager.Instance.GetCurrentSpores(), SporeManager.Instance.GetMaxSpores());
        }
    }

    // -- DESTROY --
    private void OnDestroy()
    {
        if (SporeManager.Instance != null)
        {
            SporeManager.Instance.OnSporeCountChanged -= UpdateSporeBar;
            SporeManager.Instance.OnMutatedActivated -= OnMutatedActivated;
            SporeManager.Instance.OnMutatedEnded -= OnMutatedEnded;
            SporeManager.Instance.OnMutatedDrainTick -= UpdateDrain;
        }
    }

    // -- UPDATE --
    private void Update()
    {
        if (fillImage != null && meterConfig != null)
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount, meterConfig.fillSmoothSpeed * Time.deltaTime);

        UpdateShake();
    }

    // -- UPDATE SPORE BAR --
    private void UpdateSporeBar(int currentSpores, int maxSpores)
    {
        if (fillImage == null || maxSpores <= 0)
            return;

        targetFillAmount = (float)currentSpores / maxSpores;
        UpdateColor(targetFillAmount);

        if (currentSpores >= maxSpores && !SporeManager.Instance.IsMutated)
            ShowReadyPrompt();
        else if (!SporeManager.Instance.IsMutated)
            HideReadyPrompt();
    }

    // -- UPDATE DRAIN --
    private void UpdateDrain(float normalizedRemaining)
    {
        targetFillAmount = normalizedRemaining;

        if (meterConfig != null && fillImage != null)
            fillImage.color = meterConfig.fullColor;
    }

    // -- MUTATED ACTIVATED --
    private void OnMutatedActivated()
    {
        HideReadyPrompt();
    }

    // -- MUTATED ENDED --
    private void OnMutatedEnded()
    {
        HideReadyPrompt();
    }

    // -- SHOW READY PROMPT --
    private void ShowReadyPrompt()
    {
        if (readyPrompt != null && !readyPrompt.activeSelf)
        {
            readyPrompt.SetActive(true);
            promptRect.SetParent(parentRect, false);
            promptRect.anchoredPosition = originalAnchoredPosition + readyPromptOffset;
        }

        if (!isSporeReadyActive)
        {
            isSporeReadyActive = true;
            AudioManager.Instance?.PlaySFX(sporeReadyClip, sporeReadyVolume);
        }
    }

    // -- HIDE READY PROMPT --
    private void HideReadyPrompt()
    {
        if (readyPrompt != null && readyPrompt.activeSelf)
            readyPrompt.SetActive(false);

        if (isSporeReadyActive)
        {
            isSporeReadyActive = false;  
        }
    }

    // -- UPDATE SHAKE --
    private void UpdateShake()
    {
        if (!hasOriginalPosition || shakeTarget == null || SporeManager.Instance == null)
            return;

        if (Time.timeScale == 0f)
        {
            shakeTarget.anchoredPosition = originalAnchoredPosition;
            return;
        }

        bool shouldShake = SporeManager.Instance.IsFull || SporeManager.Instance.IsMutated;

        if (!shouldShake)
        {
            shakeTarget.anchoredPosition = originalAnchoredPosition;
            return;
        }

        float time = Time.unscaledTime * shakeSpeed;
        float xOffset = Mathf.Sin(time) * shakeAmplitude;
        float yOffset = Mathf.Sin(time * 1.37f + 0.8f) * (shakeAmplitude * 0.5f);

        shakeTarget.anchoredPosition = originalAnchoredPosition + new Vector2(xOffset, yOffset);
    }

    // -- UPDATE COLOR --
    private void UpdateColor(float fillPercent)
    {
        if (meterConfig == null || fillImage == null)
            return;

        if (fillPercent >= 0.66f)
            fillImage.color = meterConfig.fullColor;
        else if (fillPercent >= 0.33f)
            fillImage.color = meterConfig.halfColor;
        else
            fillImage.color = meterConfig.lowColor;
    }
}