using UnityEngine;

public class MageAnimation : MonoBehaviour
{
    private MageAI ai;

    private void Awake()
    {
        ai = GetComponentInParent<MageAI>();
    }

    public void OnHealAction()   => ai?.OnHealAction();
    public void OnSummonAction() => ai?.OnSummonAction();
    public void OnBurstAction()  => ai?.OnBurstAction();
}