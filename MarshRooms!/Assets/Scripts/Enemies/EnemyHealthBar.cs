// Handles enemy health bar updates and health bar colour changes
using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private SpriteRenderer fill;
    [SerializeField] private Color fullColor;
    [SerializeField] private Color halfColor;
    [SerializeField] private Color lowColor;

    [Header("Elite")]
    [SerializeField] private bool isElite = false;
    [SerializeField] private Color eliteGlowColor = Color.white;
    [SerializeField] private float eliteTintStrength = 0.6f;
    [SerializeField] private SpriteRenderer background;

    private static readonly int OutlineEnabledID = Shader.PropertyToID("_OutlineEnabled");

    private MaterialPropertyBlock mpb;
    private Color backgroundOriginalColor;
    private Vector3 originalScale;

    // -- AWAKE --
    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        originalScale = fill.transform.localScale;
        if (background != null) backgroundOriginalColor = background.color;
        gameObject.SetActive(false);
    }

    // -- SET ELITE --
    public void SetElite(bool value, Color glowColor)
    {
        isElite = value;
        eliteGlowColor = glowColor;
    }

    // -- UPDATE HEALTH BAR --
    public void UpdateHealth(float current, float max)
    {
        gameObject.SetActive(true);

        if (isElite)
        {
            float half = max * 0.5f;
            bool inStageOne = current > half;

            SetOutlineVisible(inStageOne);

            if (inStageOne)
            {
                // Stage 1 - first half of health
                float stageT = (current - half) / half;
                fill.transform.localScale = new Vector3(originalScale.x * stageT, originalScale.y, 1f);
                Color baseColor = GetNormalColor(stageT);
                Color tinted = Color.Lerp(baseColor, eliteGlowColor, eliteTintStrength);
                fill.color = tinted;
                if (background != null) background.color = Color.Lerp(backgroundOriginalColor, eliteGlowColor, eliteTintStrength);
            }
            else
            {
                // Stage 2 - remaining half
                float stageT = current / half;
                fill.transform.localScale = new Vector3(originalScale.x * stageT, originalScale.y, 1f);
                fill.color = GetNormalColor(stageT);
                if (background != null) background.color = backgroundOriginalColor;
            }
        }
        else
        {
            float t = current / max;
            fill.transform.localScale = new Vector3(originalScale.x * t, originalScale.y, 1f);
            fill.color = GetNormalColor(t);
        }

        // Hide when empty
        if (current <= 0)
            gameObject.SetActive(false);
    }

    // -- SET OUTLINE VISIBLE --
    private void SetOutlineVisible(bool visible)
    {
        if (background == null) return;

        background.GetPropertyBlock(mpb);
        mpb.SetFloat(OutlineEnabledID, visible ? 1f : 0f);
        background.SetPropertyBlock(mpb);
    }

    // -- GET NORMAL COLOR --
    private Color GetNormalColor(float t)
    {
        if (t > 0.5f)
            return Color.Lerp(halfColor, fullColor, (t - 0.5f) * 2f);
        else
            return Color.Lerp(lowColor, halfColor, t * 2f);
    }
}