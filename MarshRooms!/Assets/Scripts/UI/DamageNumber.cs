// Floating damage number that rises and fades

using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TextMeshPro text;
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float riseSpeed = 1f;
    [SerializeField] private float fadeSpeed = 2f;

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