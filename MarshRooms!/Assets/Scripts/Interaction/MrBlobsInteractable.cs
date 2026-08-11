using UnityEngine;

public class MrBlobsInteractable : MonoBehaviour
{
    private Interactable interactable;
    private TutorialDirector.TutorialStage? lastSeenStage = null;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        interactable.OnInteract += HandleInteract;
    }

    private void Start()
    {
        RefreshSpeechBubble();
    }

    private void Update()
    {
        RefreshSpeechBubble();
    }

    private void HandleInteract()
    {
        if (TutorialDirector.Instance == null) return;

        DialogueSequence toPlay = TutorialDirector.Instance.GetBlobsDialogue();
        if (toPlay != null)
            DialogueManager.Instance?.StartDialogue(toPlay);

        if (TutorialDirector.Instance.CurrentStage == TutorialDirector.TutorialStage.PostToilet)
            TutorialDirector.Instance.OnBlobsPostToiletInteracted();

        lastSeenStage = TutorialDirector.Instance.CurrentStage;
        RefreshSpeechBubble();
    }

    // -- REFRESH SPEECH BUBBLE --
    private void RefreshSpeechBubble()
    {
        if (TutorialDirector.Instance == null)
        {
            interactable.SetHasSomethingToSay(false);
            return;
        }

        TutorialDirector.TutorialStage stage = TutorialDirector.Instance.CurrentStage;
        bool hasDialogue = TutorialDirector.Instance.GetBlobsDialogue() != null;
        bool alreadySeenThisStage = lastSeenStage == stage;

        interactable.SetHasSomethingToSay(hasDialogue && !alreadySeenThisStage);
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.OnInteract -= HandleInteract;
    }
}