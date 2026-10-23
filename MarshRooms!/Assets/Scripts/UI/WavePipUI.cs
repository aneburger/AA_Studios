// Shows one pip per fight on normal floors.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WavePipUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Transform pipContainer;
    [SerializeField] private WavePip pipPrefab;

    [Header("Sprites")]
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite finalEmptySprite;
    [SerializeField] private Sprite[] activeFrames;
    [SerializeField] private Sprite[] finalActiveFrames;
    [SerializeField] private Sprite completeSprite;
    [SerializeField] private Sprite finalCompleteSprite;

    [Header("Animation")]
    [SerializeField] private float framesPerSecond = 8f;

    [Header("Kill Pop")]
    [SerializeField] private float killPopScale = 0.15f;
    [SerializeField] private float killPopDuration = 0.12f;

    [Header("Completion Wave")]
    [SerializeField] private float waveHeight = 2f;
    [SerializeField] private float waveBounceDuration = 0.35f;

    [Header("Audio")]
    [SerializeField] private AudioClip waveStartClip;
    [SerializeField] private AudioClip finalWaveStartClip;
    [SerializeField] private AudioClip waveCompleteClip;

    private readonly List<WavePip> pips = new List<WavePip>();
    private int activeIndex = -1;
    private float animTimer;
    private Coroutine completionWave;

    // -- ON ENABLE --
    private void OnEnable()
    {
        RoomManager.OnWavesPlanned += HandleWavesPlanned;
        RoomManager.OnWaveStarted += HandleWaveStarted;
        RoomManager.OnWaveCompleted += HandleWaveCompleted;
        EnemyManager.OnEnemyKilled += HandleEnemyKilled;

        RestoreFromRoom();
    }

    // -- ON DISABLE --
    private void OnDisable()
    {
        RoomManager.OnWavesPlanned -= HandleWavesPlanned;
        RoomManager.OnWaveStarted -= HandleWaveStarted;
        RoomManager.OnWaveCompleted -= HandleWaveCompleted;
        EnemyManager.OnEnemyKilled -= HandleEnemyKilled;

        StopCompletionWave();
    }

    // -- RESTORE FROM ROOM --
    private void RestoreFromRoom()
    {
        RoomManager room = RoomManager.Current;
        if (room == null)
        {
            Build(0);
            return;
        }

        Build(room.TotalPips);

        for (int i = 0; i < pips.Count; i++)
        {
            if (i < room.CurrentPipIndex)
                pips[i].SetSprite(GetCompleteSprite(pips[i].IsFinal));
        }

        if (room.CurrentPipIndex < pips.Count && room.CurrentPipStarted)
            SetActivePip(room.CurrentPipIndex);
    }

    // -- BUILD --
    private void Build(int total)
    {
        StopCompletionWave();

        foreach (WavePip pip in pips)
        {
            if (pip != null) Destroy(pip.gameObject);
        }
        pips.Clear();
        activeIndex = -1;

        for (int i = 0; i < total; i++)
        {
            bool isFinal = i == total - 1;
            WavePip pip = Instantiate(pipPrefab, pipContainer);
            pip.Init(isFinal, isFinal ? finalEmptySprite : emptySprite);
            pips.Add(pip);
        }

        if (visualRoot != null)
            visualRoot.SetActive(total > 0);
    }

    // -- HANDLE WAVES PLANNED --
    private void HandleWavesPlanned(int total)
    {
        Build(total);
    }

    // -- HANDLE WAVE STARTED --
    private void HandleWaveStarted(int index)
    {
        if (index < 0 || index >= pips.Count) return;

        SetActivePip(index);
        PlaySfx(pips[index].IsFinal ? finalWaveStartClip : waveStartClip);
    }

    // -- HANDLE WAVE COMPLETED --
    private void HandleWaveCompleted(int index)
    {
        if (index < 0 || index >= pips.Count) return;

        if (activeIndex == index) activeIndex = -1;

        pips[index].SetSprite(GetCompleteSprite(pips[index].IsFinal));
        pips[index].PlayPop();
        PlaySfx(waveCompleteClip);

        // everything is cleared: the pips do a little wave
        if (pips[index].IsFinal)
            StartCompletionWave();
    }

    // -- HANDLE ENEMY KILLED --
    private void HandleEnemyKilled()
    {
        if (activeIndex < 0 || activeIndex >= pips.Count) return;

        pips[activeIndex].PlayPop(killPopScale, killPopDuration);
    }

    // -- SET ACTIVE PIP --
    private void SetActivePip(int index)
    {
        activeIndex = index;
        animTimer = 0f;
        ApplyActiveFrame();
    }

    // -- UPDATE --
    private void Update()
    {
        if (activeIndex < 0 || activeIndex >= pips.Count) return;

        animTimer += Time.deltaTime;
        ApplyActiveFrame();
    }

    // -- APPLY ACTIVE FRAME --
    private void ApplyActiveFrame()
    {
        WavePip pip = pips[activeIndex];
        Sprite[] frames = pip.IsFinal ? finalActiveFrames : activeFrames;
        if (frames == null || frames.Length == 0) return;

        int frame = (int)(animTimer * framesPerSecond) % frames.Length;
        pip.SetSprite(frames[frame]);
    }

    // -- GET COMPLETE SPRITE --
    private Sprite GetCompleteSprite(bool isFinal)
    {
        return isFinal && finalCompleteSprite != null ? finalCompleteSprite : completeSprite;
    }

    // -- PLAY SFX --
    private void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        AudioManager.Instance?.PlaySFX(clip);
    }

    // --- COMPLETION WAVE ---
    private void StartCompletionWave()
    {
        StopCompletionWave();
        completionWave = StartCoroutine(CompletionWaveRoutine());
    }

    private void StopCompletionWave()
    {
        if (completionWave == null) return;

        StopCoroutine(completionWave);
        completionWave = null;
    }

    private IEnumerator CompletionWaveRoutine()
    {
        yield return null;

        int count = pips.Count;
        RectTransform[] rects = new RectTransform[count];
        float[] baseY = new float[count];

        for (int i = 0; i < count; i++)
        {
            rects[i] = pips[i] != null ? pips[i].transform as RectTransform : null;
            if (rects[i] != null) baseY[i] = rects[i].anchoredPosition.y;
        }

        float duration = Mathf.Max(0.01f, waveBounceDuration);
        float stagger = duration * 0.5f;
        float total = (count - 1) * stagger + duration;
        float time = 0f;

        while (time < total)
        {
            time += Time.deltaTime;

            for (int i = 0; i < count; i++)
            {
                if (rects[i] == null) continue;

                float local = (time - i * stagger) / duration;
                float offset = local > 0f && local < 1f
                    ? Mathf.Round(Mathf.Sin(local * Mathf.PI) * waveHeight)
                    : 0f;

                Vector2 pos = rects[i].anchoredPosition;
                pos.y = baseY[i] + offset;
                rects[i].anchoredPosition = pos;
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (rects[i] == null) continue;

            Vector2 pos = rects[i].anchoredPosition;
            pos.y = baseY[i];
            rects[i].anchoredPosition = pos;
        }

        completionWave = null;
    }
}