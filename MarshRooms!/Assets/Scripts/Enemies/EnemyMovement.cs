// Controls enemy movments, inherits velocity and knockback from Mover

using UnityEngine;
namespace TopDown.Movement

{
    public class EnemyMover : BaseMover
    {
        public void Move(Vector2 direction)
        {
            moveInput = direction;
        }

        public void Stop()
        {
            moveInput = Vector2.zero;
        }
    }
}