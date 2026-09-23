// Boss health bar
// The bar shows the health of the CURRENT phase

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform skullIcon;

    [Header("Colours (same idea as EnemyHealthBar)")]
    [SerializeField] private Color fullColor = new Color(0.35f, 0.85f, 0.35f);
    [SerializeField] private Color halfColor = new Color(1f, 0.6f, 0.15f);
    [SerializeField] private Color lowColor = new Color(0.9f, 0.2f, 0.2f);

    [Header("Behaviour")]
    [SerializeField] private float followSpeed = 12f;
    [SerializeField] private float punchScale = 0.35f;
    [SerializeField] private float punchDuration = 0.3f;

    [Header("Skull Pulse (phase two)")]
    [SerializeField] private float skullPulseScale = 0.15f;
    [SerializeField] private float skullPulseSpeed = 5f;

    [Header("Fill Shake (intro fill + phase refill)")]
    [SerializeField] private float fillShakeInterval = 0.15f;
    [SerializeField] private float fillShakeForce = 0.1f;

    private BossHealth boundBoss;
    private float targetFill = 1f;
    private bool holdFollow;
    private bool pulsing;
    private Coroutine punchRoutine;

    // -- BIND --
    public void Bind(BossHealth boss)
    {
        Unbind();
        boundBoss = boss;
        if (boundBoss == null) return;

        boundBoss.OnHealthChanged += HandleHealthChanged;
        targetFill = boundBoss.PhaseFraction;
    }
    
    private void Unbind()
    {
        if (boundBoss != null) boundBoss.OnHealthChanged -= HandleHealthChanged;
        boundBoss = null;
    }

    private void HandleHealthChanged(float current, float max)
    {
        targetFill = boundBoss != null ? boundBoss.PhaseFraction : 0f;
    }

    // -- SHOW / HIDE --
    public void Show()
    {
        gameObject.SetActive(true);
        holdFollow = true;
        pulsing = false;
        SetFill(0f);
    }

    public void Hide()
    {
        pulsing = false;
        gameObject.SetActive(false);
    }

    // -- INTRO FILL --
    public IEnumerator PlayIntroFill(float duration)
    {
        yield return FillRoutine(0f, GoalFill(), duration);
    }

    // -- PHASE REFILL --
    public IEnumerator PlayPhaseRefill(float duration)
    {
        float start = fillImage != null ? fillImage.fillAmount : 0f;
        yield return FillRoutine(start, GoalFill(), duration);
    }

    private float GoalFill()
    {
        return boundBoss != null ? boundBoss.PhaseFraction : 1f;
    }

    private IEnumerator FillRoutine(float from, float to, float duration)
    {
        holdFollow = true;

        float t = 0f;
        float nextShake = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            SetFill(Mathf.Lerp(from, to, e));

            if (t >= nextShake)
            {
                ScreenEffects.Instance?.ShakeScreen(fillShakeForce);
                nextShake += fillShakeInterval;
            }

            yield return null;
        }

        SetFill(to);
        targetFill = to;
        Punch();

        holdFollow = false;
    }

    // -- SKULL PULSE --
    public void SetSkullPulse(bool on)
    {
        pulsing = on;

        if (!on && skullIcon != null && punchRoutine == null)
            skullIcon.localScale = Vector3.one;
    }

    // -- PUNCH --
    public void Punch()
    {
        if (skullIcon == null || !gameObject.activeInHierarchy) return;
        if (punchRoutine != null) StopCoroutine(punchRoutine);
        punchRoutine = StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        float t = 0f;
        while (t < punchDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / punchDuration);
            skullIcon.localScale = Vector3.one * (1f + punchScale * (1f - k));
            yield return null;
        }

        skullIcon.localScale = Vector3.one;
        punchRoutine = null;
    }

    // -- UPDATE --
    private void Update()
    {
        if (pulsing && skullIcon != null && punchRoutine == null)
        {
            float wave = (Mathf.Sin(Time.unscaledTime * skullPulseSpeed) + 1f) * 0.5f;
            skullIcon.localScale = Vector3.one * (1f + skullPulseScale * wave);
        }

        if (holdFollow || fillImage == null) return;

        float current = fillImage.fillAmount;
        float next = Mathf.Lerp(current, targetFill, 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime));
        if (Mathf.Abs(next - targetFill) < 0.001f) next = targetFill;
        SetFill(next);
    }

    // -- SET FILL --
    private void SetFill(float value)
    {
        if (fillImage == null) return;

        fillImage.fillAmount = value;
        fillImage.color = GetHealthColour(value);
    }

    // -- GET HEALTH COLOUR --
    private Color GetHealthColour(float t)
    {
        Color c = t > 0.5f
            ? Color.Lerp(halfColor, fullColor, (t - 0.5f) * 2f)
            : Color.Lerp(lowColor, halfColor, t * 2f);

        c.a = 1f;
        return c;
    }

    // -- CLEANUP --
    private void OnDestroy()
    {
        Unbind();
    }
}