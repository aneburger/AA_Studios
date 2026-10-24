using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Credits : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float holdDuration = 6f;
    [SerializeField] private float fadeOutDuration = 1f;

    [SerializeField] private UnityEvent onFinished;

    private void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null) group.alpha = 0f;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        yield return Fade(0f, 1f, fadeInDuration);
        yield return new WaitForSecondsRealtime(holdDuration);
        yield return Fade(1f, 0f, fadeOutDuration);

        onFinished?.Invoke();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (group == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / Mathf.Max(0.0001f, duration)));
            yield return null;
        }
        group.alpha = to;
    }
}