// Sets the facing direction of a sprite, can be reused for Player, enemies ect.

using UnityEngine;

public class DirectionalAnimator : MonoBehaviour
{
    [SerializeField] private Animator anim;

    // -- AWAKE --
    private void Awake()
    {
        if (anim == null)
            anim = GetComponentInChildren<Animator>();
    }

    // -- SET DIRECTION --
    public void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;
        
        Vector2 finalDirection = GetClosestDirection(direction);
        anim.SetFloat("x", finalDirection.x);
        anim.SetFloat("y", finalDirection.y);
    }

    // -- SET WALKING --
    public void SetWalking(bool walking)
    {
        anim.SetBool("isWalking", walking);
    }

    // -- SET ANIMATION SPEED --
    public void SetAnimationSpeed(float speed)
    {
        anim.speed = speed;
    }

    // -- SET CLOSEST DIRECTION --
    private Vector2 GetClosestDirection(Vector2 input)
    {
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            return input.x >= 0f ? Vector2.right : Vector2.left;
        }

        return input.y >= 0f ? Vector2.up : Vector2.down;
    }
}