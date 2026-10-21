using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TopDown.Movement;

public class BoonSelectionUI : MonoBehaviour
{
    public static BoonSelectionUI Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private BoonCardSlotUI[] cardSlots;
    [SerializeField] private UnityEngine.UI.Button skipButton;

    [Header("Audio")]
    [SerializeField] private AudioClip skipSfx;
    [Range(0f, 1f)] [SerializeField] private float skipSfxVolume = 0.6f;

    [Header("Slide Animation")]
    [SerializeField] private float slideDuration = 0.45f;
    [SerializeField] private float slideStagger = 0.08f;
    [SerializeField] private float slideOffset = 700f;
    [SerializeField] private float slideOutDuration = 0.3f;

    [Header("Slide Audio")]
    [SerializeField] private AudioClip cardSlideClip;
    [Range(0f, 1f)] [SerializeField] private float cardSlideVolume = 0.5f;
    [SerializeField] private float cardSlidePitchVariation = 0.08f;

    private Action<BoonCardData> currentCallback;
    private Action currentSkipCallback;

    private CanvasGroup canvasGroup;
    private Vector2[] restPositions;
    private bool managesGameState = true;
    private bool isTransitioning = false;

    // -- AWAKE --
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (panelRoot != null)
        {
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = panelRoot.AddComponent<CanvasGroup>();

            panelRoot.SetActive(false);
        }

        if (skipButton != null)
            skipButton.onClick.AddListener(HandleSkip);
    }

    // -- SHOW --
    public void Show(List<BoonCardData> offers, Action<BoonCardData> onPicked, Action onSkipped = null, bool manageGameState = true)
    {
        currentCallback = onPicked;
        currentSkipCallback = onSkipped;
        managesGameState = manageGameState;
        isTransitioning = false;

        if (SporeManager.Instance != null && SporeManager.Instance.IsMutated)
        {
            PlayerMover mover = FindPlayerMover();
            PlayerMutatedVisuals mutatedVisuals = mover?.GetComponent<PlayerMutatedVisuals>();
            mutatedVisuals?.SetEffectsVisible(false);
            SporeManager.Instance.ResetSpores();
            mutatedVisuals?.ResetStateSilently();
            mover?.GetComponent<PlayerMutatedStats>()?.ResetStateSilently();
        }
        
        panelRoot.SetActive(true);

        if (managesGameState)
        {
            SetPlayerLocked(true);
            Time.timeScale = 0f;
        }

        if (skipButton != null)
            skipButton.gameObject.SetActive(onSkipped != null);

        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (i < offers.Count)
            {
                cardSlots[i].gameObject.SetActive(true);
                cardSlots[i].Setup(offers[i], HandlePick);
            }
            else
            {
                cardSlots[i].gameObject.SetActive(true);
            }
        }

        StartCoroutine(SlideInRoutine());
    }

    // -- HANDLE PICKUP --
    private void HandlePick(BoonCardData chosen)
    {
        if (isTransitioning) return;

        Action<BoonCardData> callback = currentCallback;
        Close(() => callback?.Invoke(chosen));
    }

    // -- HANDLE SKIP --
    private void HandleSkip()
    {
        if (isTransitioning) return;

        if (skipSfx != null)
            AudioManager.Instance.PlaySFX(skipSfx, skipSfxVolume);

        Action callback = currentSkipCallback;
        Close(() => callback?.Invoke());
    }

    // -- CLOSE --
    private void Close(Action after)
    {
        isTransitioning = true;
        currentCallback = null;
        currentSkipCallback = null;

        StartCoroutine(CloseRoutine(after));
    }

    // -- SLIDE IN --
    private IEnumerator SlideInRoutine()
    {
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0f;

        Canvas.ForceUpdateCanvases();

        int count = cardSlots.Length;
        restPositions = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            RectTransform rect = (RectTransform)cardSlots[i].transform;
            restPositions[i] = rect.anchoredPosition;
            rect.anchoredPosition = restPositions[i] + Vector2.down * slideOffset;
        }

        bool[] soundPlayed = new bool[count];

        float total = slideDuration + slideStagger * (count - 1);
        float elapsed = 0f;

        while (elapsed < total)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / slideDuration);

            for (int i = 0; i < count; i++)
            {
                // whoosh as each card starts moving
                if (!soundPlayed[i] && elapsed >= i * slideStagger)
                {
                    soundPlayed[i] = true;
                    AudioManager.Instance?.PlaySFXWithPitch(cardSlideClip, cardSlideVolume, cardSlidePitchVariation);
                }

                float t = Mathf.Clamp01((elapsed - i * slideStagger) / slideDuration);
                Vector2 start = restPositions[i] + Vector2.down * slideOffset;
                SetSlotPosition(i, Vector2.LerpUnclamped(start, restPositions[i], EaseOutBack(t)));
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
            ((RectTransform)cardSlots[i].transform).anchoredPosition = restPositions[i];

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    // -- CLOSE ROUTINE --
    private IEnumerator CloseRoutine(Action after)
    {
        canvasGroup.blocksRaycasts = false;

        int count = cardSlots.Length;
        float elapsed = 0f;

        while (elapsed < slideOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideOutDuration);
            canvasGroup.alpha = 1f - t;

            for (int i = 0; i < count; i++)
            {
                Vector2 end = restPositions[i] + Vector2.down * slideOffset;
                SetSlotPosition(i, Vector2.Lerp(restPositions[i], end, t * t * t));
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
            ((RectTransform)cardSlots[i].transform).anchoredPosition = restPositions[i];

        panelRoot.SetActive(false);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (managesGameState)
        {
            SetPlayerLocked(false);
            Time.timeScale = 1f;
        }

        isTransitioning = false;
        after?.Invoke();
    }

    // -- SET SLOT POSITION --
    private void SetSlotPosition(int index, Vector2 position)
    {
        ((RectTransform)cardSlots[index].transform).anchoredPosition =
            new Vector2(Mathf.Round(position.x), Mathf.Round(position.y));
    }

    // -- EASE OUT BACK --
    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float p = t - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }

    // -- SET PLAYER LOCKED --
    private void SetPlayerLocked(bool locked)
    {
        FindPlayerMover()?.SetInputLocked(locked);
    }

    // -- FIND PLAYER MOVER --
    private PlayerMover FindPlayerMover()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<PlayerMover>() : null;
    }
}