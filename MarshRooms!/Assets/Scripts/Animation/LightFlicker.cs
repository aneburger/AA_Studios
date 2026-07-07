using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class LightFlicker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform flameSprite;

    [Header("Breathing (base pulse)")]
    [SerializeField] private float breathSpeed = 1f;
    [SerializeField] private float breathIntensityAmount = 0.08f;
    [SerializeField] private float breathScaleAmount = 0.03f;

    [Header("Occasional Flicker")]
    [Tooltip("Average seconds between flicker bursts.")]
    [SerializeField] private float flickerIntervalMin = 3f;
    [SerializeField] private float flickerIntervalMax = 8f;
    [Tooltip("How long a single flicker burst lasts.")]
    [SerializeField] private float flickerDuration = 0.15f;
    [SerializeField] private float flickerIntensityDrop = 0.35f;

    [Header("Limits")]
    [SerializeField, Range(0f, 1f)] private float minIntensityFraction = 0.85f;
    [SerializeField, Range(0f, 1f)] private float minScaleFraction = 0.9f;

    [Header("Randomization")]
    [SerializeField] private bool randomizeSeed = true;

    private Light2D light2D;
    private float baseIntensity;
    private float baseScale;
    private float phaseOffset;

    private float nextFlickerTime;
    private float flickerTimer;
    private bool isFlickering;

    private void Awake()
    {
        light2D = GetComponent<Light2D>();
        baseIntensity = light2D.intensity;

        if (flameSprite != null)
            baseScale = flameSprite.localScale.x;

        phaseOffset = randomizeSeed ? Random.Range(0f, 100f) : 0f;
        ScheduleNextFlicker();
    }

    private void Update()
    {
        float breath = Mathf.Sin((Time.time + phaseOffset) * breathSpeed);
        float intensityValue = baseIntensity + breath * breathIntensityAmount;
        float scaleValue = baseScale + breath * breathScaleAmount;

        flickerTimer -= Time.deltaTime;
        if (!isFlickering && flickerTimer <= 0f)
        {
            isFlickering = true;
            flickerTimer = flickerDuration;
        }
        else if (isFlickering && flickerTimer <= 0f)
        {
            isFlickering = false;
            ScheduleNextFlicker();
        }

        if (isFlickering)
        {
            float dip = Mathf.PerlinNoise(Time.time * 40f, 0f) * flickerIntensityDrop;
            intensityValue -= dip;
        }

        float minIntensity = baseIntensity * minIntensityFraction;
        float minScale = baseScale * minScaleFraction;

        light2D.intensity = Mathf.Max(minIntensity, intensityValue);

        if (flameSprite != null)
        {
            float s = Mathf.Max(minScale, scaleValue);
            flameSprite.localScale = new Vector3(s, s, 1f);
        }
    }

    private void ScheduleNextFlicker()
    {
        flickerTimer = Random.Range(flickerIntervalMin, flickerIntervalMax);
    }
}