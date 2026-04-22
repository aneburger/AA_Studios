using UnityEngine;
// This is so the player can wakl behind furniture

public class SpriteYSorter : MonoBehaviour
{
    void LateUpdate()
    {
        GetComponent<SpriteRenderer>().sortingOrder = -(int)(transform.position.y * 100);
    }
}
