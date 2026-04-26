// Sets the facing direction of a sprite, can be reused for Player, enemies ect.

using UnityEngine;

public class DirectionalAnimator : MonoBehaviour
{
    [SerializeField] private Animator anim;

    // -- AWAKE --
    private void Awake()
    {
        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }
    }

    // Set direction from other scripts
    public void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Vector2 finalDirection = GetClosestDirection(direction);

        anim.SetFloat("x", finalDirection.x);
        anim.SetFloat("y", finalDirection.y);
    }

    // Converts direction vector to one of 4 directions
    private Vector2 GetClosestDirection(Vector2 input)
    {
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            return input.x >= 0f ? Vector2.right : Vector2.left;
        }

        return input.y >= 0f ? Vector2.up : Vector2.down;
    }
}