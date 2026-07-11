// Detects when the player enters/exits an Interactable's trigger zone,

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInteractor : MonoBehaviour
{
    private List<Interactable> nearbyInteractables = new List<Interactable>();
    private Interactable currentClosest;

    // -- UPDATE --
    private void Update()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsRunning) return;

        UpdateClosest();

        if (Keyboard.current.eKey.wasPressedThisFrame && currentClosest != null)
        {
            currentClosest.TryInteract();
        }
    }

    // -- UPDATE CLOSEST --
    private void UpdateClosest()
    {
        Interactable closest = null;
        float closestDist = float.MaxValue;

        foreach (var interactable in nearbyInteractables)
        {
            if (interactable == null) continue;
            float dist = Vector2.Distance(transform.position, interactable.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = interactable;
            }
        }

        if (closest != currentClosest)
        {
            if (currentClosest != null) currentClosest.OnPlayerExitRange();
            if (closest != null)        closest.OnPlayerEnterRange();
            currentClosest = closest;
        }
    }

    // -- TRIGGER ENTER --
    private void OnTriggerEnter2D(Collider2D other)
    {
        Interactable interactable = other.GetComponent<Interactable>();
        if (interactable != null && !nearbyInteractables.Contains(interactable))
            nearbyInteractables.Add(interactable);
    }

    // -- TRIGGER EXIT --
    private void OnTriggerExit2D(Collider2D other)
    {
        Interactable interactable = other.GetComponent<Interactable>();
        if (interactable != null)
        {
            nearbyInteractables.Remove(interactable);
            if (currentClosest == interactable)
            {
                currentClosest.OnPlayerExitRange();
                currentClosest = null;
            }
        }
    }
}