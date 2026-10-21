using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class WavePip : MonoBehaviour
{
    [Header("Pop Effect")]
    [SerializeField] private float popDuration = 0.2f;
    [SerializeField] private float popScale = 0.3f;

    private Image image;
    private float popTimer = -1f;
    private float currentPopScale;
    private float currentPopDuration;

    public bool IsFinal { get; private set; }

    // -- AWAKE --
    private void Awake()
    {
        image = GetComponent<Image>();
    }

    // -- INIT --
    public void Init(bool isFinal, Sprite startSprite)
    {
        if (image == null) image = GetComponent<Image>();
        IsFinal = isFinal;
        SetSprite(startSprite);
    }

    // -- SET SPRITE --
    public void SetSprite(Sprite sprite)
    {
        if (sprite == null || image.sprite == sprite) return;
        image.sprite = sprite;
        image.SetNativeSize();
    }

    // -- PLAY POP --
    public void PlayPop()
    {
        PlayPop(popScale, popDuration);
    }

    // -- PLAY POP (custom size) --
    public void PlayPop(float scale, float duration)
    {
        currentPopScale = scale;
        currentPopDuration = duration;
        popTimer = 0f;
    }

    // -- UPDATE --
    private void Update()
    {
        if (popTimer < 0f) return;

        popTimer += Time.deltaTime;
        float t = popTimer / currentPopDuration;

        if (t >= 1f)
        {
            transform.localScale = Vector3.one;
            popTimer = -1f;
            return;
        }

        transform.localScale = Vector3.one * (1f + currentPopScale * Mathf.Sin(t * Mathf.PI));
    }
}