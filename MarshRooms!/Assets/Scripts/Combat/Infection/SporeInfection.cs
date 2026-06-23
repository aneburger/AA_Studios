using System.Collections;
using UnityEngine;
using TopDown.Movement;

public class SporeInfection : MonoBehaviour
{
    private static SporeInfectionConfig config;

    private EnemyMover mover;
    private EnemyShooter shooter;
    private SpriteRenderer[] renderers;
    private Color[] originalColors;
    private Coroutine infectionCoroutine;

    private void Awake()
    {
        if (config == null)
            config = Resources.Load<SporeInfectionConfig>("SporeInfectionConfig");
    }

    public void Apply(EnemyMover enemyMover, EnemyShooter enemyShooter)
    {
        mover = enemyMover;
        shooter = enemyShooter;

        if (renderers == null)
        {
            renderers = System.Array.FindAll(
                GetComponentsInChildren<SpriteRenderer>(),
                sr => sr.GetComponentInParent<EnemyHealthBar>() == null
            );
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                originalColors[i] = renderers[i].color;
        }

        if (infectionCoroutine != null)
        {
            StopCoroutine(infectionCoroutine);
            if (mover != null) mover.SetSpeed(mover.OriginalSpeed);
            if (shooter != null) shooter.SetBulletSpeedMultiplier(1f);
        }

        infectionCoroutine = StartCoroutine(InfectionRoutine());
    }

    private IEnumerator InfectionRoutine()
    {
        // Apply effects
        if (mover != null) mover.SetSpeed(mover.OriginalSpeed * config.speedMultiplier);
        if (shooter != null) shooter.SetBulletSpeedMultiplier(config.bulletSpeedMultiplier);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].color = config.infectedTint;

        float elapsed = 0f;
        float tickTimer = 0f;
        BaseHealth health = GetComponent<BaseHealth>();

        while (elapsed < config.duration)
        {
            elapsed += Time.deltaTime;
            tickTimer += Time.deltaTime;

            if (tickTimer >= config.tickInterval)
            {
                if (health != null && !health.IsDead())
                    health.TakeDamage(config.tickDamage);
                tickTimer = 0f;
            }

            yield return null;
        }

        // Remove effects
        if (mover != null) mover.SetSpeed(mover.OriginalSpeed);
        if (shooter != null) shooter.SetBulletSpeedMultiplier(1f);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].color = originalColors[i];

        Destroy(this);
    }
}