// Spawns all VFX
// Access via VFXManager.Instance

using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("Dust Prefabs")]
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

    // -- SPAWN VFX SIMPLE -- (General VFX)
    public void SpawnVFX(GameObject vfxPrefab, Vector2 position)
    {
        if (vfxPrefab == null) return;
        Instantiate(vfxPrefab, position, Quaternion.identity);
    }

    // -- SPAWN HIT VFX --
    public void SpawnHitVFX(GameObject vfxPrefab, Vector2 position, int sortingOrder = 0)
    {
        if (vfxPrefab == null) return;
        
        // Random rotation on Z axis
        Quaternion randomRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        GameObject vfx = Instantiate(vfxPrefab, position, randomRotation);
        
        SpriteRenderer sr = vfx.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = sortingOrder; 
    }

    // -- SPAWN MUZZLE FLASH --
    public void SpawnMuzzleFlash(GameObject vfxPrefab, Vector2 position, float angle, int sortingOrder = 0)
    {
        if (vfxPrefab == null) return;
        
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        GameObject vfx = Instantiate(vfxPrefab, position, rotation);
        
        SpriteRenderer sr = vfx.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = sortingOrder;
    }
}