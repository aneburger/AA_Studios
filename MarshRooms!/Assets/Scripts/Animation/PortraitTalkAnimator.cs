using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class PortraitTalkAnimator : MonoBehaviour
{
    [SerializeField] private float squishAmount = 0.08f;
    [SerializeField] private float squishSpeed = 10f;

    private RectTransform rect;
    private Coroutine squishCoroutine;
    private Vector3 baseScale;
    private Vector2 baseAnchoredPosition;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        baseScale = rect.localScale;
        baseAnchoredPosition = rect.anchoredPosition;
    }

    public void SetTalking(bool talking)
    {
        if (squishCoroutine != null)
            StopCoroutine(squishCoroutine);

        if (talking)
        {
            squishCoroutine = StartCoroutine(SquishRoutine());
        }
        else
        {
            rect.localScale = baseScale;
            rect.anchoredPosition = baseAnchoredPosition;
        }
    }

    private IEnumerator SquishRoutine()
    {
        float t = 0f;
        float height = rect.rect.height;
        float pivotY = rect.pivot.y;

        while (true)
        {
            t += Time.unscaledDeltaTime * squishSpeed;

            float wave = Mathf.Sin(t);
            float squishFactor = wave < 0f ? wave : 0f;
            float scaleY = 1f + squishFactor * squishAmount;

            rect.localScale = new Vector3(baseScale.x, baseScale.y * scaleY, baseScale.z);
            
            float compensation = pivotY * height * (1f - scaleY);
            rect.anchoredPosition = baseAnchoredPosition - new Vector2(0f, compensation);

            yield return null;
        }
    }
}