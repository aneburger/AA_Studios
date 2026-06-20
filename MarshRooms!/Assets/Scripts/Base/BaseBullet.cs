// Base class for all types of bullets

using UnityEngine;
using TopDown.Movement;

public class BaseBullet : MonoBehaviour
{
    // Bullet details
    protected float speed;
    protected float damage;
    protected float knockback;
    protected GameObject hitVFX;
    protected Vector2 direction;
    protected int weaponSortingOrder;

    protected virtual void Start() { }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void SetBullet(float speed, float damage, float knockback, GameObject hitVFX, int sortingOrder)
    {
       this.speed = speed;
       this.damage = damage;
       this.knockback = knockback;
       this.hitVFX = hitVFX;
       this.weaponSortingOrder = sortingOrder;
    }

    protected virtual void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
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

    protected virtual void OnHit() {}
}
