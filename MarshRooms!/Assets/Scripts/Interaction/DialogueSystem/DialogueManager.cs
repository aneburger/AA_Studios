using System;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // -- EVENTS --
    public static event Action OnDialogueStarted;
    public static event Action OnDialogueEnded;

    private DialogueUI dialogueUI;
    private bool isRunning = false;
    private Action pendingCallback;

    // -- AWAKE --
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        dialogueUI = GetComponentInChildren<DialogueUI>(includeInactive: true);
        if (dialogueUI == null)
            Debug.LogError("[DialogueManager] No DialogueUI found in children. " +
                           "Make sure DialogueUI is a child of this GameObject.");
    }

    // -- START DIALOUGUE --
    public void StartDialogue(DialogueSequence sequence, Action onComplete = null)
    {
        if (sequence == null || sequence.lines.Length == 0)
        {
            Debug.LogWarning("[DialogueManager] Tried to start null or empty sequence.");
            onComplete?.Invoke();
            return;
        }

        if (isRunning)
        {
            Debug.LogWarning("[DialogueManager] Dialogue already running. Ignoring new request.");
            return;
        }

        isRunning = true;
        pendingCallback = onComplete;
        Time.timeScale = 0f;
        OnDialogueStarted?.Invoke();
        dialogueUI.Show(sequence, OnSequenceComplete);
    }

    // -- SEQUENCE COMPLETED --
    private void OnSequenceComplete()
    {
        isRunning = false;
        Time.timeScale = 1f;
        OnDialogueEnded?.Invoke();
        pendingCallback?.Invoke();
        pendingCallback = null;
    }
    
    public bool IsRunning => isRunning;
}
