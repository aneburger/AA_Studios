using UnityEngine;
using UnityEngine.Events;

public class DialogueTrigger : MonoBehaviour
{
    public enum TriggerMode { OnInteract, OnEnter, Manual }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequence sequence;
    [SerializeField] private TriggerMode triggerMode = TriggerMode.OnInteract;

    [Tooltip("If true, this trigger can only fire once ever.")]
    [SerializeField] private bool fireOnce = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onDialogueStart;
    [SerializeField] private UnityEvent onDialogueComplete;

    private bool hasPlayed = false;

    public void OnPlayerInteract()
    {
        if (triggerMode == TriggerMode.OnInteract)
            TriggerDialogue();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerMode == TriggerMode.OnEnter && other.CompareTag("Player"))
            TriggerDialogue();
    }

    public void TriggerDialogue()
    {
        Debug.Log("TriggerDialogue called");
        if (fireOnce && hasPlayed) return;
        if (DialogueManager.Instance == null) return;
        if (DialogueManager.Instance.IsRunning) return;

        hasPlayed = true;
        onDialogueStart?.Invoke();
        DialogueManager.Instance.StartDialogue(sequence, () => onDialogueComplete?.Invoke());
    }

    public void Reset() => hasPlayed = false;
}
