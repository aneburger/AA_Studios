using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(TilemapRenderer))]
public class TilemapYSorter : MonoBehaviour
{
    [SerializeField] private float sortingOffset = 0f;

    private TilemapRenderer tr;

    private void Awake()
    {
        tr = GetComponent<TilemapRenderer>();
    }

    private void LateUpdate()
    {
        tr.sortingOrder = -(int)(transform.position.y * 100) + (int)sortingOffset;
    }
}