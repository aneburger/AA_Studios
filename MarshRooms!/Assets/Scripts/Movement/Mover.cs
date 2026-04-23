using UnityEngine;
// Base class for all movement logic

namespace TopDown.Movement
{
    [RequireComponent(typeof(Rigidbody2D))]
    
    public class Mover : MonoBehaviour //Parent class for PlayerMovement and EnemyMovement
    {
        [SerializeField] protected float moveSpeed;
        protected Rigidbody2D body { get; private set; }
        protected Vector2 moveInput { get; set; }

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        protected virtual void FixedUpdate()
        {
            body.linearVelocity = moveInput * moveSpeed;
        }
    }
}
