using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public class ScreenEffects : MonoBehaviour
{
    public static ScreenEffects Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Image flashImage;
    [SerializeField] private Image vignetteImage;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Sprites")]
    [SerializeField] private Sprite damageScreen;

    [Header("Settings")]
    [SerializeField] private float vignetteFadeSpeed = 2f;
    [SerializeField] private float lowHealthPulseSpeed = 2f;
    [SerializeField] private float lowHealthMaxAlpha = 0.5f;

    private float flashTimer = 0f;
    private float flashDuration = 0f;
    private float flashAlpha = 0f;

    private float vignetteTargetAlpha = 0f;
    private float vignetteCurrentAlpha = 0f;

    private bool isLowHealth = false;
    private float pulseTimer = 0f;

    // -- AWAKE --
    private void Awake()
    {
        Instance = this;
        SetFlashAlpha(0f);
        SetVignetteAlpha(0f);
    }

    // -- UPDATE --
    private void Update()
    {
        // Flash fade
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            float alpha = Mathf.Lerp(0f, flashAlpha, flashTimer / flashDuration);
            SetFlashAlpha(alpha);
            if (flashTimer <= 0f) SetFlashAlpha(0f);
        }

        // Low health pulse
        if (isLowHealth)
        {
            pulseTimer += Time.deltaTime * lowHealthPulseSpeed;
            float pulseAlpha = (Mathf.Sin(pulseTimer) + 1f) / 2f * lowHealthMaxAlpha;
            vignetteTargetAlpha = pulseAlpha;
        }

        // Vignette smooth fade
        vignetteCurrentAlpha = Mathf.Lerp(vignetteCurrentAlpha, vignetteTargetAlpha, vignetteFadeSpeed * Time.deltaTime);
        SetVignetteAlpha(vignetteCurrentAlpha);
    }

    // -- FLASH --
    public void Flash(Sprite sprite, float alpha, float duration)
    {
        flashImage.sprite = sprite;
        flashAlpha = alpha;
        flashDuration = duration;
        flashTimer = duration;
        SetFlashAlpha(alpha);
    }

    // -- DAMAGE --
    public void FlashDamage()
    {
        Flash(damageScreen, 0.3f, 0.2f);
        impulseSource?.GenerateImpulse(0.15f);
    }

    // -- SET LOW HEALTH --
    public void SetLowHealth(bool low)
    {
        isLowHealth = low;

        if (low)
        {
            vignetteImage.sprite = damageScreen;
            pulseTimer = 0f;
        }
        else
        {
            vignetteTargetAlpha = 0f;
        }
    }

    // -- SET FLASH ALPHA --
    private void SetFlashAlpha(float alpha)
    {
        Color c = flashImage.color;
        c.a = alpha;
        flashImage.color = c;
    }

    // -- SET VIGNETTE ALPHA --
    private void SetVignetteAlpha(float alpha)
    {
        Color c = vignetteImage.color;
        c.a = alpha;
        vignetteImage.color = c;
    }
}