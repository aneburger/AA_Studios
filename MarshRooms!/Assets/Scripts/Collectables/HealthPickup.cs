// Health pickup collectible

using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [SerializeField] private int healAmount = 4;

    // -- ON TRIGGER ENTER --
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.Heal(healAmount);
        Destroy(gameObject);
    }
}