// The visible laser beam.

using UnityEngine;

public class LaserBeamController : MonoBehaviour
{
    private const string TrigCharge = "Charge";
    private const string TrigFiring = "Firing";
    private const string TrigPowerDown = "PowerDown";

    [SerializeField] private Animator animator;

    [SerializeField] private float spriteUnitLength = 1f;

    // -- AWAKE --
    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        gameObject.SetActive(false);
    }

    public void Show(Vector2 origin, float angleDegrees, float length)
    {
        gameObject.SetActive(true);
        transform.position = origin;
        transform.rotation = Quaternion.Euler(0f, 0f, angleDegrees);

        Vector3 scale = transform.localScale;
        scale.x = length / Mathf.Max(0.0001f, spriteUnitLength);
        transform.localScale = scale;
    }

    public void PlayCharge() => animator?.SetTrigger(TrigCharge);
    public void PlayFiring() => animator?.SetTrigger(TrigFiring);
    public void PlayPowerDown() => animator?.SetTrigger(TrigPowerDown);

    public void Hide() => gameObject.SetActive(false);
}