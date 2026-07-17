 using UnityEngine;

public class MrBlobsAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D mrBlobsCollider;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Hide()
    {
        animator?.SetTrigger("Hide");
        if (mrBlobsCollider != null) mrBlobsCollider.enabled = false;
    }

    public void Unhide()
    {
        animator?.SetTrigger("Unhide");
        if (mrBlobsCollider != null) mrBlobsCollider.enabled = true;
    }

    public void FlipFacing()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = !spriteRenderer.flipX;
    }
}