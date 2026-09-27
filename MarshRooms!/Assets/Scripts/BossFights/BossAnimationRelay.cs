using System;
using UnityEngine;

public class BossAnimationRelay : MonoBehaviour
{
    public event Action RollWindupEnded;
    public event Action SummonAction;
    public event Action SummonReady;

    // --- Puffs --- 
    public void OnRollWindupEnd()
    {
        RollWindupEnded?.Invoke();
    }

    public void OnSummonAction()
    {
        SummonAction?.Invoke();
    }

    // --- Portobello- ---
    public void OnSummonReady()
    {
        SummonReady?.Invoke();
    }

    public event Action BurrowImpact;

    public void OnBurrowImpact()
    {
        BurrowImpact?.Invoke();
    }

}