// Shows one pip per fight on normal floors.

using UnityEngine;
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

    [Header("Audio")]
    [SerializeField] private AudioClip waveStartClip;
    [SerializeField] private AudioClip finalWaveStartClip;
    [SerializeField] private AudioClip waveCompleteClip;

    private readonly List<WavePip> pips = new List<WavePip>();
    private int activeIndex = -1;
    private float animTimer;

    // -- ON ENABLE --
    private void OnEnable()
    {
        RoomManager.OnWavesPlanned += HandleWavesPlanned;
        RoomManager.OnWaveStarted += HandleWaveStarted;
        RoomManager.OnWaveCompleted += HandleWaveCompleted;

        RestoreFromRoom();
    }

    // -- ON DISABLE --
    private void OnDisable()
    {
        RoomManager.OnWavesPlanned -= HandleWavesPlanned;
        RoomManager.OnWaveStarted -= HandleWaveStarted;
        RoomManager.OnWaveCompleted -= HandleWaveCompleted;
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
}