using System.Collections;
using UnityEngine;
using TMPro;

public class CrowdBubble : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshPro text;
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private bool autoSizeBackground = true;
    [SerializeField] private Vector2 padding = new Vector2(0.25f, 0.15f);

    [Header("Timing")]
    [SerializeField] private float popDuration = 0.12f;
    [SerializeField] private float popOvershoot = 1.2f;
    [SerializeField] private float settleDuration = 0.08f;
    [SerializeField] private float holdTime = 1.4f;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float riseDistance = 0.15f;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "";
    [SerializeField] private int sortingOrder = 500;

    public float TotalLifetime => popDuration + settleDuration + holdTime + fadeDuration;

    private Transform followTarget;
    private Vector3 followOffset;
    private float age;
    private Color bgBaseColor = Color.white;

    // -- SHOW --
    public void Show(string line, Transform target, Vector3 offset)
    {
        if (text == null)
        {
            Debug.LogWarning("CrowdBubble: no TextMeshPro assigned.", this);
            Destroy(gameObject);
            return;
        }

        followTarget = target;
        followOffset = offset;

        text.text = line;
        text.ForceMeshUpdate();

        ApplySorting();
        SizeBackground();
        if (background != null) bgBaseColor = background.color;

        StartCoroutine(Run());
    }

    private void ApplySorting()
    {
        if (!string.IsNullOrEmpty(sortingLayerName))
        {
            text.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
            if (background != null) background.sortingLayerName = sortingLayerName;
        }

        text.sortingOrder = sortingOrder + 1;
        if (background != null) background.sortingOrder = sortingOrder;
    }

    private void SizeBackground()
    {
        if (!autoSizeBackground || background == null) return;

        Bounds b = text.textBounds;
        background.drawMode = SpriteDrawMode.Sliced;
        background.size = (Vector2)b.size + padding * 2f;
        background.transform.localPosition = text.transform.localPosition + b.center;
    }

    // -- LIFETIME --
    private IEnumerator Run()
    {
        Vector3 fullScale = transform.localScale;
        transform.localScale = Vector3.zero;
        SetAlpha(1f);
        Follow();

        yield return Phase(popDuration, p => transform.localScale = fullScale * Mathf.Lerp(0f, popOvershoot, p));
        yield return Phase(settleDuration, p => transform.localScale = fullScale * Mathf.Lerp(popOvershoot, 1f, p));
        yield return Phase(holdTime, p => { });
        yield return Phase(fadeDuration, p => SetAlpha(1f - p));

        Destroy(gameObject);
    }

    private IEnumerator Phase(float duration, System.Action<float> step)
    {
        float t = 0f;
        while (t < duration)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            age += dt;
            step(Mathf.Clamp01(t / Mathf.Max(0.0001f, duration)));
            Follow();
            yield return null;
        }
        step(1f);
    }

    private void Follow()
    {
        if (followTarget == null) return;
        float rise = riseDistance * Mathf.Clamp01(age / Mathf.Max(0.0001f, TotalLifetime));
        transform.position = followTarget.position + followOffset + Vector3.up * rise;
    }

    private void SetAlpha(float a)
    {
        if (text != null) text.alpha = a;
        if (background != null)
            background.color = new Color(bgBaseColor.r, bgBaseColor.g, bgBaseColor.b, bgBaseColor.a * a);
    }
}