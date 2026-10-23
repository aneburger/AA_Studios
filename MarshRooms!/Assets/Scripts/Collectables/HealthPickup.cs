// Health pickup collectible

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class HealthPickup : MonoBehaviour
{
    [SerializeField] private int healAmount = 4;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 8f;
    [SerializeField] private float flickerStart = 5f;
    [SerializeField] private bool isPersistent = false;

    private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    private Light2D[] lights;

    // -- AWAKE --
    private void Awake()
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            if (sr.enabled) renderers.Add(sr);
        }

        lights = GetComponentsInChildren<Light2D>();

        StartCoroutine(LifetimeTimer());
    }

    public void SetPersistent(bool persistent) => isPersistent = persistent;

    // -- ON TRIGGER ENTER --
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.Heal(healAmount);
        Destroy(gameObject);
    }

    // -- LIFETIME TIMER --
    private IEnumerator LifetimeTimer()
    {
        // Wait until flicker starts
        yield return new WaitForSeconds(flickerStart);

        if (isPersistent) yield break;

        // Flicker, faster and faster as it gets closer to despawning
        float elapsed = 0f;
        float flickerInterval = 0.15f;
        float flickerDuration = Mathf.Max(0.01f, lifetime - flickerStart);
        bool visible = true;

        while (elapsed < flickerDuration)
        {
            if (isPersistent)
            {
                SetVisible(true);
                yield break;
            }

            visible = !visible;
            SetVisible(visible);

            yield return new WaitForSeconds(flickerInterval);
            elapsed += flickerInterval;

            flickerInterval = Mathf.Lerp(0.15f, 0.04f, elapsed / flickerDuration);
        }

        Destroy(gameObject);
    }

    // -- SET VISIBLE --
    private void SetVisible(bool visible)
    {
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr != null) sr.enabled = visible;
        }

        if (lights == null) return;
        foreach (Light2D light in lights)
        {
            if (light != null) light.enabled = visible;
        }
    }
}