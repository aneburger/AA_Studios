using System.Collections;
using UnityEngine;

public class ChefPuffsOutro : BossOutroSequence
{   
    [Header("Death")]
    [SerializeField] private Animator bossAnimator;
    [SerializeField] private string dazeTrigger = "FallIntoDaze";
    [SerializeField] private string deathTrigger = "Death";
    [SerializeField] private float dazeDuration = 1f;
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
    [SerializeField] private DialogueSequence dialoguePart1;
    [SerializeField] private DialogueSequence dialoguePart2;

    [Header("Croissant")]
    [SerializeField] private GameObject croissantVisualPrefab;
    [SerializeField] private Vector3 croissantOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private AudioClip croissantAppearClip;
    [Range(0f, 1f)] [SerializeField] private float croissantAppearVolume = 1f;
    [SerializeField] private float croissantAppearPause = 1f;

    [Header("Reward Popup")]
    [SerializeField] private BossRewardPopupUI rewardPopup;
    [SerializeField] private string rewardTitle = "New ability: Croissant of Chaos!";
    [SerializeField] private string rewardDescription = "Dodging deals damage to nearby enemies.";

    [Header("Pacing")]
    [SerializeField] private float pauseBeforeDialogue = 0.5f;

    public override IEnumerator Play(BossRoomController room)
    {
        yield return null;

        // Kill hit-stop, right as he dies
        Time.timeScale = 0f;
        ScreenEffects.Instance?.FlashColor(hitStopFlashColor, hitStopFlashAlpha, hitStopFlashDuration);
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
        yield return DeathRumble();

        AudioManager.Instance?.FadeOutMusic(musicFadeDuration);
        room.HealthBar?.Hide();

        // 1. Daze
        if (bossAnimator != null) bossAnimator.SetTrigger(dazeTrigger);
        yield return new WaitForSeconds(dazeDuration);

        // 2. Death
        if (bossAnimator != null) bossAnimator.SetTrigger(deathTrigger);
        room.LockPlayerExternally();

        yield return new WaitForSeconds(deathThudDelay);
        

        yield return new WaitForSeconds(pauseBeforeDialogue);

        yield return room.PlayDialogue(dialoguePart1);

        // Croissant appears above the player
        GameObject croissant = null;
        if (croissantVisualPrefab != null && room.Player != null)
            croissant = Instantiate(croissantVisualPrefab, room.Player.transform.position + croissantOffset, Quaternion.identity);

        AudioManager.Instance?.PlaySFX(croissantAppearClip, croissantAppearVolume);
        
        room.Mover?.PlayLookUpTrigger();
        yield return new WaitForSeconds(croissantAppearPause);
        room.Mover?.EndLookUpHold();

        yield return room.PlayDialogue(dialoguePart2);

        if (croissant != null) Destroy(croissant);

        // Reward popup
        if (rewardPopup != null)
        {
            bool closed = false;
            rewardPopup.Show(rewardTitle, rewardDescription, () => closed = true);
            yield return new WaitUntil(() => closed);
        }

        // Unlock the ability
        PlayerDodgeAttack dodgeAttack = room.Player != null ? room.Player.GetComponent<PlayerDodgeAttack>() : null;
        dodgeAttack?.SetUnlocked(true);
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