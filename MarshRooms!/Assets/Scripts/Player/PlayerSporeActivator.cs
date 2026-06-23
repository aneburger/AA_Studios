using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSporeActivator : MonoBehaviour
{   
    // -- MUTATE INPUT --
    public void OnActivateSpore(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        if (SporeManager.Instance == null) return;
        SporeManager.Instance.TryActivateMutatedState();
    }
}