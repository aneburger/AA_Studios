using UnityEngine;

public class DriftingCloud : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float minSpeed = 15f;
    [SerializeField] private float maxSpeed = 35f;

    private RectTransform rectTransform;
    private float speed;
    private float resetXPosition;
    private float despawnXPosition;
    private float fixedY;
    private bool initialised = false;

    // -- AWAKE --
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // -- START --
    private void Start()
    {
        RectTransform parentRect = rectTransform.parent as RectTransform;

        float halfParentWidth = parentRect.rect.width / 2f;
        float halfCloudWidth = rectTransform.rect.width / 2f;

        despawnXPosition = halfParentWidth + halfCloudWidth;
        resetXPosition = -halfParentWidth - halfCloudWidth;

        fixedY = rectTransform.anchoredPosition.y;

        speed = Random.Range(minSpeed, maxSpeed);
        fixedY = rectTransform.anchoredPosition.y;

        initialised = true;
    }

    // -- UPDATE --
    private void Update()
    {
        if (!initialised) return;

        Vector2 pos = rectTransform.anchoredPosition;
        pos.x += speed * Time.unscaledDeltaTime;

        if (pos.x > despawnXPosition)
        {
            pos.x = resetXPosition;
            speed = Random.Range(minSpeed, maxSpeed);
        }

        pos.y = fixedY;
        rectTransform.anchoredPosition = pos;
    }
}