using UnityEngine;
using System;
using System.Collections.Generic;

public class BoonSelectionUI : MonoBehaviour
{
    public static BoonSelectionUI Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private BoonCardSlotUI[] cardSlots;

    private Action<BoonCardData> currentCallback;

    // -- AWAKE --
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    // -- SHOW --
    public void Show(List<BoonCardData> offers, Action<BoonCardData> onPicked)
    {
        currentCallback = onPicked;
        panelRoot.SetActive(true);
        FindPlayerShooter()?.SetCanShoot(false);

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
        currentCallback?.Invoke(chosen);
        currentCallback = null;
    }

    // -- FIND SHOOTER --
    private PlayerShooter FindPlayerShooter()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<PlayerShooter>() : null;
    }
}