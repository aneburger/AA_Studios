// Base class for all movement logic
// Inherited by PlayerMover and EnemyMover.

using UnityEngine;

namespace TopDown.Movement
{
    [RequireComponent(typeof(Rigidbody2D))]
    
    public class BaseMover : MonoBehaviour
    {
        [SerializeField] protected float moveSpeed;
        protected Rigidbody2D body { get; private set; }
        protected Vector2 moveInput { get; set; }

        private Vector2 knockbackVelocity;

        // -- AWAKE --
        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        // -- KNOCKBACK -- (Called by Shooter)
        public void ApplyKnockback(Vector2 force)
        {
            knockbackVelocity = force;
        }

        // -- SET SPEED --
        public void SetSpeed(float speed)
        {
            moveSpeed = speed;
        }

        // -- FIXEDUPDATE --
        protected virtual void FixedUpdate()
        {
            knockbackVelocity = Vector2.Lerp(knockbackVelocity, Vector2.zero, 0.3f);
            body.linearVelocity = (moveInput * moveSpeed) + knockbackVelocity;
        }
    }
}
