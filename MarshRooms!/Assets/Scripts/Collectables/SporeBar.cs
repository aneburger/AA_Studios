using UnityEngine;
using UnityEngine.UI;

public class SporeBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private SporeMeterConfig meterConfig;

    private float targetFillAmount = 0f;

    private void Start()
    {
        if (fillImage == null)
            fillImage = GetComponentInChildren<Image>();

        if (SporeManager.Instance != null)
        {
            SporeManager.Instance.OnSporeCountChanged += UpdateSporeBar;
            SporeManager.Instance.OnMutatedDrainTick += UpdateDrain;
            SporeManager.Instance.OnMutatedEnded += OnMutatedEnded;
            UpdateSporeBar(SporeManager.Instance.GetCurrentSpores(), SporeManager.Instance.GetMaxSpores());
        }
    }

    private void OnDestroy()
    {
        if (SporeManager.Instance != null)
        {
            SporeManager.Instance.OnSporeCountChanged -= UpdateSporeBar;
            SporeManager.Instance.OnMutatedDrainTick -= UpdateDrain;
            SporeManager.Instance.OnMutatedEnded -= OnMutatedEnded;
        }
    }

    private void Update()
    {
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount, meterConfig.fillSmoothSpeed * Time.deltaTime);
    }

    private void UpdateSporeBar(int currentSpores, int maxSpores)
    {
        if (fillImage == null || maxSpores <= 0) return;
        targetFillAmount = (float)currentSpores / maxSpores;
        UpdateColor(targetFillAmount);
    }

    private void UpdateDrain(float normalizedRemaining)
    {
        targetFillAmount = normalizedRemaining;
        fillImage.color = meterConfig.fullColor;
    }

    private void OnMutatedEnded()
    {
        targetFillAmount = 0f;
        UpdateColor(0f);
    }

    private void UpdateColor(float fillPercent)
    {
        if (fillPercent >= 0.66f)
            fillImage.color = meterConfig.fullColor;
        else if (fillPercent >= 0.33f)
            fillImage.color = meterConfig.halfColor;
        else
            fillImage.color = meterConfig.lowColor;
    }
}