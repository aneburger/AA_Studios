using System.Collections;
using UnityEngine;

public class SporeTick : MonoBehaviour
{
    private Coroutine tickCoroutine;
    private SpriteRenderer[] renderers;
    private Color[] originalColors;

    public void Apply(float tickDamage, float tickInterval, float tickDuration, Color poisonTint)
    {
        if (renderers == null)
        {
            renderers = GetComponentsInChildren<SpriteRenderer>();

            renderers = System.Array.FindAll(renderers, 
                sr => sr.GetComponentInParent<EnemyHealthBar>() == null);

            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                originalColors[i] = renderers[i].color;
        }

        if (tickCoroutine != null)
            StopCoroutine(tickCoroutine);

        tickCoroutine = StartCoroutine(TickRoutine(tickDamage, tickInterval, tickDuration, poisonTint));
    }

    private IEnumerator TickRoutine(float tickDamage, float tickInterval, float tickDuration, Color poisonTint)
    {
        foreach (var sr in renderers)
            sr.color = poisonTint;

        BaseHealth health = GetComponent<BaseHealth>();
        float elapsed = 0f;

        while (elapsed < tickDuration)
        {
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;

            if (health == null || health.IsDead()) break;
            health.TakeDamage(tickDamage);
        }

        for (int i = 0; i < renderers.Length; i++)
            renderers[i].color = originalColors[i];

        Destroy(this);
    }
}