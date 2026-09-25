using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossRewardPopupUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup overlay;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button okButton;

    [Header("Slide Animation")]
    [SerializeField] private Vector2 hiddenOffset = new Vector2(600f, 0f);
    [SerializeField] private float slideInDuration = 0.4f;
    [SerializeField] private float slideOutDuration = 0.3f;

    [Header("Overlay Fade")]
    [Range(0f, 1f)] [SerializeField] private float overlayMaxAlpha = 0.6f;
    [SerializeField] private float overlayFadeInDuration = 0.3f;
    [SerializeField] private float overlayFadeOutDuration = 0.25f;

    [Header("Audio")]
    [SerializeField] private AudioClip showClip;
    [Range(0f, 1f)] [SerializeField] private float showVolume = 1f;

    private Vector2 shownPosition;
    private Action onClosed;

    // -- AWAKE --
    private void Awake()
    {
        shownPosition = panel.anchoredPosition;
        if (overlay != null) overlay.alpha = 0f;

        SetVisible(false);

        if (okButton != null) okButton.onClick.AddListener(HandleOkClicked);
    }

    // -- VISIBILITY --
    private void SetVisible(bool visible)
    {
        if (rootGroup == null) return;
        rootGroup.alpha = visible ? 1f : 0f;
        rootGroup.interactable = visible;
        rootGroup.blocksRaycasts = visible;
    }

    // -- SHOW --
    public void Show(string title, string description, Action onClosedCallback)
    {
        onClosed = onClosedCallback;

        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;

        SetVisible(true);
        Time.timeScale = 0f;

        AudioManager.Instance?.PlaySFX(showClip, showVolume);

        StartCoroutine(FadeOverlay(0f, overlayMaxAlpha, overlayFadeInDuration));
        StartCoroutine(SlideIn());
    }

    // -- SLIDE IN --
    private IEnumerator SlideIn()
    {
        panel.anchoredPosition = shownPosition + hiddenOffset;

        float t = 0f;
        while (t < slideInDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / slideInDuration);
            panel.anchoredPosition = Vector2.Lerp(shownPosition + hiddenOffset, shownPosition, p);
            yield return null;
        }

        panel.anchoredPosition = shownPosition;
    }

    // -- OVERLAY FADE --
    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (overlay == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            overlay.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }

        overlay.alpha = to;
    }

    // -- OK CLICKED --
    private void HandleOkClicked()
    {
        StartCoroutine(SlideOutThenClose());
    }

    private IEnumerator SlideOutThenClose()
    {
        StartCoroutine(FadeOverlay(overlayMaxAlpha, 0f, overlayFadeOutDuration));

        float t = 0f;
        Vector2 start = panel.anchoredPosition;

        while (t < slideOutDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / slideOutDuration);
            panel.anchoredPosition = Vector2.Lerp(start, shownPosition + hiddenOffset, p);
            yield return null;
        }

        SetVisible(false);
        Time.timeScale = 1f;

        Action callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }
}