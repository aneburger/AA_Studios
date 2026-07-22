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
    protected AudioClip wallHitClip;
    protected float wallHitVolume;

    protected virtual void Start() { }

    // -- SET DIRECTION --
    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // -- AIM ORIGIN -- 
    public void SetAimOrigin(Vector2 origin)
    {
        aimOrigin = origin;
    }

    // --  SET BULLET --
    public void SetBullet(float speed, float damage, float knockback, GameObject hitVFX, int sortingOrder, AudioClip wallHitClip = null, float wallHitVolume = 0.7f)
    {
        this.speed = speed;
        this.damage = damage;
        this.knockback = knockback;
        this.hitVFX = hitVFX;
        weaponSortingOrder = sortingOrder;
        this.wallHitClip = wallHitClip;
        this.wallHitVolume = wallHitVolume;
    }

    // -- UPDATE -- 
    protected virtual void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    // -- BULLET HIT --
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        BaseHealth health = collision.GetComponentInParent<BaseHealth>();

        if (health != null)
        {
            health.TakeDamage(damage);

            BaseMover mover = collision.GetComponentInParent<BaseMover>();
            if (mover != null)
                mover.ApplyKnockback(direction * knockback);
        }
        else
        {
            AudioManager.Instance?.PlaySFXWithPitch(wallHitClip, wallHitVolume, 0.1f);
        }

        VFXManager.Instance.SpawnHitVFX(hitVFX, transform.position, weaponSortingOrder);
        Destroy(gameObject);
    }

    protected virtual void OnHit() {}
}
