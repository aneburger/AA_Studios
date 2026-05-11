// Spawns all VFX
// Access via VFXManager.Instance

using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("VFX Prefabs")]
    [SerializeField] private GameObject dustWalkPrefab;
    [SerializeField] private GameObject dodgeDustPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    
    // -- DODGE DUST --
    public void SpawnWalkDust(Vector2 position)
    {
        if (dustWalkPrefab == null) return;
        Instantiate(dustWalkPrefab, position, Quaternion.identity);
    }

    // -- DODGE DUST INPUT --
    public void SpawnDodgeDust(Vector2 position)
    {
        if (dodgeDustPrefab == null) return;
        Instantiate(dodgeDustPrefab, position, Quaternion.identity);
    }
}