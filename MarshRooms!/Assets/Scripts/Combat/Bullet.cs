using UnityEngine;
using TopDown.Movement;

public class Bullet : MonoBehaviour
{
    // Bullet details
    private float speed;
    private float damage;
    private float knockback;
    private GameObject hitVFX;
    private Vector2 direction;
    private int weaponSortingOrder;

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    public void setBullet(float speed, float damage, float knockback, GameObject hitVFX)
    {
       this.speed = speed;
       this.damage = damage;
       this.knockback = knockback;
       this.hitVFX = hitVFX;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        BaseHealth health = collision.GetComponentInParent<BaseHealth>();

        if (health != null)
        {
            health.TakeDamage(damage);

            // Apply knockback
            BaseMover mover = collision.GetComponentInParent<BaseMover>();
            if (mover != null)
                mover.ApplyKnockback(direction * knockback);
        }

        VFXManager.Instance.SpawnHitVFX(hitVFX, transform.position, weaponSortingOrder);
        Destroy(gameObject);
    }
}
