using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerMutatedVisuals : MonoBehaviour
{
    [SerializeField] private Light2D sporeLight;

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
    }

    private void OnEnded()
    {
        if (sporeLight != null) sporeLight.enabled = false;
        ScreenEffects.Instance.HideMutatedVignette();
    }
}