// Handles enemy health bar updates and health bar colour changes

using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private SpriteRenderer fill;
    [SerializeField] private Color fullColor = Color.green;
    [SerializeField] private Color halfColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color lowColor = Color.red;

    private Vector3 originalScale;

    // -- AWAKE --
    private void Awake()
    {
        originalScale = fill.transform.localScale;
        gameObject.SetActive(false);
    }

    // -- UPDATE HEALTH BAR --
    public void UpdateHealth(float current, float max)
    {
        gameObject.SetActive(true);

        float t = current / max;

        fill.transform.localScale = new Vector3(originalScale.x * t, originalScale.y, 1f);
    
        if (t > 0.5f)
            fill.color = Color.Lerp(halfColor, fullColor, (t - 0.5f) * 2f);
        else
            fill.color = Color.Lerp(lowColor, halfColor, t * 2f);
    }
}