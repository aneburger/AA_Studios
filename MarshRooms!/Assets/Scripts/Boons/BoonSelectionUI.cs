using UnityEngine;
using System;
using System.Collections.Generic;

public class BoonSelectionUI : MonoBehaviour
{
    public static BoonSelectionUI Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private BoonCardSlotUI[] cardSlots;
    [SerializeField] private UnityEngine.UI.Button skipButton;

    [Header("Audio")]
    [SerializeField] private AudioClip skipSfx;
    [Range(0f, 1f)] [SerializeField] private float skipSfxVolume = 0.6f;

    private Action<BoonCardData> currentCallback;
    private Action currentSkipCallback;

    // -- AWAKE --
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (skipButton != null)
            skipButton.onClick.AddListener(HandleSkip);
    }

    // -- SHOW --
    public void Show(List<BoonCardData> offers, Action<BoonCardData> onPicked, Action onSkipped = null)
    {
        currentCallback = onPicked;
        currentSkipCallback = onSkipped;

        panelRoot.SetActive(true);
        FindPlayerShooter()?.SetCanShoot(false);
        Time.timeScale = 0f;

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
    }

    // -- HANDLE PICKUP --
    private void HandlePick(BoonCardData chosen)
    {
        panelRoot.SetActive(false);
        FindPlayerShooter()?.SetCanShoot(true);
        Time.timeScale = 1f;
        currentCallback?.Invoke(chosen);
        currentCallback = null;
        currentSkipCallback = null;
    }

    // -- HANDLE SKIP --
    private void HandleSkip()
    {
        if (skipSfx != null)
            AudioManager.Instance.PlaySFX(skipSfx, skipSfxVolume);

        panelRoot.SetActive(false);
        FindPlayerShooter()?.SetCanShoot(true);
        Time.timeScale = 1f;
        currentSkipCallback?.Invoke();
        currentCallback = null;
        currentSkipCallback = null;
    }

    // -- FIND SHOOTER --
    private PlayerShooter FindPlayerShooter()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<PlayerShooter>() : null;
    }
}