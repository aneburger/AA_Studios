using UnityEngine;

public interface IBreakable
{
    bool IsBroken { get; }
    void Break();
}

public static class BreakableUtil
{
    public static void BreakInRadius(Vector2 center, float radius, float maxStagger = 0.1f)
    {
        var hits = Physics2D.OverlapCircleAll(center, radius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IBreakable>(out var breakable))
            {
                if (breakable is DestructibleProp prop)
                    prop.BreakDelayed(Random.Range(0f, maxStagger));
                else
                    breakable.Break();
            }
        }
    }

    public static bool TryBreak(Collider2D other)
    {
        if (other.TryGetComponent<IBreakable>(out var breakable) && !breakable.IsBroken)
        {
            breakable.Break();
            return true;
        }
        return false;
    }
}