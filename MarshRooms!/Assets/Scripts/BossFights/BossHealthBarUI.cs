using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform skullIcon;

    [Header("Colours")]
    [SerializeField] private Color fullColor;
    [SerializeField] private Color halfColor;
    [SerializeField] private Color lowColor;

    [Header("Behaviour")]
    [SerializeField] private float followSpeed = 12f;
    [SerializeField] private float punchScale = 0.35f;
    [SerializeField] private float punchDuration = 0.3f;

    private BossHealth boundBoss;
    private float targetFill = 1f;
    private bool introRunning;
    private Coroutine punchRoutine;

    // -- BIND --
    public void Bind(BossHealth boss)
    {
        Unbind();
        boundBoss = boss;
        if (boundBoss == null) return;

        boundBoss.OnHealthChanged += HandleHealthChanged;
        targetFill = boundBoss.HealthFraction;
    }

    private void Unbind()
    {
        if (boundBoss != null) boundBoss.OnHealthChanged -= HandleHealthChanged;
        boundBoss = null;
    }

    private void HandleHealthChanged(float current, float max)
    {
        targetFill = max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }

    // -- SHOW / HIDE --
    public void Show()
    {
        gameObject.SetActive(true);
        introRunning = true;
        SetFill(0f);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // -- INTRO FILL --
    public IEnumerator PlayIntroFill(float duration)
    {
        introRunning = true;
        float goal = boundBoss != null ? boundBoss.HealthFraction : 1f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            SetFill(goal * e);
            yield return null;
        }

        SetFill(goal);
        targetFill = goal;
        Punch();

        introRunning = false;
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
    }

    // -- UPDATE --
    private void Update()
    {
        if (introRunning || fillImage == null) return;

        float current = fillImage.fillAmount;
        float next = Mathf.Lerp(current, targetFill, 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime));
        if (Mathf.Abs(next - targetFill) < 0.001f) next = targetFill;
        SetFill(next);
    }

    private void SetFill(float value)
    {
        if (fillImage == null) return;

        fillImage.fillAmount = value;
        fillImage.color = GetHealthColor(value);
    }

    // Same blend as EnemyHealthBar:
    private Color GetHealthColor(float t)
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