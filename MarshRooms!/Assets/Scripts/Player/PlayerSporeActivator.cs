using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSporeActivator : MonoBehaviour
{   
    private bool canActivate = true;

    public void SetCanActivate(bool value) => canActivate = value;

    // -- MUTATE INPUT --
    public void OnActivateSpore(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        if (!canActivate) return;
        if (SporeManager.Instance == null) return;
        SporeManager.Instance.TryActivateMutatedState();
    }
}