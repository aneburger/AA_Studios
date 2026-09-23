using System.Collections;
using UnityEngine;

public class ChefPuffsOutro : BossOutroSequence
{   
    [Header("Death")]
    [SerializeField] private Animator bossAnimator;
    [SerializeField] private string dazeTrigger = "FallIntoDaze";
    [SerializeField] private string deathTrigger = "Death";
    [SerializeField] private float dazeDuration = 1f;
    [SerializeField] private float deathAnimDuration = 1.5f;
    [SerializeField] private float musicFadeDuration = 1.5f;

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

        AudioManager.Instance?.FadeOutMusic(musicFadeDuration);
        room.HealthBar?.Hide();

        // 1. Daze
        if (bossAnimator != null) bossAnimator.SetTrigger(dazeTrigger);
        yield return new WaitForSeconds(dazeDuration);

        // 2. Death
        if (bossAnimator != null) bossAnimator.SetTrigger(deathTrigger);
        yield return new WaitForSeconds(deathAnimDuration);

        room.LockPlayerExternally();

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
}