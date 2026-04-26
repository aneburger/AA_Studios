// Player health extends BaseHealth
// Handles game over and UI updates .. please add this :)

using UnityEngine;

public class PlayerHealth : BaseHealth
{
    public static event System.Action OnPlayerDeath;

    protected override void Die()
    {
        base.Die();
        OnPlayerDeath?.Invoke();
        gameObject.SetActive(false);
    }
}