using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTester : MonoBehaviour
{
    [Header("Sequences to test")]
    [SerializeField] private DialogueSequence[] sequences;

    [Header("Keys")]
    [SerializeField] private Key toggleKey = Key.F8;
    [SerializeField] private Key previousKey = Key.LeftBracket;
    [SerializeField] private Key nextKey = Key.RightBracket;
    [SerializeField] private Key playKey = Key.Backslash;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private bool showPanel;
    private int selected;
    private Vector2 scroll;

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || sequences == null || sequences.Length == 0) return;

        if (kb[toggleKey].wasPressedThisFrame) showPanel = !showPanel;
        if (kb[previousKey].wasPressedThisFrame) selected = (selected - 1 + sequences.Length) % sequences.Length;
        if (kb[nextKey].wasPressedThisFrame) selected = (selected + 1) % sequences.Length;
        if (kb[playKey].wasPressedThisFrame) Play(selected);
    }

    private void OnGUI()
    {
        if (!showPanel || sequences == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, Screen.height - 20), GUI.skin.box);
        GUILayout.Label("Dialogue Tester (F8 to hide)");

        scroll = GUILayout.BeginScrollView(scroll);
        for (int i = 0; i < sequences.Length; i++)
        {
            string label = sequences[i] != null ? sequences[i].name : "(empty slot)";
            if (i == selected) label = "> " + label;

            if (GUILayout.Button(label))
            {
                selected = i;
                Play(i);
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void Play(int index)
    {
        if (index < 0 || index >= sequences.Length || sequences[index] == null) return;

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("DialogueTester: no DialogueManager in the scene. Start from the scene that has it (Persistent).");
            return;
        }

        if (DialogueManager.Instance.IsRunning) return;

        DialogueManager.Instance.StartDialogue(sequences[index]);
    }
#endif
}