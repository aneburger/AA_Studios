using UnityEngine;

// A single line of dialogue spoken by one character.

[System.Serializable] 
public class DialogueLine 
{ 
    [Header("Visual Settings")]
    [Tooltip("Name displayed in the nameplate. Leave empty to hide nameplate.")] 
    public string speakerName; 
    
    [Tooltip("Portrait sprite shown for this line. Can be null.")] 
    public Sprite portrait; 

    [Header("Content")]
    [Tooltip("The dialogue text. Supports <wave>, <shake>, <b> tags.")] 
    [TextArea(2, 5)] 
    public string text; 

    [Header("Audio & Timing")]
    [Tooltip("Audio clip to play when this line starts typing. Optional.")] 
    public AudioClip voiceClip; 
    
    [Tooltip("Override the typewriter speed for this line. 0 = use default.")] 
    public float typewriterSpeedOverride = 0f; 
}

// A full conversation (a sequence of dialogue lines )
// Create via: Right Click > Create > Dialogue > Dialogue Sequence

[CreateAssetMenu(fileName = "NewDialogueSequence", menuName = "Dialogue/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    public DialogueLine[] lines;
}