// Controls enemy movments, inherits velocity and knockback from Mover

using UnityEngine;

namespace TopDown.Movement
{
    public class EnemyMover : BaseMover
    {
         // -- AWAKE --
        protected override void Awake()
        {
            base.Awake();
        }

        // -- MOVE --
        public void Move(Vector2 direction)
        {
            moveInput = direction;
            directionalAnimator?.SetWalking(true);
        }

        // -- STOP --
        public void Stop()
        {
            moveInput = Vector2.zero;
            directionalAnimator?.SetWalking(false);
        }
    }
}