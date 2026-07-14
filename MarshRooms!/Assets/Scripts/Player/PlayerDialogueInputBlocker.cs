using UnityEngine;
using TopDown.Movement;

public class PlayerDialogueInputBlocker : MonoBehaviour
{
    private BaseMover mover;
    private BaseShooter shooter;
    private PlayerAimer aimer;

    private void Awake()
    {
        mover = GetComponent<BaseMover>();
        shooter = GetComponent<BaseShooter>();
        aimer = GetComponent<PlayerAimer>();
    }

    private void OnEnable()
    {
        DialogueManager.OnDialogueStarted += DisableInput;
        DialogueManager.OnDialogueEnded += EnableInput;
    }

    private void OnDisable()
    {
        DialogueManager.OnDialogueStarted -= DisableInput;
        DialogueManager.OnDialogueEnded -= EnableInput;
    }

    private void DisableInput()
    {
        if (mover != null) mover.enabled = false;
        if (shooter != null) shooter.enabled = false;
        if (aimer != null) aimer.enabled = false;
    }

    private void EnableInput()
    {
        if (mover != null) mover.enabled = true;
        if (shooter != null) shooter.enabled = true;
        if (aimer != null) aimer.enabled = true;
    }
}