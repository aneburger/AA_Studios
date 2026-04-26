// Base class for all helathlogic
// Handles taking damage, death, and hit animation
// Inherited by PlayerHealth and EnemyHealth

using UnityEngine;
using UnityEngine.Events;

public abstract class BaseHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] protected float maxHealth;

    protected float currentHealth;

    // -- Events --
    public UnityEvent onDeath;
    public UnityEvent onTakeDamage;

    protected Animator anim;

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

        if (IsDead())
            Die();
    }

    // -- DIE --
    protected virtual void Die()
    {
        onDeath?.Invoke();
    }
}