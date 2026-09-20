// Fires once when the player crosses the boss room door threshold.

using System;
using UnityEngine;
using TopDown.Movement;

[RequireComponent(typeof(Collider2D))]
public class BossDoorTrigger : MonoBehaviour
{
    public event Action OnPlayerCrossed;

    private bool fired;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (fired) return;

        if (other.GetComponentInParent<PlayerMover>() == null) return;

        fired = true;
        OnPlayerCrossed?.Invoke();
    }
}