using UnityEngine;
using System.Collections;

public class SporeManager : MonoBehaviour
{
    public static SporeManager Instance { get; private set; }

    [SerializeField] private SporeMeterConfig meterConfig;

    [Header("Mutated State")]
    [SerializeField] private float mutatedDuration = 6f;

    private int currentSpores = 0;
    private int maxSpores;
    private bool isMutated = false;
    private float mutatedTimeRemaining = 0f;

    public bool IsFull => currentSpores >= maxSpores;

    // Bonus from boons
    private float bonusMutatedDuration = 0f;
    private int sporeGainAmount = 1;

    // Spore count event
    public delegate void SporeCountChangedDelegate(int current, int max);
    public event SporeCountChangedDelegate OnSporeCountChanged;

    // Mutated state events
    public event System.Action OnMutatedActivated;
    public event System.Action OnMutatedEnded;
    public event System.Action<float> OnMutatedDrainTick;

    public bool IsMutated => isMutated;

    // -- AWAKE --
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        maxSpores = meterConfig != null ? meterConfig.sporeCapacity : 10; 

        OnMutatedActivated += () => 
        {
            if (TutorialDirector.Instance != null)
                TutorialDirector.Instance.OnMutateActivated();
        };
    }

    // -- UPDATE --
    private void Update()
    {
        if (!isMutated) return;

        mutatedTimeRemaining -= Time.deltaTime;
        OnMutatedDrainTick?.Invoke(mutatedTimeRemaining / (mutatedDuration + bonusMutatedDuration));

        if (mutatedTimeRemaining <= 0f)
            EndMutatedState();
    }

    // -- SET BONUS MUTATED DURATION --
    public void SetBonusMutatedDuration(float bonus)
    {
        bonusMutatedDuration = bonus;
    }

    // -- SET SPORE GAIN AMOUNT --
    public void SetSporeGainAmount(int amount)
    {
        sporeGainAmount = amount;
    }

    // -- COLLECT SPORE --
    public void CollectSpore()
    {
        if (isMutated) return;
        if (currentSpores >= maxSpores) return;

        currentSpores = Mathf.Min(currentSpores + sporeGainAmount, maxSpores);
        OnSporeCountChanged?.Invoke(currentSpores, maxSpores);
    }

    // -- TRY ACTIVATE --
    public void TryActivateMutatedState()
    {
        if (isMutated || currentSpores < maxSpores) return;

        isMutated = true;
        mutatedTimeRemaining = mutatedDuration + bonusMutatedDuration;
        currentSpores = 0;
        OnMutatedActivated?.Invoke();
    }

    // -- END MUTATED STATE --
    private void EndMutatedState()
    {
        isMutated = false;
        mutatedTimeRemaining = 0f;
        OnMutatedEnded?.Invoke();
    }

    // -- END MUTATED STATE SILENTLY (death/reset) --
    private void EndMutatedStateSilent()
    {
        isMutated = false;
        mutatedTimeRemaining = 0f;
    }

    // -- RESET (new level / respawn) --
    public void ResetSpores()
    {
        if (isMutated) EndMutatedStateSilent();
        currentSpores = 0;
        OnSporeCountChanged?.Invoke(currentSpores, maxSpores);
    }

    // -- FILL TO MAX --
    public void FillToMax()
    {
        currentSpores = maxSpores;
        OnSporeCountChanged?.Invoke(currentSpores, maxSpores);
    }

    public int GetCurrentSpores() => currentSpores;
    public int GetMaxSpores() => maxSpores;
    public float GetMutatedDuration() => mutatedDuration;
}