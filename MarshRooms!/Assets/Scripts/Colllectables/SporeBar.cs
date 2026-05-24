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

        // subscribe to spore count changes
        if (SporeManager.Instance != null)
        {
            SporeManager.Instance.OnSporeCountChanged += UpdateSporeBar;
            UpdateSporeBar(SporeManager.Instance.GetCurrentSpores(), SporeManager.Instance.GetMaxSpores());
        }
    }

    private void OnDestroy()
    {
        if (SporeManager.Instance != null)
            SporeManager.Instance.OnSporeCountChanged -= UpdateSporeBar;
    }

    private void Update()
    {
        // smooth fill animation
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount,
                meterConfig.fillSmoothSpeed * Time.deltaTime);
        }
    }

    // -- UPDATE SPORE BAR --
    private void UpdateSporeBar(int currentSpores, int maxSpores)
    {
        if (fillImage == null || maxSpores <= 0) return;

        targetFillAmount = (float)currentSpores / maxSpores;

        // change color based on fill percentage
        float fillPercent = targetFillAmount;
        if (fillPercent >= 0.66f)
            fillImage.color = meterConfig.fullColor;
        else if (fillPercent >= 0.33f)
            fillImage.color = meterConfig.halfColor;
        else
            fillImage.color = meterConfig.lowColor;
    }
}