using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour {
    [Header("References")]
    public Rigidbody2D body;
    public SpriteRenderer spriteRenderer;
    private Animator anim;

    [Header("Player Settings")]
    public float walkSpeed;

    Vector2 lastDirection;  
    Vector2 direction; 

    // ----------------------------------------------------------- START ----------------------------------------------------------- 
    void Start(){
        anim = GetComponent<Animator>();
    }

    // ----------------------------------------------------------- UPDATE ----------------------------------------------------------- 
    void Update() 
    {
        // Movement based on key input
        direction = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        // Handle movement
        bool isMoving = direction.sqrMagnitude > 0.01f;
        if (isMoving)
            lastDirection = direction;
        
        // Send state to animator
        anim.SetBool("isWalking", isMoving);

        Vector2 animDirection = isMoving ? direction : lastDirection;

        anim.SetFloat("x", animDirection.x);
        anim.SetFloat("y", animDirection.y);
    }

    // ---------------------------------------------------------- PHYSICS ----------------------------------------------------------- 
    void FixedUpdate()
    {
        body.linearVelocity = direction * walkSpeed;
    }
}