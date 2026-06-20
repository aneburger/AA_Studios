using UnityEngine;

public class FadeOutSprite : MonoBehaviour
{
    private SpriteRenderer sr;
    private float lifetime;
    private float fadeStart;
    private float elapsed = 0f;
    private Color originalColor;

    public void Setup(float lifetime, float fadeStartPercent = 0.5f)
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
        this.lifetime = lifetime;
        this.fadeStart = lifetime * fadeStartPercent;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        if (elapsed >= fadeStart)
        {
            float t = (elapsed - fadeStart) / (lifetime - fadeStart);
            float alpha = Mathf.Lerp(1f, 0f, t);
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        }

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }
}