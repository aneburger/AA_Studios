using UnityEngine;

public class MrBlobsInteractable : MonoBehaviour
{
    private Interactable interactable;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        interactable.OnInteract += HandleInteract;
    }

    private void HandleInteract()
    {
        if (TutorialDirector.Instance == null) return;

        DialogueSequence toPlay = TutorialDirector.Instance.GetBlobsDialogue();
        if (toPlay != null)
            DialogueManager.Instance?.StartDialogue(toPlay);

        if (TutorialDirector.Instance.CurrentStage == TutorialDirector.TutorialStage.PostToilet)
            TutorialDirector.Instance.OnBlobsPostToiletInteracted();
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.OnInteract -= HandleInteract;
    }
}