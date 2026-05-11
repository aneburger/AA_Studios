// Base class for all helathlogic
// Handles taking damage, death, and hit animation
// Inherited by PlayerHealth and EnemyHealth

using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public abstract class BaseHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] protected float maxHealth;


    private Color flashColor = Color.red;
    private float flashDuration = 0.1f;

    protected float currentHealth;

    // -- Events --
    public UnityEvent onDeath;
    public UnityEvent onTakeDamage;

    protected Animator anim;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;

    public void Initialise(float max)
    {
        maxHealth = max;
        currentHealth = max;
    }

    // -- AWAKE --
    protected virtual void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        
        currentHealth = maxHealth;

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;
    }

    // -- IS DEAD
    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    // -- TAKE DAMAGE --
    public virtual void TakeDamage(float amount)
    {
        if (IsDead()) return;

        currentHealth -= amount;
        onTakeDamage?.Invoke();

        anim?.SetTrigger("TakeDamage");
        
        OnHitEffect();

        if (IsDead())
            Die();
    }

    // -- DIE --
    protected virtual void Die()
    {
        onDeath?.Invoke();
    }

    // -- HIT EFFECT --
    protected virtual void OnHitEffect()
    {
        StartCoroutine(FlashRed());
    }

    // -- FLASH RED --
    private IEnumerator FlashRed()
    {
        foreach (var sr in spriteRenderers)
            sr.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteRenderers[i].color = originalColors[i];
    }
}