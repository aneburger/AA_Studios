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
    protected Vector2 aimOrigin;

    protected bool isInfected = false;

    protected virtual void Start() { }

    public void SetInfected(bool infected)
    {
        isInfected = infected;
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void SetAimOrigin(Vector2 origin)
    {
        aimOrigin = origin;
    }

    public void SetBullet(float speed, float damage, float knockback, GameObject hitVFX, int sortingOrder)
    {
       this.speed = speed;
       this.damage = damage;
       this.knockback = knockback;
       this.hitVFX = hitVFX;
       weaponSortingOrder = sortingOrder;
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

            BaseMover mover = collision.GetComponentInParent<BaseMover>();
            if (mover != null)
                mover.ApplyKnockback(direction * knockback);

            // Apply infection if infected bullet hits an enemy
            if (isInfected)
            {
                EnemyMover enemyMover = collision.GetComponentInParent<EnemyMover>();
                EnemyShooter enemyShooter = collision.GetComponentInParent<EnemyShooter>();

                if (enemyMover != null || enemyShooter != null)
                {
                    SporeInfection infection = collision.GetComponentInParent<SporeInfection>();
                    if (infection == null)
                        infection = collision.GetComponentInParent<Transform>().gameObject.AddComponent<SporeInfection>();

                    infection.Apply(enemyMover, enemyShooter);
                }
            }
        }

        VFXManager.Instance.SpawnHitVFX(hitVFX, transform.position, weaponSortingOrder);
        Destroy(gameObject);
    }

    protected void TryApplyInfection(Collider2D collision)
    {
        if (!isInfected) return;

        EnemyMover enemyMover = collision.GetComponentInParent<EnemyMover>();
        EnemyShooter enemyShooter = collision.GetComponentInParent<EnemyShooter>();

        if (enemyMover == null && enemyShooter == null) return;

        SporeInfection infection = collision.GetComponentInParent<SporeInfection>();
        if (infection == null)
            infection = collision.GetComponentInParent<Transform>().gameObject.AddComponent<SporeInfection>();

        infection.Apply(enemyMover, enemyShooter);
    }

    protected virtual void OnHit() {}
}
