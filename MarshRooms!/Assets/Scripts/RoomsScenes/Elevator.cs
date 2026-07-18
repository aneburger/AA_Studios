using UnityEngine;
using System.Collections.Generic;

public class Elevator : MonoBehaviour
{
    [SerializeField] private Interactable interactable;

    [Header("Audio")]
    [SerializeField] private AudioClip elevatorDingClip;
    [Range(0f, 1f)] public float elevatorDingClipVolume;
    [SerializeField] private AudioClip elevatorOpenClip;
    [Range(0f, 1f)] public float elevatorOpenClipVolume;
    [SerializeField] private AudioClip elevatorTuneClip;
    [Range(0f, 1f)] public float elevatorTuneClipVolume;
    [SerializeField] private AudioClip elevatorEnterClip;
    [Range(0f, 1f)] public float elevatorEnterClipVolume;

    private Animator anim;
    private bool isOpen = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        interactable.SetInteractionLocked(true);
    }

    private void OnEnable()
    {
        RoomManager.OnRoomCleared += Open;
        interactable.OnInteract += OnElevatorEntered;
    }

    private void OnDisable()
    {
        RoomManager.OnRoomCleared -= Open;
        interactable.OnInteract -= OnElevatorEntered;
    }

    private void Open()
    {
        isOpen = true;
        //yield return new WaitForSeconds(1.5f);
        // Play door opening sound
        anim.SetTrigger("Open");
        // Wait for animation to finish
        // Play ding sound effect
        interactable.SetInteractionLocked(false);
    }

    private void OnElevatorEntered()
    {
        if (!isOpen) return;

        interactable.SetInteractionLocked(true);

        int floorNumber = LevelLoader.Instance.GetCurrentFloorNumber();
        List<BoonCardData> offers = BoonManager.Instance.GetThreeCardOffers(floorNumber);

        BoonSelectionUI.Instance.Show(offers, chosenCard =>
        {
            BoonManager.Instance.ApplyBoonById(chosenCard.boonId);
            ProceedToNextFloor();
        });
    }

    private void ProceedToNextFloor()
    {
        string nextScene = LevelLoader.Instance.GetNextFloorSceneName();
        LevelLoader.Instance.LoadLevel(nextScene);
    }
}