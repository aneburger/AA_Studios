using UnityEngine;
using TopDown.Movement;

public class BounceBullet : BaseBullet
{
    [Header("Bounce Settings")]
    [SerializeField] private int maxBounces = 2;
    [SerializeField] private LayerMask wallMask;

    [Header("Bounce Audio")]
    [SerializeField] private AudioClip bounceClip;
    [Range(0f, 1f)] [SerializeField] private float bounceVolume = 0.8f;

    private int bouncesLeft;

    protected override void Start()
    {
        base.Start();
        bouncesLeft = maxBounces;

        if (wallMask == 0)
            wallMask = LayerMask.GetMask("Walls");
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        BaseHealth health = collision.GetComponentInParent<BaseHealth>();

        if (health != null)
        {
            health.TakeDamage(damage);

            BaseMover mover = collision.GetComponentInParent<BaseMover>();
            if (mover != null)
                mover.ApplyKnockback(direction * knockback);

            VFXManager.Instance.SpawnHitVFX(hitVFX, transform.position, weaponSortingOrder);
            Destroy(gameObject);
            return;
        }

        bool isWall = ((1 << collision.gameObject.layer) & wallMask) != 0;
        if (!isWall || bouncesLeft <= 0)
        {
            AudioManager.Instance?.PlaySFXWithPitch(wallHitClip, wallHitVolume, 0.1f);
            VFXManager.Instance.SpawnHitVFX(hitVFX, transform.position, weaponSortingOrder);
            Destroy(gameObject);
            return;
        }

        Bounce(collision);
    }

    private void Bounce(Collider2D wallCollider)
    {
        bouncesLeft--;

        Vector2 normal = GetWallNormal(wallCollider);
        Vector2 newDirection = Vector2.Reflect(direction, normal).normalized;

        SetDirection(newDirection);

        transform.position += (Vector3)(newDirection * 0.1f);

        AudioManager.Instance?.PlaySFXWithPitch(bounceClip, bounceVolume, 0.1f);
    }

    private Vector2 GetWallNormal(Collider2D wallCollider)
    {
        Vector2 origin = (Vector2)transform.position - direction * 0.5f;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, 1f, wallMask);

        if (hit.collider == wallCollider)
            return hit.normal;

        return -direction;
    }
}