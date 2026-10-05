// Fires once when the player crosses the boss room door threshold.

using System;
using System.Collections;
using UnityEngine;
using TopDown.Movement;

[RequireComponent(typeof(Collider2D))]
public class BossDoorTrigger : MonoBehaviour
{
    public event Action OnPlayerCrossed;

    [SerializeField] private float armDelay = 2f;

    private bool fired;
    private bool armed;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnEnable()
    {
        fired = false;
        armed = false;
        StartCoroutine(ArmAfterDelay());
    }

    private IEnumerator ArmAfterDelay()
    {
        yield return new WaitForSeconds(armDelay);
        armed = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (fired || !armed) return;

        if (other.GetComponentInParent<PlayerMover>() == null) return;

        fired = true;
        OnPlayerCrossed?.Invoke();
    }
}