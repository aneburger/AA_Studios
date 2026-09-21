// Animation events can only call methods on components that sit on the Animator's own GameObject.
// Put this on the object that has the boss Animator; the brain (on the parent) subscribes to these events.

using System;
using UnityEngine;

public class BossAnimationRelay : MonoBehaviour
{
    public event Action RollWindupEnded;   // step 4
    public event Action SummonAction;

    // Animation event: last frame of puffs-roll-windup
    public void OnRollWindupEnd()
    {
        RollWindupEnded?.Invoke();
    }

    // Animation event: the frame of puffs-summon where the minions / spikes appear
    public void OnSummonAction()
    {
        SummonAction?.Invoke();
    }
}