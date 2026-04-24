using UnityEngine;
// This is so the player can walk behind furniture

public class SpriteYSorter : MonoBehaviour
{
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        sr.sortingOrder = -(int)(transform.position.y * 100);
    }
}
