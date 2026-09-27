using System.Collections;
using UnityEngine;

public class SpikeHazard : MonoBehaviour
{
    [Header("Animator Triggers")]
    [SerializeField] private string chargeTrigger = "Charge";
    [SerializeField] private string eruptTrigger = "Erupt";
    [SerializeField] private string retractTrigger = "Retract";

    [Header("Timing")]
    [SerializeField] private float hitDelay = 0.05f;
    [SerializeField] private float upTime = 0.7f;
    [SerializeField] private float retractTimeout = 1.5f;

    [Header("Damage")]
    [SerializeField] private float damage = 4f;
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private Vector2 hitOffset;
    [SerializeField] private string attackerName = "Hazard";

    [Header("Audio / FX")]
    [SerializeField] private AudioClip targetClip;
    [Range(0f, 1f)] [SerializeField] private float targetVolume = 1f;
    [SerializeField] private AudioClip eruptClip;
    [Range(0f, 1f)] [SerializeField] private float eruptVolume = 1f;
    [SerializeField] private AudioClip retractClip;
    [Range(0f, 1f)] [SerializeField] private float retractVolume = 0.8f;
    [SerializeField] private float eruptShake = 0.2f;

    private Animator anim;
    private int playerMask;

    // Many spikes go off together, so sounds and shakes are rate limited
    private static float lastTargetSound = -999f;
    private static float lastEruptSound = -999f;
    private static float lastRetractSound = -999f;
    private static float lastShake = -999f;

    // -- AWAKE --
    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        playerMask = LayerMask.GetMask("Player", "PlayerInvincible");
    }

    // -- SET ATTACKER NAME --
    public void SetAttackerName(string name)
    {
        attackerName = name;
    }

    // -- BEGIN --
    // warningTime: how long the target shows before the spike shoots up (this is the player's dodge window)
    public void Begin(float warningTime)
    {
        StartCoroutine(Lifecycle(warningTime));
    }

    private IEnumerator Lifecycle(float warningTime)
    {
        // 1. Target appears
        SetTrigger(chargeTrigger);
        if (Ready(ref lastTargetSound, 0.06f))
            AudioManager.Instance?.PlaySFXWithPitch(targetClip, targetVolume, 0.1f);

        yield return new WaitForSeconds(warningTime);

        // 2. Shoots up
        SetTrigger(eruptTrigger);
        if (Ready(ref lastEruptSound, 0.06f))
            AudioManager.Instance?.PlaySFXWithPitch(eruptClip, eruptVolume, 0.1f);
        if (Ready(ref lastShake, 0.1f))
            ScreenEffects.Instance?.ShakeScreen(eruptShake);

        yield return new WaitForSeconds(hitDelay);

        // 3. Dangerous while it's up
        float t = 0f;
        while (t < upTime)
        {
            CheckHit();
            t += Time.deltaTime;
            yield return null;
        }

        // 4. Retracts back into the ground
        SetTrigger(retractTrigger);
        if (Ready(ref lastRetractSound, 0.06f))
            AudioManager.Instance?.PlaySFXWithPitch(retractClip, retractVolume, 0.1f);

        yield return WaitForRetractEnd();
        Destroy(gameObject);
    }

    // PlayerHealth ignores the hit while Marsh is dodging or still on his post-hit cooldown
    private void CheckHit()
    {
        Vector2 center = (Vector2)transform.position + hitOffset;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, hitRadius, playerMask);

        foreach (Collider2D hit in hits)
        {
            PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null || playerHealth.IsOnCooldown()) continue;

            playerHealth.SetLastAttacker(attackerName);
            playerHealth.TakeDamage(damage);
            return;
        }
    }

    private IEnumerator WaitForRetractEnd()
    {
        if (anim == null)
        {
            yield return new WaitForSeconds(retractTimeout);
            yield break;
        }

        yield return null;   // let the trigger take effect

        float t = 0f;
        while (t < retractTimeout)
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            if (!anim.IsInTransition(0) && info.normalizedTime >= 1f) yield break;

            t += Time.deltaTime;
            yield return null;
        }
    }

    private void SetTrigger(string triggerName)
    {
        if (anim != null) anim.SetTrigger(triggerName);
    }

    private static bool Ready(ref float last, float cooldown)
    {
        if (Time.time < last) last = -999f;   // time restarted (new play session)
        if (Time.time - last < cooldown) return false;

        last = Time.time;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((Vector2)transform.position + hitOffset, hitRadius);
    }
}