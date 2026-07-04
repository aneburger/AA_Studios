using UnityEngine;

public class ContinueIndicatorBob : MonoBehaviour
{
    private enum BobStyle { SideToSide, UpDown }

    [SerializeField] private BobStyle style = BobStyle.UpDown;
    [SerializeField] private float amount = 6f;
    [SerializeField] private float speed = 4f;

    private RectTransform rect;
    private Vector2 basePos;
    private float t;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        basePos = rect.anchoredPosition;
    }

    private void OnEnable()
    {
        t = 0f;
    }

    private void Update()
    {
        t += Time.unscaledDeltaTime * speed;
        float offset = Mathf.Sin(t) * amount;

        rect.anchoredPosition = style == BobStyle.UpDown
            ? basePos + new Vector2(0f, offset)
            : basePos + new Vector2(offset, 0f);
    }
}