// Contact damage for the boss.

using UnityEngine;
using TopDown.Movement;

public class BossContactDamage : MonoBehaviour
{
    private float damage;
    private float knockback;

    public void SetContact(float newDamage, float newKnockback)
    {
        damage = newDamage;
        knockback = newKnockback;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (damage <= 0f) return;

        PlayerHealth playerHealth = collision.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;

        if (playerHealth.IsOnCooldown()) return;

        playerHealth.TakeDamage(damage);

        BaseMover playerMover = collision.GetComponentInParent<BaseMover>();
        if (playerMover != null)
        {
            Vector2 knockbackDir = ((Vector2)collision.transform.position - (Vector2)transform.position).normalized;
            playerMover.ApplyKnockback(knockbackDir * knockback);
        }
    }
}