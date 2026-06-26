using UnityEngine;
using TopDown.Movement;

public class PlayerMutatedStats : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BaseMover mover;
    [SerializeField] private PlayerShooter shooter;

    [Header("Mutated Boosts")]
    [SerializeField] private float speedMultiplier = 1.2f;
    [SerializeField] private float fireRateMultiplier = 0.7f;

    private float originalSpeed;
    private float originalFireRate;

    private void Start()
    {
        SporeManager.Instance.OnMutatedActivated += OnActivated;
        SporeManager.Instance.OnMutatedEnded += OnEnded;
    }

    private void OnDisable()
    {
        if (SporeManager.Instance == null) return;
        SporeManager.Instance.OnMutatedActivated -= OnActivated;
        SporeManager.Instance.OnMutatedEnded -= OnEnded;
    }

    private void OnActivated()
    {
        mover.SetSpeedMultiplier(speedMultiplier);
        shooter.SetFireRateMultiplier(fireRateMultiplier);
    }

    private void OnEnded()
    {
        mover.SetSpeedMultiplier(1f);
        shooter.SetFireRateMultiplier(1f);
    }
}