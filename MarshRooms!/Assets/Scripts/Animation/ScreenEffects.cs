using UnityEngine;
using UnityEngine.UI;

public class ScreenEffects : MonoBehaviour
{
    public static ScreenEffects Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Image flashImage;

    [Header("Sprites")]
    [SerializeField] private Sprite damageScreen;

    private float flashTimer = 0f;
    private float flashDuration = 0f;
    private float flashAlpha = 0f;

    // -- AWAKE --
    private void Awake()
    {
        Instance = this;
        SetAlpha(0f);
    }

    // -- UPDATE --
    private void Update()
    {
        if (flashTimer <= 0f) return;

        flashTimer -= Time.deltaTime;
        float alpha = Mathf.Lerp(0f, flashAlpha, flashTimer / flashDuration);
        SetAlpha(alpha);

        if (flashTimer <= 0f)
            SetAlpha(0f);
    }

    // -- FLASH --
    public void Flash(Sprite sprite, float alpha, float duration)
    {
        flashImage.sprite = sprite;
        flashAlpha = alpha;
        flashDuration = duration;
        flashTimer = duration;
        SetAlpha(alpha);
    }

    // -- SET ALPHA --
    private void SetAlpha(float alpha)
    {
        Color c = flashImage.color;
        c.a = alpha;
        flashImage.color = c;
    }

    // -- DAMAGE --
    public void FlashDamage()
    {
        Flash(damageScreen, 0.4f, 0.2f);
    }
}