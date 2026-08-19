// Defines the valid area for item drops

using UnityEngine;

public class RoomDropZone : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleMask;

    private PolygonCollider2D zone;

    private void Awake()
    {
        zone = GetComponent<PolygonCollider2D>();
    }

    private bool IsValidDrop(Vector2 point)
    {
        if (!zone.OverlapPoint(point)) return false;
        if (Physics2D.OverlapPoint(point, obstacleMask) != null) return false;
        return true;
    }

    public Vector2 GetSafeDropPosition(Vector2 preferredPosition)
    {
        if (IsValidDrop(preferredPosition))
            return preferredPosition;

        Vector2 closest = preferredPosition;
        float closestDist = float.MaxValue;
        bool found = false;

        for (int angle = 0; angle < 360; angle += 15)
        {
            for (float radius = 0.5f; radius <= 3f; radius += 0.5f)
            {
                float rad = angle * Mathf.Deg2Rad;
                Vector2 candidate = preferredPosition + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;

                if (IsValidDrop(candidate))
                {
                    float dist = Vector2.Distance(preferredPosition, candidate);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = candidate;
                        found = true;
                    }
                }
            }
        }

        return found ? closest : preferredPosition;
    }
}