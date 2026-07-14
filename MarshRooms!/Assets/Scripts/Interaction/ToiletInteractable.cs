using UnityEngine;

public class ToiletInteractable : MonoBehaviour
{
    private Interactable interactable;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        interactable.OnInteract += HandleInteract;
    }

    private void HandleInteract()
    {
        TutorialDirector.Instance?.OnToiletInteracted();
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.OnInteract -= HandleInteract;
    }
}