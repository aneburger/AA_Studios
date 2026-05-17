using UnityEngine;
// This is so the player can walk behind furniture

public class SpriteYSorter : MonoBehaviour
{
    [SerializeField] private float sortingOffset = 0f;
    
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        sr.sortingOrder = -(int)(transform.position.y * 100) + (int)sortingOffset;
    }
}