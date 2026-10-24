using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Credits : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Credits Scroll")]
    [SerializeField] private RectTransform creditsContent;
    [SerializeField] private Vector2 scrollStartPosition;
    [SerializeField] private Vector2 scrollEndPosition;
    [SerializeField] private float titleHoldDuration = 5f;
    [SerializeField] private float scrollDuration = 30f;

    [Header("Cloud Movement")]
    [SerializeField] private RectTransform[] clouds;
    [SerializeField] private float cloudSpeed = 8f;
    [SerializeField] private float cloudLeftLimit = -1200f;
    [SerializeField] private float cloudRightLimit = 1200f;

    [Header("Completion")]
    [SerializeField] private UnityEvent onFinished;

    private Coroutine creditsCoroutine;
    private Coroutine cloudCoroutine;

    private void Awake()
    {
        if (group == null)
            group = GetComponent<CanvasGroup>();

        if (group != null)
            group.alpha = 0f;

        if (creditsContent != null)
            creditsContent.anchoredPosition = scrollStartPosition;
    }

    public void Show()
    {
        gameObject.SetActive(true);

        if (creditsCoroutine != null)
            StopCoroutine(creditsCoroutine);

        if (cloudCoroutine != null)
            StopCoroutine(cloudCoroutine);

        if (group != null)
            group.alpha = 0f;

        if (creditsContent != null)
            creditsContent.anchoredPosition = scrollStartPosition;

        cloudCoroutine = StartCoroutine(MoveClouds());
        creditsCoroutine = StartCoroutine(RunCredits());
    }

    private IEnumerator RunCredits()
    {
        yield return Fade(0f, 1f, fadeInDuration);

        yield return WaitRealtime(titleHoldDuration);

        yield return ScrollCredits();

        yield return Fade(1f, 0f, fadeOutDuration);

        if (cloudCoroutine != null)
            StopCoroutine(cloudCoroutine);

        onFinished?.Invoke();
    }

    private IEnumerator ScrollCredits()
    {
        if (creditsContent == null)
            yield break;

        float elapsed = 0f;
        float duration = Mathf.Max(0.0001f, scrollDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            creditsContent.anchoredPosition = Vector2.Lerp(
                scrollStartPosition,
                scrollEndPosition,
                progress);

            yield return null;
        }

        creditsContent.anchoredPosition = scrollEndPosition;
    }

    private IEnumerator MoveClouds()
    {
        while (true)
        {
            if (clouds != null)
            {
                foreach (RectTransform cloud in clouds)
                {
                    if (cloud == null)
                        continue;

                    Vector2 position = cloud.anchoredPosition;
                    position.x += cloudSpeed * Time.unscaledDeltaTime;

                    float cloudWidth = cloud.rect.width * cloud.lossyScale.x;

                    if (position.x - cloudWidth * 0.5f > cloudRightLimit)
                        position.x = cloudLeftLimit - cloudWidth * 0.5f;

                    cloud.anchoredPosition = position;
                }
            }

            yield return null;
        }
    }

    private IEnumerator WaitRealtime(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (group == null)
            yield break;

        float elapsed = 0f;
        float fadeDuration = Mathf.Max(0.0001f, duration);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            group.alpha = Mathf.Lerp(from, to, progress);

            yield return null;
        }

        group.alpha = to;
    }
}