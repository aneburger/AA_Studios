using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerMutatedVisuals : MonoBehaviour
{
    [Header("Glow")]
    [SerializeField] private Light2D sporeLight;

    [Header("Outline")]
    [SerializeField] private Color mutatedOutlineColor = new Color(0.4f, 1f, 0.4f);
    [SerializeField] private float outlineThickness = 1f;

    private static readonly int OutlineEnabledID = Shader.PropertyToID("_OutlineEnabled");
    private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineThicknessID = Shader.PropertyToID("_OutlineThickness");

    private SpriteRenderer[] playerRenderers;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();

        // Grab only the renderers we want
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
    }

    private void OnDisable()
    {
        if (SporeManager.Instance == null) return;
        SporeManager.Instance.OnMutatedActivated -= OnActivated;
        SporeManager.Instance.OnMutatedEnded -= OnEnded;
    }

    private void OnActivated()
    {
        if (sporeLight != null) sporeLight.enabled = true;
        ScreenEffects.Instance.ShowMutatedVignette();
        SetOutline(true);
    }

    private void OnEnded()
    {
        if (sporeLight != null) sporeLight.enabled = false;
        ScreenEffects.Instance.HideMutatedVignette();
        SetOutline(false);
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

    public void SetOutlineAndLightVisible(bool visible)
    {
        if (!SporeManager.Instance.IsMutated) return;
        
        if (sporeLight != null) sporeLight.enabled = visible;
        SetOutline(visible);
    }
}