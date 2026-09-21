// Decides what player weapons are allowed to damage.

using UnityEngine;

public static class DamageTargets
{

    public static bool TryGetEnemyHealth(Component source, out BaseHealth health)
    {
        health = source != null ? source.GetComponentInParent<BaseHealth>() : null;
        return health != null && (health is EnemyHealth || health is BossHealth);
    }


    public static bool IsImmune(BaseHealth health)
    {
        return health is BossHealth boss && boss.IsInvulnerable;
    }
}