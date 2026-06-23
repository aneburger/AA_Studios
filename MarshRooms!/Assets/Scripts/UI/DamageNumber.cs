// Floating damage number that rises and fades
// Scales based on damage amount so players notice which weapons hit harder

using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TextMeshPro text;
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float riseSpeed = 1f;

    [Header("Scaling")]
    [SerializeField] private float minDamageForScale = 5f;
    [SerializeField] private float maxDamageForScale = 50f;
    [SerializeField] private float minScale = 0.7f;
    [SerializeField] private float maxScale = 1.6f;

    private float elapsed = 0f;
    private Color originalColor;

    // -- AWAKE --
    private void Awake()
    {
        originalColor = text.color;
    }

    // -- SET DAMAGE --
    public void SetDamage(float amount)
    {
        text.text = ((int)amount).ToString();

        // Scale text size based on how much damage was dealt
        float t = Mathf.InverseLerp(minDamageForScale, maxDamageForScale, amount);
        float scale = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = Vector3.one * scale;
    }

    // -- UPDATE --
    private void Update()
    {
        elapsed += Time.deltaTime;

        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        float alpha = Mathf.Lerp(1f, 0f, elapsed / lifetime);
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }
}