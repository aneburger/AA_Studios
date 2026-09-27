// Contact damage for the boss.

using UnityEngine;
using TopDown.Movement;

public class BossContactDamage : MonoBehaviour
{
    [SerializeField] private string bossName = "Boss";
    public string BossName => bossName;

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

        playerHealth.SetLastAttacker(bossName);
        playerHealth.TakeDamage(damage);

        BaseMover playerMover = collision.GetComponentInParent<BaseMover>();
        if (playerMover != null)
        {
            Vector2 knockbackDir = ((Vector2)collision.transform.position - (Vector2)transform.position).normalized;
            playerMover.ApplyKnockback(knockbackDir * knockback);
        }
    }
}