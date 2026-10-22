using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Interactable))]
public class StoryInteractable : MonoBehaviour
{
    [System.Serializable]
    public class Stage
    {
        public DialogueSequence dialogue;
        public string requiresRunFlag;
        public UnityEvent onCompleted;
    }

    [SerializeField] private string beatId = "floor02_book";
    [SerializeField] private Stage[] stages;

    [Header("Unlock")]
    [SerializeField] private bool waitForRoomCleared = true;
    [SerializeField] private float unlockDelay = 1f;
    [SerializeField] private float reEnableDelay = 0.4f;

    private Interactable interactable;
    private bool roomCleared;
    private bool busy;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        interactable.OnInteract += HandleInteract;
    }

    private void OnEnable()  { RoomManager.OnRoomCleared += HandleRoomCleared; }
    private void OnDisable() { RoomManager.OnRoomCleared -= HandleRoomCleared; }
    private void OnDestroy() { if (interactable != null) interactable.OnInteract -= HandleInteract; }

    private void Start()
    {
        roomCleared = !waitForRoomCleared;
        Refresh();
    }

    private int NextStage()
    {
        if (stages == null) return -1;

        for (int i = 0; i < stages.Length; i++)
        {
            if (StoryFlags.Has(Key(i))) continue;
            Stage s = stages[i];
            if (!string.IsNullOrEmpty(s.requiresRunFlag) && !StoryFlags.HasRun(s.requiresRunFlag))
                return -1;
            return i;
        }
        return -1;
    }

    private string Key(int i) => $"{beatId}#{i}";

    private void Refresh()
    {
        bool available = roomCleared && !busy && NextStage() >= 0;
        interactable.SetInteractionLocked(!available);
        interactable.SetHasSomethingToSay(available);
    }

    private void HandleRoomCleared()
    {
        StartCoroutine(UnlockAfterDelay());
    }

    private IEnumerator UnlockAfterDelay()
    {
        yield return new WaitForSeconds(unlockDelay);
        roomCleared = true;
        Refresh();
    }

    private void HandleInteract()
    {
        int index = NextStage();
        if (!roomCleared || busy || index < 0) return;

        Stage stage = stages[index];
        busy = true;
        Refresh();

        DialogueManager.Instance.StartDialogue(stage.dialogue, () => StartCoroutine(Finish(index)));
    }

    private IEnumerator Finish(int index)
    {
        StoryFlags.Set(Key(index));
        stages[index].onCompleted?.Invoke();

        yield return new WaitForSecondsRealtime(reEnableDelay);
        busy = false;
        Refresh();
    }
}
