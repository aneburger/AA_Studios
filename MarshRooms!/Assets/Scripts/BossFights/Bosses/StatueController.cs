using System.Collections;
using UnityEngine;

public class StatueController : MonoBehaviour
{
    private const string TrigSpawn = "Spawn";
    private const string TrigCharge = "Charge";
    private const string TrigFiring = "Firing";
    private const string TrigPowerDown = "PowerDown";

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform eyePoint;
    [SerializeField] private Transform[] eyePoints;
    [SerializeField] private LaserBeamController[] beams;

    [Header("Laser")]
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private float raycastMaxDistance = 30f;
    [SerializeField] private float damage = 2f;
    [SerializeField] private float tickInterval = 0.25f;
    [SerializeField] private float beamHideDelay = 0.4f;

    [Header("Audio")]
    [SerializeField] private AudioClip chargeClip;
    [Range(0f, 1f)] [SerializeField] private float chargeVolume = 1f;
    [SerializeField] private AudioClip firingClip;
    [Range(0f, 1f)] [SerializeField] private float firingVolume = 1f;
    [SerializeField] private AudioClip powerDownClip;
    [Range(0f, 1f)] [SerializeField] private float powerDownVolume = 1f;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    public void PlaySpawn()
    {
        gameObject.SetActive(true);
        animator?.SetTrigger(TrigSpawn);
    }

    public Coroutine Fire(float angleDegrees, float chargeDuration, float firingDuration)
    {
        return StartCoroutine(FireRoutine(angleDegrees, chargeDuration, firingDuration));
    }

    private IEnumerator FireRoutine(float angleDegrees, float chargeDuration, float firingDuration)
    {
        Vector2 dir = AngleToDir(angleDegrees);

        if (eyePoint != null)
            eyePoint.rotation = Quaternion.Euler(0f, 0f, angleDegrees);

        Vector2 damageOrigin = eyePoint != null ? (Vector2)eyePoint.position : (Vector2)transform.position;

        RaycastHit2D initialHit = Physics2D.Raycast(damageOrigin, dir, raycastMaxDistance, hitMask);
        float beamLength = initialHit.collider != null ? initialHit.distance : raycastMaxDistance;

        ShowBeams(damageOrigin, angleDegrees, beamLength);

        animator?.SetTrigger(TrigCharge);
        PlayBeams(b => b.PlayCharge());
        AudioManager.Instance?.PlaySFXWithPitch(chargeClip, chargeVolume, 0.1f);
        yield return new WaitForSeconds(chargeDuration);

        animator?.SetTrigger(TrigFiring);
        PlayBeams(b => b.PlayFiring());
        AudioManager.Instance?.PlaySFXWithPitch(firingClip, firingVolume, 0.1f);

        float elapsed = 0f;
        float nextTick = 0f;

        while (elapsed < firingDuration)
        {
            if (elapsed >= nextTick)
            {
                RaycastHit2D hit = Physics2D.Raycast(damageOrigin, dir, raycastMaxDistance, hitMask);
                if (hit.collider != null)
                {
                    PlayerHealth ph = hit.collider.GetComponentInParent<PlayerHealth>();
                    if (ph != null && !ph.IsOnCooldown())
                        ph.TakeDamage(damage);
                }

                nextTick = elapsed + tickInterval;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        animator?.SetTrigger(TrigPowerDown);
        PlayBeams(b => b.PlayPowerDown());
        AudioManager.Instance?.PlaySFXWithPitch(powerDownClip, powerDownVolume, 0.1f);

        yield return new WaitForSeconds(beamHideDelay);
        PlayBeams(b => b.Hide());
    }

    private void ShowBeams(Vector2 fallbackOrigin, float angleDegrees, float length)
    {
        if (beams == null) return;

        for (int i = 0; i < beams.Length; i++)
        {
            if (beams[i] == null) continue;

            Vector2 origin = (eyePoints != null && i < eyePoints.Length && eyePoints[i] != null)
                ? (Vector2)eyePoints[i].position
                : fallbackOrigin;

            if (eyePoints != null && i < eyePoints.Length && eyePoints[i] != null)
                eyePoints[i].rotation = Quaternion.Euler(0f, 0f, angleDegrees);

            beams[i].Show(origin, angleDegrees, length);
        }
    }

    private void PlayBeams(System.Action<LaserBeamController> action)
    {
        if (beams == null) return;

        foreach (LaserBeamController b in beams)
            if (b != null) action(b);
    }

    private static Vector2 AngleToDir(float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}