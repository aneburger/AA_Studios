// Destroy VFX GameObject when the animation finishes

using UnityEngine;

public class VFXAnimator : MonoBehaviour
{
    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        if (state.normalizedTime >= 1f)
        {
            Destroy(gameObject);
        }
    }
}