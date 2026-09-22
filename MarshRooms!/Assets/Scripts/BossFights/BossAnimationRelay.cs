using System;
using UnityEngine;

public class BossAnimationRelay : MonoBehaviour
{
    public event Action RollWindupEnded;
    public event Action SummonAction;

    public void OnRollWindupEnd()
    {
        RollWindupEnded?.Invoke();
    }

    public void OnSummonAction()
    {
        SummonAction?.Invoke();
    }
}