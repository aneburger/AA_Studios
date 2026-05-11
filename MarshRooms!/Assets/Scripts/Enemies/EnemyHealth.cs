// Enemy health, extends BaseHealth
// Handles enemy death, despawn and drops

using UnityEngine;

public class EnemyHealth : BaseHealth
{

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Die()
    {
        base.Die();

        // Destroy game object for now
        Destroy(gameObject);
    }
}