using UnityEngine;

public class Elevator : MonoBehaviour
{
    private Animator anim;
    private bool isOpen = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        RoomManager.OnRoomCleared += Open;
    }

    private void OnDisable()
    {
        RoomManager.OnRoomCleared -= Open;
    }

    private void Open()
    {
        isOpen = true;
        anim.SetTrigger("Open");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isOpen) return;
        if (!collision.CompareTag("Player")) return;

        
        Debug.Log("Player entered elevator");

        // Will add upgrade screen trigger here later..
    }
}