using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyKillTester : MonoBehaviour
{
    [SerializeField] private Key killKey = Key.K;
    [SerializeField] private float killDamage = 99999f;
    [SerializeField] private int maxPasses = 6;
    [SerializeField] private float passInterval = 0.2f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private Coroutine running;

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb[killKey].wasPressedThisFrame && running == null)
            running = StartCoroutine(KillAll());
    }

    private IEnumerator KillAll()
    {
        int totalKilled = 0;

        for (int pass = 0; pass < maxPasses; pass++)
        {
            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            int killedThisPass = 0;

            foreach (EnemyHealth enemy in enemies)
            {
                if (enemy == null || enemy.IsDead()) continue;

                enemy.TakeDamage(killDamage);
                killedThisPass++;
            }

            totalKilled += killedThisPass;

            // Nothing left to kill, so no splits or late spawns are coming
            if (killedThisPass == 0) break;

            yield return new WaitForSeconds(passInterval);
        }

        Debug.Log($"EnemyKillTester: killed {totalKilled} enemies.");
        running = null;
    }
#endif
}