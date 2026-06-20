using UnityEngine;

public class SporeManager : MonoBehaviour
{
    public static SporeManager Instance { get; private set; }

    [SerializeField] private SporeMeterConfig meterConfig;

    private int currentSpores = 0;
    private int maxSpores;

    // Event for UI updates
    public delegate void SporeCountChangedDelegate(int current, int max);
    public event SporeCountChangedDelegate OnSporeCountChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (meterConfig != null)
            maxSpores = meterConfig.sporeCapacity;
        else
            maxSpores = 10; // default
    }

    // -- COLLECT SPORE --
    public void CollectSpore()
    {
        if (currentSpores < maxSpores)
        {
            currentSpores++;
            OnSporeCountChanged?.Invoke(currentSpores, maxSpores);

            // Check if meter is full
            if (currentSpores >= maxSpores)
                OnSporesMeterFull();
        }
    }

    // -- ON SPORES METER FULL --
    private void OnSporesMeterFull()
    {
        Debug.Log("spore meter is full");

        // Mega spore power upgrade (or something like that) will go here 
    }

    // -- GET CURRENT SPORES --
    public int GetCurrentSpores() => currentSpores;

    // -- GET MAX SPORES --
    public int GetMaxSpores() => maxSpores;

    // -- RESET SPORES (for new levels) --
    public void ResetSpores()
    {
        currentSpores = 0;
        OnSporeCountChanged?.Invoke(currentSpores, maxSpores);
    }
}