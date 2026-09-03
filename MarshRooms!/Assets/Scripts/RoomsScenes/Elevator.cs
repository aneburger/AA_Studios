using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Elevator : MonoBehaviour
{
    [SerializeField] private Interactable interactable;

    [Header("Audio")]
    [SerializeField] private AudioClip elevatorOpenClip;
    [Range(0f, 1f)] public float elevatorOpenClipVolume;
    [SerializeField] private AudioClip elevatorTuneClip;
    [Range(0f, 1f)] public float elevatorTuneClipVolume;

    private Animator anim;
    private bool isOpen = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
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
        StartCoroutine(OpenSequence());
    }

    private IEnumerator OpenSequence()
    {
        isOpen = true;
        yield return new WaitForSeconds(2f);

        AudioManager.Instance.PlaySFX(elevatorOpenClip, elevatorOpenClipVolume);
        yield return new WaitForSeconds(0.2f);
        anim.SetTrigger("Open");
        interactable.SetInteractionLocked(false);
    }

    private void OnElevatorEntered()
    {
        if (!isOpen) return;

        interactable.SetInteractionLocked(true);
        AudioManager.Instance.CrossfadeMusic(elevatorTuneClip, 0.5f, elevatorTuneClipVolume);
        Time.timeScale = 0f;

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
        int currentFloor = LevelLoader.Instance.GetCurrentFloorNumber();

        string nextScene = LevelLoader.Instance.GetNextFloorSceneName();
        LevelLoader.Instance.LoadLevel(nextScene);
        Time.timeScale = 1f;
    }
}