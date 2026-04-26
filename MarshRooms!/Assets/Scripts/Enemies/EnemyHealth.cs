// Enemy health, extends BaseHealth
// Handles enemy death, despawn and drops

using UnityEngine;

public class EnemyHealth : BaseHealth
{
    [Header("Death Settings")]
    [SerializeField] private float deathDelay = 0f; // update later for death animation

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Die()
    {
        base.Die();

        // Ddestroy game object for now
        Destroy(gameObject, deathDelay);
    }
}