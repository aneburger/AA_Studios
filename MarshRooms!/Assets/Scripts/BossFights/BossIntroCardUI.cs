
using System;
using System.Collections;
using UnityEngine;

public class BossIntroCardUI : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] private RectTransform leftElement;
    [SerializeField] private RectTransform centerElement;
    [SerializeField] private RectTransform rightElement;

    [Header("Timing (seconds, unscaled)")]
    [SerializeField] private float slideInDuration = 0.45f;
    [SerializeField] private float centerDelay = 0.2f;
    [SerializeField] private float centerPopDuration = 0.25f;
    [SerializeField] private float holdDuration = 1.6f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Motion")]
    [SerializeField] private float slideDistance = 1400f;
    [SerializeField] private float centerStartScale = 2.5f;

    [Header("Audio / FX")]
    [SerializeField] private AudioClip slideClip;
    [Range(0f, 1f)] [SerializeField] private float slideVolume = 1f;
    [SerializeField] private AudioClip vsClip;
    [Range(0f, 1f)] [SerializeField] private float vsVolume = 1f;
    [SerializeField] private float vsShakeForce = 0.4f;

    private CanvasGroup canvasGroup;

    // -- PLAY --
    public IEnumerator Play()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        gameObject.SetActive(true);
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        Vector2 leftEnd = leftElement != null ? leftElement.anchoredPosition : Vector2.zero;
        Vector2 rightEnd = rightElement != null ? rightElement.anchoredPosition : Vector2.zero;
        Vector2 leftStart = leftEnd + Vector2.left * slideDistance;
        Vector2 rightStart = rightEnd + Vector2.right * slideDistance;

        if (leftElement != null) leftElement.anchoredPosition = leftStart;
        if (rightElement != null) rightElement.anchoredPosition = rightStart;
        if (centerElement != null) centerElement.gameObject.SetActive(false);

        AudioManager.Instance?.PlaySFX(slideClip, slideVolume);

        // Slide the two sides in
        yield return Tween(slideInDuration, k =>
        {
            float e = EaseOutCubic(k);
            if (leftElement != null) leftElement.anchoredPosition = Vector2.LerpUnclamped(leftStart, leftEnd, e);
            if (rightElement != null) rightElement.anchoredPosition = Vector2.LerpUnclamped(rightStart, rightEnd, e);
        });

        yield return new WaitForSecondsRealtime(centerDelay);

        // VS slams in
        if (centerElement != null)
        {
            centerElement.gameObject.SetActive(true);
            centerElement.localScale = Vector3.one * centerStartScale;
            AudioManager.Instance?.PlaySFX(vsClip, vsVolume);

            yield return Tween(centerPopDuration, k =>
                centerElement.localScale = Vector3.one * Mathf.Lerp(centerStartScale, 1f, EaseOutCubic(k)));

            ScreenEffects.Instance?.ShakeScreen(vsShakeForce);
        }

        yield return new WaitForSecondsRealtime(holdDuration);

        // Fade out
        if (canvasGroup != null)
            yield return Tween(fadeOutDuration, k => canvasGroup.alpha = 1f - k);

        gameObject.SetActive(false);
    }

    // -- HELPERS --
    private static IEnumerator Tween(float duration, Action<float> apply)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            apply(Mathf.Clamp01(t / duration));
            yield return null;
        }
        apply(1f);
    }

    private static float EaseOutCubic(float k)
    {
        float inv = 1f - k;
        return 1f - inv * inv * inv;
    }
}
