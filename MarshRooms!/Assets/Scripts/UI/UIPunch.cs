// Reusable UI juice!

using UnityEngine;
using UnityEngine.UI;

public class UIPunch : MonoBehaviour
{
    private RectTransform rect;
    private Graphic graphic;

    // punch
    private float punchTimer = -1f;
    private float punchDuration;
    private float punchAmount;
    private Vector3 punchBaseScale = Vector3.one;

    // shake
    private float shakeTimer = -1f;
    private float shakeDuration;
    private float shakeAmount;
    private Vector2 shakeBasePos;

    // flash
    private float flashTimer = -1f;
    private float flashDuration;
    private Color flashTint;
    private Color flashBaseColor;
    private Material flashMaterial;
    private Material flashBaseMaterial;

    // pulse
    private bool pulsing;
    private float pulseInterval;
    private float pulseAmount;
    private float pulseTimer;

    // -- INIT --
    private void Init()
    {
        if (rect != null) return;
        rect = (RectTransform)transform;
        graphic = GetComponent<Graphic>();
    }

    // -- PUNCH --
    public void Punch(float amount = 0.3f, float duration = 0.2f)
    {
        Init();
        if (punchTimer < 0f) punchBaseScale = rect.localScale;

        punchAmount = amount;
        punchDuration = duration;
        punchTimer = 0f;
    }

    // -- SHAKE --
    public void Shake(float amount = 2f, float duration = 0.25f)
    {
        Init();
        if (shakeTimer < 0f) shakeBasePos = rect.anchoredPosition;

        shakeAmount = amount;
        shakeDuration = duration;
        shakeTimer = 0f;
    }

    // -- FLASH --
    public void Flash(Color tint, float duration = 0.15f, Material material = null)
    {
        Init();
        if (graphic == null) return;

        if (flashTimer < 0f)
        {
            flashBaseColor = graphic.color;
            flashBaseMaterial = graphic.material;
        }

        flashTint = tint;
        flashDuration = duration;
        flashMaterial = material;
        flashTimer = 0f;

        if (flashMaterial != null)
            graphic.material = flashMaterial;
    }

    // -- START PULSE --
    public void StartPulse(float interval, float amount)
    {
        pulseInterval = interval;
        pulseAmount = amount;

        if (pulsing) return;

        pulsing = true;
        pulseTimer = interval; // first beat happens right away
    }

    // -- STOP PULSE --
    public void StopPulse()
    {
        pulsing = false;
    }

    // -- UPDATE --
    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // paused

        UpdatePulse(dt);
        UpdatePunch(dt);
        UpdateShake(dt);
        UpdateFlash(dt);
    }

    // -- UPDATE PULSE --
    private void UpdatePulse(float dt)
    {
        if (!pulsing) return;

        pulseTimer += dt;
        if (pulseTimer >= pulseInterval)
        {
            pulseTimer = 0f;
            Punch(pulseAmount, 0.18f);
        }
    }

    // -- UPDATE PUNCH --
    private void UpdatePunch(float dt)
    {
        if (punchTimer < 0f) return;

        punchTimer += dt;
        float t = punchTimer / punchDuration;

        if (t >= 1f)
        {
            rect.localScale = punchBaseScale;
            punchTimer = -1f;
            return;
        }

        rect.localScale = punchBaseScale * (1f + punchAmount * Mathf.Sin(t * Mathf.PI));
    }

    // -- UPDATE SHAKE --
    private void UpdateShake(float dt)
    {
        if (shakeTimer < 0f) return;

        shakeTimer += dt;
        float t = shakeTimer / shakeDuration;

        if (t >= 1f)
        {
            rect.anchoredPosition = shakeBasePos;
            shakeTimer = -1f;
            return;
        }

        Vector2 offset = Random.insideUnitCircle * shakeAmount * (1f - t);
        offset = new Vector2(Mathf.Round(offset.x), Mathf.Round(offset.y));
        rect.anchoredPosition = shakeBasePos + offset;
    }

    // -- UPDATE FLASH --
    private void UpdateFlash(float dt)
    {
        if (flashTimer < 0f) return;

        flashTimer += dt;
        float t = flashTimer / flashDuration;

        if (t >= 1f)
        {
            EndFlash();
            return;
        }

        if (flashMaterial == null)
            graphic.color = Color.Lerp(flashTint, flashBaseColor, t);
    }

    // -- END FLASH --
    private void EndFlash()
    {
        graphic.color = flashBaseColor;
        graphic.material = flashBaseMaterial;
        flashTimer = -1f;
    }

    // -- ON DISABLE --
    private void OnDisable()
    {
        if (rect == null) return;

        if (punchTimer >= 0f)
        {
            rect.localScale = punchBaseScale;
            punchTimer = -1f;
        }

        if (shakeTimer >= 0f)
        {
            rect.anchoredPosition = shakeBasePos;
            shakeTimer = -1f;
        }

        if (flashTimer >= 0f && graphic != null)
            EndFlash();
    }
}