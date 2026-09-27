using System.Collections;
using UnityEngine;

public class PortobelloOutro : BossOutroSequence
{
    [Header("Death")]
    [SerializeField] private Animator bossAnimator;
    [SerializeField] private BossContactDamage contactDamage;
    [SerializeField] private string deathTrigger = "Die";
    [SerializeField] private float musicFadeDuration = 1.5f;

    [Header("Kill Hit-Stop")]
    [SerializeField] private float hitStopDuration = 0.08f;
    [SerializeField] private Color hitStopFlashColor = Color.white;
    [Range(0f, 1f)] [SerializeField] private float hitStopFlashAlpha = 0.7f;
    [SerializeField] private float hitStopFlashDuration = 0.3f;

    [Header("Death Rumble")]
    [SerializeField] private float deathThudDelay = 0.3f;
    [SerializeField] private int rumbleShakeCount = 4;
    [SerializeField] private float rumbleShakeInterval = 0.15f;
    [SerializeField] private float rumbleShakeMin = 0.2f;
    [SerializeField] private float rumbleShakeMax = 0.5f;

    [Header("Dialogue")]
    [SerializeField] private DialogueSequence dialogue;

    [Header("Pacing")]
    [SerializeField] private float pauseBeforeDialogue = 0.5f;

    public override IEnumerator Play(BossRoomController room)
    {
        yield return null;

        contactDamage?.SetContact(0f, 0f);

        // Kill hit-stop, right as he dies
        Time.timeScale = 0f;
        ScreenEffects.Instance?.FlashColor(hitStopFlashColor, hitStopFlashAlpha, hitStopFlashDuration);
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
        yield return DeathRumble();

        AudioManager.Instance?.FadeOutMusic(musicFadeDuration);
        room.HealthBar?.Hide();

        // Death
        if (bossAnimator != null) bossAnimator.SetTrigger(deathTrigger);
        room.LockPlayerExternally();

        yield return new WaitForSeconds(deathThudDelay);
        yield return new WaitForSeconds(pauseBeforeDialogue);

        yield return room.PlayDialogue(dialogue);
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
}