using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DialogueUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image portraitImage;
    [SerializeField] private GameObject nameplatePanel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject continueIndicator;
    [SerializeField] private CanvasGroup dimOverlay;
    [SerializeField] private PortraitTalkAnimator portraitTalkAnimator;

    [Header("Dim Overlay Settings")]
    [SerializeField] private float dimFadeDuration = 0.25f;
    [SerializeField] private float dimMaxAlpha = 0.55f;

    [Header("Typewriter Settings")]
    [SerializeField] private float defaultCharsPerSecond = 30f;
    [SerializeField] private AudioClip defaultTypingSound;
    [SerializeField] private float typingSoundInterval = 0.08f;
    [SerializeField] private float typingPitchVariation = 0.1f;

    private DialogueTextEffects textEffects;
    private AudioSource audioSource;

    private DialogueSequence currentSequence;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool skipRequested = false;
    private Action onCompleteCallback;

    private Coroutine typewriterCoroutine;
    private Coroutine dimFadeCoroutine;

    private void Awake()
    {
        textEffects = dialogueText.GetComponent<DialogueTextEffects>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        dialoguePanel.SetActive(false);
        portraitImage.gameObject.SetActive(false);

        if (dimOverlay != null)
        {
            dimOverlay.alpha = 0f;
            dimOverlay.gameObject.SetActive(false);
            dimOverlay.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ForceEndDialogue();
            return;
        }

        bool advancePressed =
            (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame ||
                                        Mouse.current.rightButton.wasPressedThisFrame)) ||
            (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame ||
                                        Keyboard.current.enterKey.wasPressedThisFrame ||
                                        Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                                        Keyboard.current.spaceKey.wasPressedThisFrame));

        if (advancePressed)
        {
            HandleAdvanceInput();
        }
    }

    private void ForceEndDialogue()
    {
        if (currentSequence == null) return;
        EndDialogue();
    }

    public void Show(DialogueSequence sequence, Action onComplete)
    {
        currentSequence = sequence;
        currentLineIndex = 0;
        onCompleteCallback = onComplete;

        dialoguePanel.SetActive(true);
        continueIndicator.SetActive(false);

        if (dimOverlay != null)
        {
            dimOverlay.gameObject.SetActive(true);
            dimOverlay.alpha = dimMaxAlpha;
        }

        ShowLine(currentLineIndex);
    }

    private void ShowLine(int index)
    {
        if (index >= currentSequence.lines.Length)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = currentSequence.lines[index];

        // Portrait
        if (line.portrait != null || line.talkingPortrait != null)
        {
            portraitImage.sprite = line.talkingPortrait != null ? line.talkingPortrait : line.portrait;
            portraitImage.gameObject.SetActive(true);
        }
        else
        {
            portraitImage.gameObject.SetActive(false);
        }

        // Nameplate
        if (!string.IsNullOrEmpty(line.speakerName))
        {
            nameplatePanel.SetActive(true);
            nameText.text = line.speakerName;
        }
        else
        {
            nameplatePanel.SetActive(false);
        }

        // Parse custom tags from text
        var parsed = DialogueTextParser.Parse(line.text);
        textEffects?.SetEffectRanges(parsed.effectRanges);

        // Voice clip for this line
        if (line.voiceClip != null)
        {
            audioSource.PlayOneShot(line.voiceClip);
        }

        // Start typewriter
        float speed = line.typewriterSpeedOverride > 0f
            ? line.typewriterSpeedOverride
            : defaultCharsPerSecond;

        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        typewriterCoroutine = StartCoroutine(TypewriterRoutine(parsed.cleanText, speed, line));
    }

    private IEnumerator TypewriterRoutine(string fullText, float charsPerSecond, DialogueLine line)
    {
        isTyping = true;
        skipRequested = false;
        continueIndicator.SetActive(false);

        portraitTalkAnimator?.SetTalking(true);

        dialogueText.text = "";
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.text = fullText;
        dialogueText.ForceMeshUpdate();

        int totalChars = dialogueText.textInfo.characterCount;
        float interval = 1f / charsPerSecond;
        float typingSoundTimer = 0f;

        for (int i = 0; i <= totalChars; i++)
        {
            if (skipRequested)
            {
                // Reveal all text instantly
                dialogueText.maxVisibleCharacters = totalChars;
                break;
            }

            dialogueText.maxVisibleCharacters = i;

            // Play typing sound
            typingSoundTimer -= interval;
            if (typingSoundTimer <= 0f && defaultTypingSound != null && i < totalChars)
            {
                AudioManager.Instance.PlaySFXWithPitch(defaultTypingSound, 1f, typingPitchVariation);
                typingSoundTimer = typingSoundInterval;
            }

            yield return new WaitForSecondsRealtime(interval);
        }

        isTyping = false;
        skipRequested = false;

        portraitTalkAnimator?.SetTalking(false);

        if (line.portrait != null) 
            portraitImage.sprite = line.portrait;

        continueIndicator.SetActive(true);
    }

    private void HandleAdvanceInput()
    {
        if (currentSequence == null) return;

        if (isTyping)
        {
            // First click: skip to end of current line
            skipRequested = true;
        }
        else
        {
            // Second click: advance to next line
            continueIndicator.SetActive(false);
            currentLineIndex++;
            ShowLine(currentLineIndex);
        }
    }

    private void EndDialogue()
    {
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        textEffects?.ClearEffects();
        dialoguePanel.SetActive(false);
        portraitImage.gameObject.SetActive(false);
        FadeDim(false);
        currentSequence = null;
        onCompleteCallback?.Invoke();
    }

    private void FadeDim(bool show)
    {
        if (dimOverlay == null) return;

        if (dimFadeCoroutine != null)
            StopCoroutine(dimFadeCoroutine);

        if (show) dimOverlay.gameObject.SetActive(true);
        dimFadeCoroutine = StartCoroutine(FadeDimRoutine(show));
    }

    private IEnumerator FadeDimRoutine(bool show)
    {
        float start = dimOverlay.alpha;
        float end = show ? dimMaxAlpha : 0f;
        float t = 0f;

        while (t < dimFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            dimOverlay.alpha = Mathf.Lerp(start, end, t / dimFadeDuration);
            yield return null;
        }

        dimOverlay.alpha = end;

        if (!show)
            dimOverlay.gameObject.SetActive(false);
    }
}
