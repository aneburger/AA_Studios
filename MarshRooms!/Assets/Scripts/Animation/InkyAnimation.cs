using UnityEngine;

public class InkyAnimation : MonoBehaviour
{
    private InkyAI ai;

    private void Awake()
    {
        ai = GetComponentInParent<InkyAI>();
    }

    public void OnRollBurstStart() => ai?.OnRollBurstStart();
    public void OnRollComplete()   => ai?.OnRollComplete();
}