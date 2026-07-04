// Defines the valid area for item drops

using UnityEngine;

public class RoomDropZone : MonoBehaviour
{
    private PolygonCollider2D zone;

    // -- AWAKE --
    private void Awake()
    {
        zone = GetComponent<PolygonCollider2D>();
    }

    // -- GET SAFE POSITION --
    public Vector2 GetSafeDropPosition(Vector2 preferredPosition)
    {
        if (zone.OverlapPoint(preferredPosition))
            return preferredPosition;

        // Find nearest point inside the zone
        Vector2 closest = preferredPosition;
        float closestDist = float.MaxValue;

        for (int angle = 0; angle < 360; angle += 15)
        {
            for (float radius = 0.5f; radius <= 3f; radius += 0.5f)
            {
                float rad = angle * Mathf.Deg2Rad;
                Vector2 candidate = preferredPosition + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;

                if (zone.OverlapPoint(candidate))
                {
                    float dist = Vector2.Distance(preferredPosition, candidate);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = candidate;
                    }
                }
            }
        }

        return closest;
    }
}