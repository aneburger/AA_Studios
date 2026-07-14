// Base class for all movement logic
// Inherited by PlayerMover and EnemyMover.

using UnityEngine;

namespace TopDown.Movement
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BaseMover : MonoBehaviour
    {
        [SerializeField] protected float moveSpeed;
        public float OriginalSpeed { get; private set; }
        
        protected Rigidbody2D body { get; private set; }
        protected Vector2 moveInput { get; set; }
        protected DirectionalAnimator directionalAnimator { get; private set; }
        public DirectionalAnimator DirectionalAnimator => directionalAnimator;

        protected Vector2 lastMoveDirection = Vector2.down;
        private Vector2? facingOverride = null;

        private Vector2 knockbackVelocity;

        private float speedMultiplier = 1f;

        // -- AWAKE --
        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            OriginalSpeed = moveSpeed;
            directionalAnimator = GetComponentInChildren<DirectionalAnimator>();
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
        
        // -- SET SPEED MULTIPLIER --
        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = multiplier;
        }

        // -- SET ANIMATION SPEED --
        protected void SetAnimationSpeed(float speed)
        {
            directionalAnimator?.SetAnimationSpeed(speed);
        }

        // -- SET FACING OVERRIDE --
        public void SetFacingOverride(Vector2 direction)
        {
            facingOverride = direction.sqrMagnitude > 0.001f ? direction : (Vector2?)null;
        }

        // -- SET MOVE INPUTv --
        public void SetMoveInput(Vector2 input)
        {
            moveInput = input;
        }

        // -- STOP MOVEMENT IMMEDIATELY --
        public void StopMovement()
        {
            moveInput = Vector2.zero;
            body.linearVelocity = Vector2.zero;
        }
        
        // -- CLEAR FACING --
        public void ClearFacingOverride()
        {
            facingOverride = null;
        }

        // -- SET FACING --
        protected void UpdateFacing(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f) return;
            directionalAnimator?.SetDirection(direction);
        }

        // -- FACE DIRECTION --
        public void FaceDirection(Vector2 direction)
        {
            UpdateFacing(direction);
        }

        // -- FIXEDUPDATE --
        protected virtual void FixedUpdate()
        {
            knockbackVelocity = Vector2.Lerp(knockbackVelocity, Vector2.zero, 0.3f);
            body.linearVelocity = (moveInput * moveSpeed * speedMultiplier) + knockbackVelocity;

            if (moveInput.sqrMagnitude > 0.01f)
                lastMoveDirection = moveInput;

            UpdateFacing(facingOverride ?? lastMoveDirection);
        }
    }
}
