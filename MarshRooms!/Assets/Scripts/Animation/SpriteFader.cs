// Reusable fade in / fade out for any SpriteRenderer.

using UnityEngine;
using System.Collections;
using System;

public class SpriteFader : MonoBehaviour
{
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private bool startInvisible = true;

    private SpriteRenderer sr;
    private Coroutine currentFade;

    // -- AWAKE --
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        if (startInvisible)
            SetAlpha(0f);
    }

    // -- FADE IN --
    public void FadeIn(Action onComplete = null)
    {
        if (currentFade != null) StopCoroutine(currentFade);
        gameObject.SetActive(true);
        currentFade = StartCoroutine(FadeRoutine(sr.color.a, 1f, fadeInDuration, onComplete));
    }

    // -- FADE OUT --
    public void FadeOut(Action onComplete = null)
    {
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeRoutine(sr.color.a, 0f, fadeOutDuration, () =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        }));
    }

    // -- FADE IN WITH DURATION OVERRIDE --
    public void FadeIn(float duration, Action onComplete = null)
    {
        if (currentFade != null) StopCoroutine(currentFade);
        gameObject.SetActive(true);
        currentFade = StartCoroutine(FadeRoutine(sr.color.a, 1f, duration, onComplete));
    }

    // -- FADE OUT WITH DURATION OVERRIDE --
    public void FadeOut(float duration, Action onComplete = null)
    {
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeRoutine(sr.color.a, 0f, duration, () =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        }));
    }

    // -- SET VISIBLE INSTANTLY --
    public void SetVisible(bool visible)
    {
        if (currentFade != null) StopCoroutine(currentFade);
        SetAlpha(visible ? 1f : 0f);
        gameObject.SetActive(visible);
    }

    // -- FADE ROUTINE --
    private IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete)
    {
        float elapsed = 0f;
        SetAlpha(from);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetAlpha(to);
        onComplete?.Invoke();
        currentFade = null;
    }

    // -- SET ALPHA --
    private void SetAlpha(float alpha)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}