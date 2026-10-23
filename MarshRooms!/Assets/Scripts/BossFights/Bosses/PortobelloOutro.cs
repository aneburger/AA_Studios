using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PortobelloOutro : BossOutroSequence
{
    [Header("Boss References")]
    [SerializeField] private Animator bossAnimator;

    [Header("Kill Hit-Stop")]
    [SerializeField] private float hitStopDuration = 0.08f;
    [SerializeField] private Color hitStopFlashColor = Color.white;
    [Range(0f, 1f)] [SerializeField] private float hitStopFlashAlpha = 0.7f;
    [SerializeField] private float hitStopFlashDuration = 0.3f;

    [Header("Death Rumble")]
    [SerializeField] private int rumbleShakeCount = 4;
    [SerializeField] private float rumbleShakeInterval = 0.15f;
    [SerializeField] private float rumbleShakeMin = 0.2f;
    [SerializeField] private float rumbleShakeMax = 0.5f;
    [SerializeField] private float musicFadeDuration = 1.5f;

    [Header("Dialogue")]
    [SerializeField] private DialogueSequence portobelloDeathDialogue;
    [SerializeField] private DialogueSequence marshReactionDialogue;

    [Header("Die Animation")]
    [SerializeField] private string deathTrigger = "Die";
    [SerializeField] private float deathAnimationDuration = 0f;

    [Header("Final Death Animation")]
    [SerializeField] private string finalDeathTrigger = "FinalDeath";
    [SerializeField] private float finalDeathDuration = 1.5f;

    [Header("Vanish")]
    [SerializeField] private GameObject bossRoot;
    [SerializeField] private GameObject[] hideOnVanish;
    [SerializeField] private GameObject vanishVfxPrefab;
    [SerializeField] private Vector3 vanishVfxOffset = Vector3.zero;
    [SerializeField] private AudioClip vanishClip;
    [Range(0f, 1f)] [SerializeField] private float vanishVolume = 1f;
    [SerializeField] private float vanishScreenShake = 0.5f;

    [Header("Pause")]
    [SerializeField] private float pauseBeforeDeathDialogue = 0.6f;
    [SerializeField] private float pauseAfterDeathDialogue = 0.5f;
    [SerializeField] private float pauseAfterDeathAnimation = 0.4f;
    [SerializeField] private float pauseBeforeMarshLine = 0.8f;
    [SerializeField] private float pauseAfterMarshLine = 0.5f;

    [Header("Hand-over")]
    [SerializeField] private UnityEvent onOutroFinished;
    [SerializeField] private bool neverReturnControl = true;

    public override IEnumerator Play(BossRoomController room)
    {
        yield return null;

        // -- Die animation ---
        if (bossAnimator != null && !string.IsNullOrEmpty(deathTrigger))
        {
            if (HasTrigger(bossAnimator, deathTrigger))
                bossAnimator.SetTrigger(deathTrigger);
        }

        // -- Kill hit-stop --
        Time.timeScale = 0f;
        ScreenEffects.Instance?.FlashColor(hitStopFlashColor, hitStopFlashAlpha, hitStopFlashDuration);
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;

        yield return DeathRumble();

        AudioManager.Instance?.FadeMusicVolume(0.2f, musicFadeDuration);
        room.HealthBar?.Hide();

        // -- Make Marsh safe for the whole ending --
        room.LockPlayerExternally();

        while (room.Mover != null && room.Mover.IsDodging)
            yield return null;

        PlayerHealth playerHealth = room.Player != null ? room.Player.GetComponent<PlayerHealth>() : null;
        playerHealth?.SetInvincible(true, affectSpeed: false);

        // -- Final cutscene --
        HUDManager.Instance?.SetHUDVisible(false);
        playerHealth?.ResetHealth();
        SporeManager.Instance?.ResetSpores();

        if (deathAnimationDuration > 0f)
            yield return new WaitForSeconds(deathAnimationDuration);

        yield return Pause(pauseBeforeDeathDialogue);

        yield return room.PlayDialogue(portobelloDeathDialogue);

        yield return Pause(pauseAfterDeathDialogue);

        if (bossAnimator != null && !string.IsNullOrEmpty(finalDeathTrigger))
        {
            if (HasTrigger(bossAnimator, finalDeathTrigger))
                bossAnimator.SetTrigger(finalDeathTrigger);
        }

        AudioManager.Instance?.FadeOutMusic(musicFadeDuration);

        yield return Pause(finalDeathDuration);
        yield return Pause(pauseAfterDeathAnimation);

        // -- Vanish
        Vanish();

        yield return Pause(pauseBeforeMarshLine);
        yield return room.PlayDialogue(marshReactionDialogue);
        yield return Pause(pauseAfterMarshLine);

        onOutroFinished?.Invoke();

        // The ending takes over from here
        while (neverReturnControl)
            yield return null;
    }

    private static IEnumerator Pause(float seconds)
    {
        if (seconds > 0f)
            yield return new WaitForSeconds(seconds);
    }

    private IEnumerator DeathRumble()
    {
        for (int i = 0; i < rumbleShakeCount; i++)
        {
            float strength = Mathf.Lerp(rumbleShakeMax, rumbleShakeMin, (float)i / Mathf.Max(1, rumbleShakeCount - 1));
            ScreenEffects.Instance?.ShakeScreen(strength);
            yield return new WaitForSeconds(rumbleShakeInterval);
        }
    }

    private void Vanish()
    {
        Vector3 vfxPos = bossRoot != null ? bossRoot.transform.position : transform.position;

        if (bossRoot != null)
        {
            foreach (SpriteFlash flash in bossRoot.GetComponentsInChildren<SpriteFlash>(true))
                flash.enabled = false;

            foreach (SpriteRenderer sr in bossRoot.GetComponentsInChildren<SpriteRenderer>(true))
                sr.enabled = false;

            foreach (Collider2D col in bossRoot.GetComponentsInChildren<Collider2D>(true))
                col.enabled = false;
        }

        if (hideOnVanish != null)
        {
            foreach (GameObject go in hideOnVanish)
                if (go != null) go.SetActive(false);
        }

        if (vanishVfxPrefab != null)
            Instantiate(vanishVfxPrefab, vfxPos + vanishVfxOffset, Quaternion.identity);

        AudioManager.Instance?.PlaySFX(vanishClip, vanishVolume);
        if (vanishScreenShake > 0f) ScreenEffects.Instance?.ShakeScreen(vanishScreenShake);
    }

    private static bool HasTrigger(Animator animator, string triggerName)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
                return true;
        return false;
    }
}