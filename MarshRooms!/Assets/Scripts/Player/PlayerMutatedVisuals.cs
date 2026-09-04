using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using Unity.Cinemachine;

public class PlayerMutatedVisuals : MonoBehaviour
{
    [Header("Glow")]
    [SerializeField] private Light2D sporeLight;

    [Header("Outline")]
    [SerializeField] private Color mutatedOutlineColor = new Color(0.4f, 1f, 0.4f);
    [SerializeField] private float outlineThickness = 1f;

    [Header("Particles")]
    [SerializeField] private ParticleSystem mutatedTrail;
    [SerializeField] private ParticleSystem mutatedShootBurst;

    [Header("Flicker Settings")]
    [SerializeField] private float flickerThreshold = 0.25f;
    [SerializeField] private float flickerInterval = 0.1f;

    [Header("Activation Effects")]
    [SerializeField] private GameObject activationVFX;
    [SerializeField] private ParticleSystem activationBurst;
    [SerializeField] private Sprite greenFlashSprite;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private float shakeForce = 0.5f;
    [SerializeField] private float freezeDuration = 0.15f;
    [SerializeField] private float freezeTimeScale = 0.05f;

    [Header("Audio")]
    [SerializeField] private AudioClip activationClip;
    [Range(0f, 1f)] public float activationVolume;
    [SerializeField] private AudioClip deactivationClip;
    [Range(0f, 1f)] public float deactivationVolume;

    private Coroutine flickerCoroutine;
    private bool isFlickering = false;

    private bool isActive = false;

    private static readonly int OutlineEnabledID = Shader.PropertyToID("_OutlineEnabled");
    private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineThicknessID = Shader.PropertyToID("_OutlineThickness");

    private SpriteRenderer[] playerRenderers;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        playerRenderers = System.Array.FindAll(
            GetComponentsInChildren<SpriteRenderer>(),
            sr => sr.gameObject.name != "PlayerShadow"
            && sr.gameObject.name != "MutateGlow"
        );
    }

    private void Start()
    {
        SporeManager.Instance.OnMutatedActivated += OnActivated;
        SporeManager.Instance.OnMutatedEnded += OnEnded;
        SporeManager.Instance.OnMutatedDrainTick += OnDrainTick;
    }

    private void OnDisable()
    {
        if (SporeManager.Instance == null) return;
        SporeManager.Instance.OnMutatedActivated -= OnActivated;
        SporeManager.Instance.OnMutatedEnded -= OnEnded;
        SporeManager.Instance.OnMutatedDrainTick -= OnDrainTick;
    }

    private void OnEnded()
    {
        isFlickering = false;
        if (flickerCoroutine != null) StopCoroutine(flickerCoroutine);

        ApplyVisibility(false);
        isActive = false;

        AudioManager.Instance.PlaySFXWithPitch(deactivationClip, deactivationVolume, 0.1f);
        if (activationBurst != null)
            activationBurst.Play();
    }

    private void OnActivated()
    {
        isActive = true;
        isFlickering = false;

        ApplyVisibility(true);

        StartCoroutine(ActivationSequence());
    }

    // -- PUBLIC: SET ALL EFFECTS VISIBLE -- 
    public void SetEffectsVisible(bool visible)
    {
        if (!isActive) return;
        ApplyVisibility(visible);
    }

    // -- INTERNAL: actually applies visibility, no gating --
    private void ApplyVisibility(bool visible)
    {
        if (sporeLight != null) sporeLight.enabled = visible;

        if (mutatedTrail != null)
        {
            if (visible) mutatedTrail.Play();
            else mutatedTrail.Stop();
        }

        SetOutline(visible);
    }

    private void SetOutline(bool enabled)
    {
        foreach (var sr in playerRenderers)
        {
            sr.GetPropertyBlock(mpb);
            mpb.SetFloat(OutlineEnabledID, enabled ? 1f : 0f);
            mpb.SetFloat(OutlineThicknessID, enabled ? outlineThickness : 0f);
            mpb.SetColor(OutlineColorID, mutatedOutlineColor);
            sr.SetPropertyBlock(mpb);
        }
    }

    public void PlayShootBurst(Vector3 position)
    {
        if (mutatedShootBurst == null) return;
        mutatedShootBurst.transform.position = position;
        mutatedShootBurst.Play();
    }

    private void OnDrainTick(float normalizedRemaining)
    {
        if (normalizedRemaining <= flickerThreshold && !isFlickering)
        {
            isFlickering = true;
            flickerCoroutine = StartCoroutine(FlickerRoutine());
        }
    }

    private IEnumerator FlickerRoutine()
    {
        while (SporeManager.Instance.IsMutated)
        {
            SetOutline(false);
            if (sporeLight != null) sporeLight.enabled = false;
            yield return new WaitForSeconds(flickerInterval);

            SetOutline(true);
            if (sporeLight != null) sporeLight.enabled = true;
            yield return new WaitForSeconds(flickerInterval);
        }
    }

    private IEnumerator ActivationSequence()
    {
        // Screen shake
        impulseSource?.GenerateImpulse(shakeForce);

        // Green flash
        ScreenEffects.Instance.Flash(greenFlashSprite, 0.5f, 0.15f);

        // Sound
        AudioManager.Instance.PlaySFXWithPitch(activationClip, activationVolume, 0.1f);

        // Spawn explosion VFX at player position
        if (activationVFX != null)
            Instantiate(activationVFX, transform.position, Quaternion.identity);

        // Activation burst particles
        if (activationBurst != null)
            activationBurst.Play();

        // Freeze moment
        Time.timeScale = freezeTimeScale;
        yield return new WaitForSecondsRealtime(freezeDuration);
        Time.timeScale = 1f;
    }

    // -- RESET SILENTLY --
    public void ResetStateSilently()
    {
        isFlickering = false;
        if (flickerCoroutine != null) StopCoroutine(flickerCoroutine);

        ApplyVisibility(false);
        isActive = false;
    }

}